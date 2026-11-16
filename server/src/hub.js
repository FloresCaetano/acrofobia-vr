import {
  CONSOLE_COMMANDS,
  MessageTypes,
  ProtocolError,
  Roles,
  createMessage,
  normalizeCommand,
  normalizeSudsResponse,
  parseMessage,
} from './protocol.js';

const HELLO_TIMEOUT_MS = 5000;
const HEARTBEAT_MS = 15000;

/**
 * WebSocket hub: identifies clients by role and routes messages between the
 * therapist consoles and the (single) VR client, persisting through SessionService.
 */
export class Hub {
  constructor({ wss, sessions, log = console }) {
    this.sessions = sessions;
    this.log = log;
    this.vr = null;
    this.vrInfo = null;
    this.consoles = new Set();

    wss.on('connection', (socket, request) => this.accept(socket, request));
    // unref: the heartbeat alone must never keep the process alive.
    this.heartbeat = setInterval(() => this.checkHeartbeats(wss), HEARTBEAT_MS).unref();
    wss.on('close', () => clearInterval(this.heartbeat));
  }

  accept(socket, request) {
    socket.isAlive = true;
    socket.remote = request.socket.remoteAddress;
    socket.on('pong', () => { socket.isAlive = true; });

    const helloTimer = setTimeout(() => {
      this.sendError(socket, 'hello_timeout', 'Se esperaba un mensaje "hello" al conectar.');
      socket.close(4001, 'hello timeout');
    }, HELLO_TIMEOUT_MS);

    socket.on('message', (data) => {
      try {
        const message = parseMessage(data.toString());
        if (!socket.role) {
          clearTimeout(helloTimer);
          this.handleHello(socket, message);
        } else if (socket.role === Roles.VR) {
          this.handleVrMessage(message);
        } else {
          this.handleConsoleMessage(socket, message);
        }
      } catch (error) {
        if (!(error instanceof ProtocolError)) this.log.error('[hub] error inesperado', error);
        this.sendError(socket, error.code ?? 'internal_error', error.message);
      }
    });

    socket.on('close', () => {
      clearTimeout(helloTimer);
      this.handleDisconnect(socket);
    });
    socket.on('error', (error) => this.log.warn(`[hub] error de socket (${socket.remote}): ${error.message}`));
  }

  handleHello(socket, message) {
    if (message.type !== MessageTypes.HELLO) {
      throw new ProtocolError('hello_required', 'El primer mensaje debe ser "hello".');
    }
    const role = message.payload.role;
    if (role === Roles.VR) {
      this.attachVr(socket, message.payload);
    } else if (role === Roles.CONSOLE) {
      socket.role = Roles.CONSOLE;
      this.consoles.add(socket);
      this.send(socket, MessageTypes.HELLO, { role, vr: this.vr ? this.vrInfo : null, ...this.sessions.snapshot() });
      this.log.info(`[hub] consola conectada (${socket.remote}). Consolas: ${this.consoles.size}`);
    } else {
      throw new ProtocolError('invalid_role', 'El rol debe ser "vr" o "console".');
    }
  }

  attachVr(socket, payload) {
    if (this.vr) {
      this.log.warn('[hub] llegó otra conexión VR; se reemplaza la anterior.');
      this.vr.replaced = true;
      this.vr.close(4000, 'replaced');
    }
    socket.role = Roles.VR;
    this.vr = socket;
    this.vrInfo = {
      client: String(payload.client ?? 'desconocido'),
      version: String(payload.version ?? ''),
      levels: Array.isArray(payload.levels) ? payload.levels.slice(0, 20) : [],
    };

    const session = this.sessions.active;
    this.send(socket, MessageTypes.HELLO, { role: Roles.VR, session });
    if (session) this.send(socket, MessageTypes.SESSION_START, session);

    this.sessions.record('server', 'vr.connected', { client: this.vrInfo.client, version: this.vrInfo.version });
    this.broadcastToConsoles(MessageTypes.VR_STATUS, { connected: true, ...this.vrInfo });
    this.log.info(`[hub] VR conectada (${socket.remote}).`);
  }

  handleDisconnect(socket) {
    if (socket.role === Roles.CONSOLE) {
      this.consoles.delete(socket);
      this.log.info(`[hub] consola desconectada. Consolas: ${this.consoles.size}`);
    } else if (socket === this.vr) {
      this.vr = null;
      this.sessions.record('server', 'vr.disconnected', {});
      this.broadcastToConsoles(MessageTypes.VR_STATUS, { connected: false });
      this.log.info('[hub] VR desconectada.');
    }
  }

  handleConsoleMessage(socket, message) {
    const { type } = message;

    if (type === MessageTypes.SESSION_START) {
      const session = this.sessions.start(message.payload.patientAlias);
      this.broadcastToConsoles(MessageTypes.SESSION_START, session);
      this.sendToVr(MessageTypes.SESSION_START, session);
      this.log.info(`[hub] sesión iniciada para "${session.patientAlias}" (${session.id}).`);
      return;
    }

    if (type === MessageTypes.SESSION_END) {
      const result = this.sessions.end('console');
      if (!result) throw new ProtocolError('no_session', 'No hay una sesión activa.');
      this.broadcastToConsoles(MessageTypes.SESSION_END, result);
      this.sendToVr(MessageTypes.SESSION_END, { id: result.session.id });
      this.log.info(`[hub] sesión finalizada (${result.session.id}).`);
      return;
    }

    if (!CONSOLE_COMMANDS.has(type)) {
      throw new ProtocolError('unknown_type', `Tipo de mensaje no soportado: ${type}`);
    }

    const payload = normalizeCommand(type, message.payload);
    this.sessions.record('console', type, payload);
    if (!this.sendToVr(type, payload)) {
      this.sendError(socket, 'vr_offline', 'La VR no está conectada; el comando no se pudo entregar.');
    }
  }

  handleVrMessage(message) {
    switch (message.type) {
      case MessageTypes.STATE_UPDATE:
        this.sessions.updateVrState(message.payload);
        this.broadcastToConsoles(MessageTypes.STATE_UPDATE, message.payload);
        break;
      case MessageTypes.SUDS_RESPONSE: {
        const response = normalizeSudsResponse(message.payload);
        const isNew = this.sessions.recordSuds(response);
        this.sendToVr(MessageTypes.SUDS_ACK, { id: response.id });
        if (isNew) this.broadcastToConsoles(MessageTypes.SUDS_RESPONSE, response);
        break;
      }
      case MessageTypes.CALM_TRIGGER:
        this.sessions.record('vr', MessageTypes.CALM_TRIGGER, { source: 'patient' });
        this.broadcastToConsoles(MessageTypes.CALM_TRIGGER, { source: 'patient' });
        break;
      default:
        throw new ProtocolError('unknown_type', `Tipo de mensaje no soportado desde la VR: ${message.type}`);
    }
  }

  sendToVr(type, payload) {
    if (!this.vr) return false;
    this.send(this.vr, type, payload);
    return true;
  }

  broadcastToConsoles(type, payload) {
    const json = JSON.stringify(createMessage(type, payload));
    for (const socket of this.consoles) {
      if (socket.readyState === socket.OPEN) socket.send(json);
    }
  }

  send(socket, type, payload) {
    if (socket.readyState === socket.OPEN) socket.send(JSON.stringify(createMessage(type, payload)));
  }

  sendError(socket, code, message) {
    this.send(socket, MessageTypes.ERROR, { code, message });
  }

  checkHeartbeats(wss) {
    for (const socket of wss.clients) {
      if (!socket.isAlive) {
        socket.terminate();
        continue;
      }
      socket.isAlive = false;
      socket.ping();
    }
  }
}

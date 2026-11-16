// Mensajes JSON con la forma { type, payload, ts }. Ver docs/PROTOCOL.md.

export const Roles = Object.freeze({ VR: 'vr', CONSOLE: 'console' });

export const MessageTypes = Object.freeze({
  HELLO: 'hello',
  ERROR: 'error',
  SESSION_START: 'session.start',
  SESSION_END: 'session.end',
  LEVEL_SET: 'level.set',
  STIMULUS_SET: 'stimulus.set',
  CALM_TRIGGER: 'calm.trigger',
  SUDS_REQUEST: 'suds.request',
  SUDS_CONFIG: 'suds.config',
  SUDS_RESPONSE: 'suds.response',
  SUDS_ACK: 'suds.ack',
  STATE_UPDATE: 'state.update',
  VR_STATUS: 'vr.status',
});

export const STIMULI = Object.freeze(['height', 'wind', 'sway', 'fog', 'sound']);
export const LEVEL_COUNT = 4;
export const SUDS_INTERVAL_RANGE = Object.freeze({ min: 10, max: 600 });

/** Commands a console may send; the server forwards them to the VR client. */
export const CONSOLE_COMMANDS = new Set([
  MessageTypes.LEVEL_SET,
  MessageTypes.STIMULUS_SET,
  MessageTypes.CALM_TRIGGER,
  MessageTypes.SUDS_REQUEST,
  MessageTypes.SUDS_CONFIG,
]);

export function createMessage(type, payload = {}) {
  return { type, payload, ts: Date.now() };
}

export class ProtocolError extends Error {
  constructor(code, message) {
    super(message);
    this.code = code;
  }
}

export function parseMessage(raw) {
  let message;
  try {
    message = JSON.parse(raw);
  } catch {
    throw new ProtocolError('invalid_json', 'El mensaje no es JSON válido.');
  }
  if (!message || typeof message.type !== 'string') {
    throw new ProtocolError('invalid_message', 'Falta el campo "type".');
  }
  if (message.payload == null) message.payload = {};
  if (typeof message.payload !== 'object' || Array.isArray(message.payload)) {
    throw new ProtocolError('invalid_message', '"payload" debe ser un objeto.');
  }
  if (!Number.isFinite(message.ts)) message.ts = Date.now();
  return message;
}

const clamp = (value, min, max) => Math.min(max, Math.max(min, value));

function requireNumber(value, field) {
  const number = Number(value);
  if (!Number.isFinite(number)) {
    throw new ProtocolError('invalid_payload', `"${field}" debe ser un número.`);
  }
  return number;
}

export function sanitizeAlias(alias) {
  const clean = String(alias ?? '').trim().replace(/\s+/g, ' ').slice(0, 40);
  if (!clean) throw new ProtocolError('invalid_payload', 'El alias del paciente es obligatorio.');
  return clean;
}

/** Validates and normalizes the payload of a console command. Returns a new payload object. */
export function normalizeCommand(type, payload) {
  switch (type) {
    case MessageTypes.LEVEL_SET: {
      const level = Math.round(requireNumber(payload.level, 'level'));
      if (level < 1 || level > LEVEL_COUNT) {
        throw new ProtocolError('invalid_payload', `"level" debe estar entre 1 y ${LEVEL_COUNT}.`);
      }
      return { level };
    }
    case MessageTypes.STIMULUS_SET: {
      if (!STIMULI.includes(payload.stimulus)) {
        throw new ProtocolError('invalid_payload', `"stimulus" debe ser uno de: ${STIMULI.join(', ')}.`);
      }
      return { stimulus: payload.stimulus, value: clamp(Math.round(requireNumber(payload.value, 'value')), 0, 100) };
    }
    case MessageTypes.SUDS_CONFIG: {
      const seconds = Math.round(requireNumber(payload.intervalSeconds, 'intervalSeconds'));
      return { intervalSeconds: clamp(seconds, SUDS_INTERVAL_RANGE.min, SUDS_INTERVAL_RANGE.max) };
    }
    case MessageTypes.CALM_TRIGGER:
    case MessageTypes.SUDS_REQUEST:
      return { source: 'console' };
    default:
      throw new ProtocolError('unknown_type', `Tipo de mensaje no soportado: ${type}`);
  }
}

/** Validates a SUDS response coming from the VR client. */
export function normalizeSudsResponse(payload) {
  const id = String(payload.id ?? '').trim();
  if (!id) throw new ProtocolError('invalid_payload', 'La respuesta SUDS necesita un "id".');
  const stimuli = {};
  for (const name of STIMULI) {
    const raw = payload.stimuli?.[name];
    stimuli[name] = Number.isFinite(Number(raw)) ? clamp(Number(raw), 0, 100) : null;
  }
  return {
    id,
    value: clamp(Math.round(requireNumber(payload.value, 'value')), 0, 100),
    level: Number.isFinite(Number(payload.level)) ? Math.round(Number(payload.level)) : 0,
    scene: String(payload.scene ?? ''),
    trigger: String(payload.trigger ?? 'auto'),
    answeredAt: Number.isFinite(Number(payload.answeredAt)) ? Number(payload.answeredAt) : Date.now(),
    queued: Boolean(payload.queued),
    stimuli,
  };
}

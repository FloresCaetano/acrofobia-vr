#!/usr/bin/env node
// Cliente VR falso: simula la app de Unity para probar la consola sin abrir Unity.
// Uso: node tools/fake-vr.js [ws://localhost:8080] [--interval 20]
// Sin dependencias: usa el WebSocket nativo de Node 22+.

const args = process.argv.slice(2);
const url = args.find((arg) => arg.startsWith('ws')) ?? process.env.SERVER_URL ?? 'ws://localhost:8080';
const intervalArg = args.indexOf('--interval');
let sudsIntervalSeconds = intervalArg >= 0 ? Number(args[intervalArg + 1]) : 20;

const LEVELS = [
  { number: 1, name: 'La orilla', description: 'Tierra firme, mirando el puente.', defaults: { height: 35, wind: 15, sway: 0, fog: 25, sound: 55 } },
  { number: 2, name: 'Primer tramo', description: 'Los primeros metros del puente.', defaults: { height: 45, wind: 25, sway: 20, fog: 20, sound: 65 } },
  { number: 3, name: 'Centro', description: 'La mitad del puente.', defaults: { height: 75, wind: 40, sway: 40, fog: 12, sound: 75 } },
  { number: 4, name: 'Mirar abajo', description: 'Viento fuerte y una tabla faltante.', defaults: { height: 100, wind: 70, sway: 65, fog: 5, sound: 85 } },
];
const ZERO = { height: 0, wind: 0, sway: 0, fog: 0, sound: 0 };

const sim = {
  scene: 'neutral',
  level: 0,
  levelEnteredAt: Date.now(),
  targets: { ...ZERO },
  current: { ...ZERO },
  sinceLastSuds: 0,
};
const outbox = new Map(); // SUDS sin confirmar, como la cola offline de Unity
let socket = null;

function log(message) {
  console.log(`[${new Date().toLocaleTimeString('es-EC', { hour12: false })}] ${message}`);
}

function send(type, payload) {
  if (socket?.readyState !== WebSocket.OPEN) return false;
  socket.send(JSON.stringify({ type, payload, ts: Date.now() }));
  return true;
}

function sendState() {
  const level = LEVELS.find((item) => item.number === sim.level);
  send('state.update', {
    scene: sim.scene,
    level: sim.level,
    levelName: level?.name ?? 'Prado',
    stimuli: roundAll(sim.current),
    targets: roundAll(sim.targets),
    sudsIntervalSeconds,
    transitioning: false,
    sudsOpen: false,
  });
}

/** Anxiety model: a peak that depends on the level and the stimuli, which habituates over time. */
function simulatedSuds() {
  if (sim.scene !== 'acrophobia') return clamp(Math.round(5 + Math.random() * 10));
  const intensity = (sim.current.height + sim.current.wind + sim.current.sway) / 3;
  const peak = 20 + sim.level * 12 + intensity * 0.25;
  const minutes = (Date.now() - sim.levelEnteredAt) / 60000;
  const habituated = peak * Math.exp(-minutes / 2.5);
  return clamp(Math.round(habituated + (Math.random() - 0.5) * 10));
}

function answerSuds(trigger) {
  const delay = 1500 + Math.random() * 2000;
  setTimeout(() => {
    const response = {
      id: crypto.randomUUID().replaceAll('-', ''),
      value: simulatedSuds(),
      level: sim.level,
      scene: sim.scene,
      trigger,
      answeredAt: Date.now(),
      stimuli: roundAll(sim.current),
    };
    outbox.set(response.id, response);
    log(`SUDS respondido: ${response.value} (nivel ${response.level}, ${trigger})`);
    flushOutbox();
  }, delay);
}

function flushOutbox() {
  for (const response of outbox.values()) {
    if (!send('suds.response', { ...response, queued: Date.now() - response.answeredAt > 10000 })) break;
  }
}

function handle({ type, payload }) {
  switch (type) {
    case 'hello':
      log('Servidor confirmó la conexión.');
      flushOutbox();
      sendState();
      break;
    case 'level.set': {
      const level = LEVELS.find((item) => item.number === payload.level);
      if (!level) return;
      Object.assign(sim, { scene: 'acrophobia', level: level.number, levelEnteredAt: Date.now(), sinceLastSuds: 0 });
      sim.targets = { ...level.defaults };
      sim.current = { ...level.defaults };
      log(`Nivel ${level.number}: ${level.name}`);
      sendState();
      break;
    }
    case 'stimulus.set':
      sim.targets[payload.stimulus] = payload.value;
      sendState();
      break;
    case 'calm.trigger':
      Object.assign(sim, { scene: 'neutral', level: 0, sinceLastSuds: 0 });
      sim.targets = { ...ZERO };
      sim.current = { ...ZERO };
      log('Botón de calma: de vuelta al prado.');
      sendState();
      break;
    case 'suds.request':
      answerSuds('console');
      break;
    case 'suds.config':
      sudsIntervalSeconds = payload.intervalSeconds;
      log(`Intervalo de SUDS: ${sudsIntervalSeconds} s`);
      sendState();
      break;
    case 'suds.ack':
      outbox.delete(payload.id);
      break;
    case 'session.start':
      log(`Sesión iniciada: ${payload.patientAlias}`);
      break;
    case 'session.end':
      log('Sesión finalizada.');
      break;
    case 'error':
      log(`Error del servidor: ${payload.message}`);
      break;
    default:
      break;
  }
}

function connect() {
  log(`Conectando a ${url}…`);
  socket = new WebSocket(url);
  socket.addEventListener('open', () => {
    send('hello', {
      role: 'vr',
      client: 'fake-vr',
      version: '1.0',
      levels: LEVELS.map(({ number, name, description }) => ({ number, name, description })),
    });
  });
  socket.addEventListener('message', (event) => handle(JSON.parse(event.data)));
  socket.addEventListener('close', (event) => {
    // 4000 = another VR client took over; back off so two clients never fight for the slot.
    const delay = event.code === 4000 ? 15000 : 2000;
    log(event.code === 4000 ? 'Otra VR se conectó y reemplazó a esta. Reintento en 15 s.' : 'Conexión cerrada. Reintentando en 2 s…');
    setTimeout(connect, delay);
  });
  socket.addEventListener('error', () => {});
}

// Simulation loop: interpolation toward the targets, periodic state and automatic SUDS.
setInterval(() => {
  for (const key of Object.keys(sim.current)) {
    const diff = sim.targets[key] - sim.current[key];
    sim.current[key] += Math.sign(diff) * Math.min(Math.abs(diff), 25);
  }
  if (sim.scene === 'acrophobia') {
    sim.sinceLastSuds += 0.5;
    if (sim.sinceLastSuds >= sudsIntervalSeconds) {
      sim.sinceLastSuds = 0;
      answerSuds('auto');
    }
  }
}, 500);
setInterval(sendState, 3000);

const clamp = (value) => Math.max(0, Math.min(100, value));
const roundAll = (values) => Object.fromEntries(Object.entries(values).map(([key, value]) => [key, Math.round(value)]));

connect();

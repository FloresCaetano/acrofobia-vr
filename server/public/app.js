// Consola del terapeuta: cliente WebSocket, controles, gráfica de SUDS y sugerencias.
import { SudsChart, LEVEL_COLORS } from './suds-chart.js';

const STIMULI = [
  { key: 'height', label: 'Altura' },
  { key: 'wind', label: 'Viento' },
  { key: 'sway', label: 'Balanceo' },
  { key: 'fog', label: 'Niebla' },
  { key: 'sound', label: 'Sonido' },
];

const DEFAULT_LEVELS = [
  { number: 1, name: 'La orilla' },
  { number: 2, name: 'Primer tramo' },
  { number: 3, name: 'Centro' },
  { number: 4, name: 'Mirar abajo' },
];

const SEND_INTERVAL_MS = 120;
const RECONNECT_MS = 2000;

const $ = (selector) => document.querySelector(selector);

const state = {
  socket: null,
  session: null,
  vrConnected: false,
  levels: DEFAULT_LEVELS,
  vrState: null,
  suds: [],
  chart: null,
};

// ---------- Conexión ----------

function connect() {
  const url = `${location.protocol === 'https:' ? 'wss' : 'ws'}://${location.host}`;
  const socket = new WebSocket(url);
  state.socket = socket;
  setBadge('#server-status', 'warn', 'Servidor: conectando…');

  socket.addEventListener('open', () => {
    setBadge('#server-status', 'on', 'Servidor conectado');
    send('hello', { role: 'console' });
  });

  socket.addEventListener('message', (event) => {
    try {
      handle(JSON.parse(event.data));
    } catch (error) {
      console.error('Mensaje inválido', error);
    }
  });

  socket.addEventListener('close', () => {
    setBadge('#server-status', 'off', 'Servidor desconectado · reintentando');
    setVrStatus(false);
    setTimeout(connect, RECONNECT_MS);
  });
}

function send(type, payload = {}) {
  if (state.socket?.readyState !== WebSocket.OPEN) return false;
  state.socket.send(JSON.stringify({ type, payload, ts: Date.now() }));
  return true;
}

function handle({ type, payload, ts }) {
  switch (type) {
    case 'hello':
      setVrStatus(Boolean(payload.vr), payload.vr);
      if (payload.vrState) applyVrState(payload.vrState);
      if (payload.session) enterSession(payload.session, payload.suds ?? []);
      else if (!state.session) showView('start');
      break;
    case 'vr.status':
      setVrStatus(payload.connected, payload);
      log(payload.connected ? 'VR conectada' : 'VR desconectada', payload.connected ? 'info' : 'error');
      break;
    case 'session.start':
      enterSession(payload, []);
      log(`Sesión iniciada con «${payload.patientAlias}»`);
      break;
    case 'session.end':
      showSummary(payload);
      break;
    case 'state.update':
      applyVrState(payload);
      break;
    case 'suds.response':
      addSuds(payload);
      break;
    case 'calm.trigger':
      log('El paciente pulsó el botón de calma', 'calm', ts);
      break;
    case 'error':
      log(`Aviso: ${payload.message}`, 'error');
      break;
    default:
      break;
  }
}

// ---------- Vistas ----------

function showView(name) {
  for (const view of ['start', 'session', 'summary']) {
    $(`#${view}-view`).hidden = view !== name;
  }
  if (name === 'start') loadSessionList();
}

function setBadge(selector, status, text) {
  const badge = $(selector);
  badge.dataset.state = status;
  badge.querySelector('.label').textContent = text;
}

function setVrStatus(connected, info) {
  state.vrConnected = connected;
  setBadge('#vr-status', connected ? 'on' : 'off', connected ? 'VR conectada' : 'VR desconectada');
  if (connected && info?.levels?.length) {
    state.levels = info.levels;
    renderLevels();
  }
}

// ---------- Inicio ----------

$('#start-form').addEventListener('submit', (event) => {
  event.preventDefault();
  const alias = $('#alias').value.trim();
  if (!alias) return;
  if (!send('session.start', { patientAlias: alias })) alert('No hay conexión con el servidor.');
});

async function loadSessionList() {
  const list = $('#session-list');
  try {
    const sessions = await (await fetch('/api/sessions')).json();
    list.replaceChildren();
    if (!sessions.length) {
      list.innerHTML = '<li class="muted">Todavía no hay sesiones guardadas.</li>';
      return;
    }
    for (const session of sessions.slice(0, 8)) {
      const item = document.createElement('li');
      const when = new Date(session.startedAt).toLocaleString('es-EC', { dateStyle: 'medium', timeStyle: 'short' });
      const info = document.createElement('span');
      info.textContent = `${session.patientAlias} · ${when} · ${session.sudsCount} SUDS${session.endedAt ? '' : ' · en curso'}`;
      const link = document.createElement('a');
      link.href = `/api/sessions/${session.id}/export.csv`;
      link.textContent = 'CSV';
      link.setAttribute('aria-label', `Exportar CSV de la sesión de ${session.patientAlias}`);
      item.append(info, link);
      list.append(item);
    }
  } catch {
    list.innerHTML = '<li class="muted">No se pudo cargar la lista.</li>';
  }
}

// ---------- Sesión ----------

let clockTimer = null;

function enterSession(session, suds) {
  const isNew = state.session?.id !== session.id;
  state.session = session;
  showView('session');
  $('#session-alias').textContent = session.patientAlias;
  $('#session-start').textContent = new Date(session.startedAt).toLocaleTimeString('es-EC', { timeStyle: 'short' });

  if (isNew) {
    state.suds = [];
    state.chart?.destroy();
    state.chart = new SudsChart($('#suds-chart'), session.startedAt);
    $('#event-log').replaceChildren();
    for (const response of suds) addSuds(response, true);
    if (state.vrState) state.chart.trackState(state.vrState);
  }

  clearInterval(clockTimer);
  clockTimer = setInterval(() => {
    $('#session-clock').textContent = formatDuration(Date.now() - session.startedAt);
    state.chart?.tick();
  }, 1000);
}

function renderLevels() {
  const container = $('#level-buttons');
  container.replaceChildren();
  for (const level of state.levels) {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'btn';
    button.dataset.level = level.number;
    button.innerHTML = `<span>${level.number} · ${escapeHtml(level.name)}</span>`;
    if (level.description) {
      const small = document.createElement('small');
      small.textContent = level.description;
      button.append(small);
    }
    button.addEventListener('click', () => {
      send('level.set', { level: level.number });
      log(`Nivel ${level.number} solicitado`);
    });
    container.append(button);
  }
  highlightLevel(state.vrState?.level ?? 0);
  renderLegend();
}

function renderLegend() {
  const legend = $('#level-legend');
  legend.replaceChildren();
  const entries = [{ number: 0, name: 'Prado (descanso)' }, ...state.levels];
  for (const level of entries) {
    const span = document.createElement('span');
    span.innerHTML = `<i style="background:${LEVEL_COLORS[level.number] ?? LEVEL_COLORS[4]}"></i>${level.number ? `Nivel ${level.number}` : escapeHtml(level.name)}`;
    legend.append(span);
  }
}

function highlightLevel(level) {
  for (const button of document.querySelectorAll('#level-buttons .btn')) {
    button.setAttribute('aria-pressed', String(Number(button.dataset.level) === level));
  }
}

function renderStimuli() {
  const container = $('#stimuli');
  for (const { key, label } of STIMULI) {
    const row = document.createElement('div');
    row.className = 'stimulus';
    row.innerHTML = `
      <label for="stim-${key}">${label}</label>
      <input type="range" id="stim-${key}" min="0" max="100" step="1" value="0" aria-describedby="current-${key}">
      <output for="stim-${key}" id="out-${key}">0</output>
      <span class="current" id="current-${key}">En la VR: —</span>`;
    container.append(row);

    const input = row.querySelector('input');
    const output = row.querySelector('output');
    const throttled = throttle((value) => send('stimulus.set', { stimulus: key, value }), SEND_INTERVAL_MS);
    input.addEventListener('input', () => {
      input.dataset.editing = '1';
      output.textContent = input.value;
      throttled(Number(input.value));
    });
    input.addEventListener('change', () => {
      delete input.dataset.editing;
      log(`${label}: ${input.value}`);
    });
  }
}

function applyVrState(vrState) {
  state.vrState = vrState;
  const scene = vrState.scene === 'acrophobia' ? `Cañón · Nivel ${vrState.level}` : 'Prado (descanso)';
  $('#current-scene').textContent = vrState.transitioning ? `${scene} (en transición)` : scene;
  highlightLevel(vrState.level ?? 0);

  for (const { key } of STIMULI) {
    const input = $(`#stim-${key}`);
    const target = Math.round(vrState.targets?.[key] ?? 0);
    const current = Math.round(vrState.stimuli?.[key] ?? 0);
    if (!input.dataset.editing && document.activeElement !== input) {
      input.value = target;
      $(`#out-${key}`).textContent = target;
    }
    $(`#current-${key}`).textContent = `En la VR: ${current}`;
  }

  if (vrState.sudsIntervalSeconds) {
    const select = $('#suds-interval');
    const value = String(Math.round(vrState.sudsIntervalSeconds));
    if (![...select.options].some((option) => option.value === value)) {
      select.add(new Option(`${value} segundos`, value));
    }
    if (document.activeElement !== select) select.value = value;
  }

  state.chart?.trackState(vrState);
}

function addSuds(response, silent = false) {
  if (state.suds.some((item) => item.id === response.id)) return;
  state.suds.push(response);
  state.suds.sort((a, b) => a.answeredAt - b.answeredAt);
  state.chart?.addSuds(response);
  $('#last-suds').textContent = `${response.value} (nivel ${response.level})`;
  if (!silent) {
    const late = response.queued ? ' · enviado al reconectar' : '';
    log(`SUDS ${response.value} en nivel ${response.level}${late}`, 'suds', response.answeredAt);
  }
  updateSuggestion();
}

/** Simple heuristics. They only suggest; the therapist decides. */
function updateSuggestion() {
  const box = $('#suggestion');
  const recent = state.suds.filter((item) => item.level > 0).slice(-2);
  const last = state.suds.at(-1);
  let text = '';

  if (last && last.value >= 70) {
    text = 'Considerar bajar la intensidad.';
  } else if (recent.length === 2 && recent.every((item) => item.value <= 30)
      && Math.abs(recent[0].value - recent[1].value) <= 10 && recent[0].level === recent[1].level) {
    text = recent[1].level < state.levels.length ? 'Considerar subir de nivel.' : 'Ansiedad baja y estable en el último nivel.';
  }

  box.hidden = !text;
  $('#suggestion-text').textContent = text;
}

$('#calm-button').addEventListener('click', () => {
  send('calm.trigger');
  log('Botón de calma activado desde la consola', 'calm');
});

$('#suds-request').addEventListener('click', () => {
  send('suds.request');
  log('Pregunta SUDS solicitada');
});

$('#suds-interval-apply').addEventListener('click', () => {
  const seconds = Number($('#suds-interval').value);
  send('suds.config', { intervalSeconds: seconds });
  log(`Intervalo de SUDS: cada ${seconds} s`);
});

$('#end-button').addEventListener('click', () => {
  if (confirm('¿Terminar la sesión? Se guardará el resumen.')) send('session.end');
});

$('#new-session').addEventListener('click', () => {
  state.session = null;
  $('#alias').value = '';
  showView('start');
  $('#alias').focus();
});

// ---------- Resumen ----------

function showSummary({ session, summary }) {
  clearInterval(clockTimer);
  state.session = null;
  showView('summary');

  const stats = [
    ['Paciente', session.patientAlias],
    ['Duración', formatDuration(summary.durationMs)],
    ['Respuestas SUDS', summary.sudsCount],
    ['Promedio', summary.sudsAverage ?? '—'],
    ['Pico', summary.sudsPeak ?? '—'],
    ['Primera → última', summary.sudsCount ? `${summary.sudsFirst} → ${summary.sudsLast}` : '—'],
    ['Nivel máximo', summary.maxLevel ?? '—'],
  ];
  $('#summary-stats').innerHTML = stats
    .map(([label, value]) => `<div><dt>${label}</dt><dd>${escapeHtml(String(value))}</dd></div>`)
    .join('');

  const rows = Object.entries(summary.byLevel)
    .map(([level, data]) => `<tr><td>${level === '0' ? 'Prado' : `Nivel ${level}`}</td><td>${data.count}</td><td>${data.first}</td><td>${data.last}</td><td>${data.peak}</td></tr>`)
    .join('');
  $('#summary-levels').innerHTML = rows
    ? `<h3>Por nivel</h3><table><thead><tr><th>Nivel</th><th>Respuestas</th><th>Primera</th><th>Última</th><th>Pico</th></tr></thead><tbody>${rows}</tbody></table>`
    : '';

  $('#export-link').href = `/api/sessions/${session.id}/export.csv`;
}

// ---------- Utilidades ----------

function log(message, kind = 'info', ts = Date.now()) {
  const item = document.createElement('li');
  item.dataset.kind = kind;
  const time = document.createElement('time');
  time.dateTime = new Date(ts).toISOString();
  time.textContent = new Date(ts).toLocaleTimeString('es-EC', { hour12: false });
  const text = document.createElement('span');
  text.textContent = message;
  item.append(time, text);
  $('#event-log').prepend(item);
}

function throttle(fn, ms) {
  let last = 0;
  let timer = null;
  let pending;
  return (value) => {
    pending = value;
    const wait = ms - (Date.now() - last);
    if (wait <= 0) {
      last = Date.now();
      fn(pending);
    } else if (!timer) {
      timer = setTimeout(() => {
        timer = null;
        last = Date.now();
        fn(pending);
      }, wait);
    }
  };
}

function formatDuration(ms) {
  const total = Math.max(0, Math.floor(ms / 1000));
  const minutes = String(Math.floor(total / 60)).padStart(2, '0');
  const seconds = String(total % 60).padStart(2, '0');
  return `${minutes}:${seconds}`;
}

function escapeHtml(text) {
  return text.replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);
}

renderStimuli();
renderLevels();
connect();

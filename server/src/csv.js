// CSV export of a session: SUDS answers and events on one timeline.
// Uses ";" as separator and a UTF-8 BOM so Excel in Spanish locales opens it correctly.

const SEPARATOR = ';';
const HEADER = [
  'tipo', 'fecha_hora', 'segundos_desde_inicio', 'nivel', 'escena', 'suds',
  'altura', 'viento', 'balanceo', 'niebla', 'sonido', 'evento', 'origen', 'detalle',
];

function escape(value) {
  if (value === null || value === undefined) return '';
  const text = String(value);
  return /[";\n\r]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
}

const round = (value) => (value === null || value === undefined ? '' : Math.round(value));

export function sessionToCsv({ session, suds, events }) {
  const rows = [];
  const secondsFromStart = (ts) => ((ts - session.startedAt) / 1000).toFixed(1);

  for (const response of suds) {
    rows.push({
      ts: response.answeredAt,
      cells: [
        'suds', new Date(response.answeredAt).toISOString(), secondsFromStart(response.answeredAt), response.level,
        response.scene, response.value, round(response.stimuli.height), round(response.stimuli.wind),
        round(response.stimuli.sway), round(response.stimuli.fog), round(response.stimuli.sound),
        '', response.trigger, response.queued ? 'enviada al reconectar' : '',
      ],
    });
  }

  for (const event of events) {
    if (event.type === 'state.update') continue;
    rows.push({
      ts: event.ts,
      cells: [
        'evento', new Date(event.ts).toISOString(), secondsFromStart(event.ts), event.payload.level ?? '',
        '', '', '', '', '', '', '', event.type, event.source, JSON.stringify(event.payload),
      ],
    });
  }

  rows.sort((a, b) => a.ts - b.ts);
  const lines = [HEADER, ...rows.map((row) => row.cells)].map((cells) => cells.map(escape).join(SEPARATOR));
  return '﻿' + lines.join('\r\n') + '\r\n';
}

// Gráfica de SUDS en vivo (Chart.js) con bandas de fondo según el nivel activo.
/* global Chart */

export const LEVEL_COLORS = {
  0: 'rgba(120, 190, 140, 0.20)',
  1: 'rgba(90, 150, 230, 0.20)',
  2: 'rgba(140, 120, 230, 0.24)',
  3: 'rgba(200, 110, 200, 0.26)',
  4: 'rgba(240, 120, 110, 0.30)',
};

const TEAL = '#2bb3a8';
const TEXT = '#b8c4d3';
const GRID = 'rgba(184, 196, 211, 0.15)';

const levelBands = {
  id: 'levelBands',
  beforeDatasetsDraw(chart, _args, options) {
    const { ctx, chartArea, scales } = chart;
    ctx.save();
    for (const band of options.bands ?? []) {
      const from = Math.max(scales.x.getPixelForValue(band.from), chartArea.left);
      const to = Math.min(scales.x.getPixelForValue(band.to), chartArea.right);
      if (to <= from) continue;
      ctx.fillStyle = LEVEL_COLORS[band.level] ?? LEVEL_COLORS[4];
      ctx.fillRect(from, chartArea.top, to - from, chartArea.bottom - chartArea.top);
      if (to - from > 40) {
        ctx.fillStyle = TEXT;
        ctx.font = '600 12px "Atkinson Hyperlegible", system-ui, sans-serif';
        ctx.fillText(band.level ? `N${band.level}` : 'Prado', from + 6, chartArea.top + 16);
      }
    }
    ctx.restore();
  },
};

const sudsColor = (value) => (value >= 70 ? '#f08a7e' : value >= 40 ? '#f2a93b' : TEAL);

export class SudsChart {
  constructor(canvas, startedAt) {
    this.startedAt = startedAt;
    this.bands = [];
    this.chart = new Chart(canvas, {
      type: 'line',
      data: {
        datasets: [{
          label: 'SUDS',
          data: [],
          borderColor: TEAL,
          backgroundColor: TEAL,
          borderWidth: 3,
          tension: 0.25,
          pointRadius: 6,
          pointHoverRadius: 8,
          pointBackgroundColor: (context) => sudsColor(context.raw?.y ?? 0),
          pointBorderColor: '#0b1424',
          pointBorderWidth: 2,
        }],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        animation: { duration: 250 },
        parsing: false,
        scales: {
          x: {
            type: 'linear',
            min: 0,
            max: 5,
            title: { display: true, text: 'Minutos desde el inicio', color: TEXT },
            ticks: { color: TEXT, stepSize: 1 },
            grid: { color: GRID },
          },
          y: {
            min: 0,
            max: 100,
            title: { display: true, text: 'SUDS (0–100)', color: TEXT },
            ticks: { color: TEXT, stepSize: 20 },
            grid: { color: GRID },
          },
        },
        plugins: {
          legend: { display: false },
          levelBands: { bands: this.bands },
          tooltip: {
            callbacks: {
              label: (context) => `SUDS ${context.raw.y} · nivel ${context.raw.level}`,
              title: (items) => formatMinutes(items[0].raw.x),
            },
          },
        },
      },
      plugins: [levelBands],
    });
  }

  minutes(ts) {
    return (ts - this.startedAt) / 60000;
  }

  addSuds(response) {
    const data = this.chart.data.datasets[0].data;
    data.push({ x: this.minutes(response.answeredAt), y: response.value, level: response.level });
    data.sort((a, b) => a.x - b.x);
    this.refresh();
  }

  /** Opens a new background band whenever the patient changes level or scene. */
  trackState(vrState) {
    const level = vrState.scene === 'acrophobia' ? vrState.level : 0;
    const now = this.minutes(Date.now());
    const last = this.bands.at(-1);
    if (last && last.level === level) return;
    if (last) last.to = now;
    this.bands.push({ level, from: Math.max(0, now), to: now });
    this.refresh();
  }

  tick() {
    this.refresh();
  }

  refresh() {
    const now = this.minutes(Date.now());
    const last = this.bands.at(-1);
    if (last) last.to = now;
    this.chart.options.scales.x.max = Math.max(5, Math.ceil(now + 0.5));
    this.chart.update('none');
  }

  destroy() {
    this.chart.destroy();
  }
}

function formatMinutes(minutes) {
  const total = Math.round(minutes * 60);
  return `${Math.floor(total / 60)}:${String(total % 60).padStart(2, '0')} desde el inicio`;
}

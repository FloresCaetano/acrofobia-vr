import { sanitizeAlias } from './protocol.js';

/**
 * Owns the lifecycle of the therapy session and decides what gets persisted.
 * Only one session is active at a time; an unfinished session survives a server restart.
 */
export class SessionService {
  constructor(repository) {
    this.repository = repository;
    this.active = repository.findOpenSession();
    this.vrState = null;
    this.lastPersistedStateKey = null;
  }

  get activeSessionId() {
    return this.active?.id ?? null;
  }

  start(patientAlias) {
    if (this.active) this.end('replaced');
    this.active = this.repository.createSession(sanitizeAlias(patientAlias));
    this.record('server', 'session.start', { patientAlias: this.active.patientAlias });
    return this.active;
  }

  end(reason = 'console') {
    if (!this.active) return null;
    const { id } = this.active;
    this.record('server', 'session.end', { reason });
    const session = this.repository.endSession(id);
    this.active = null;
    return { session, summary: summarize(session, this.repository.listSudsResponses(id)) };
  }

  record(source, type, payload, ts = Date.now()) {
    this.repository.addEvent({ sessionId: this.activeSessionId, ts, source, type, payload });
  }

  /** Returns true when the response is new; repeated ids are acknowledged but not stored twice. */
  recordSuds(response) {
    return this.repository.addSudsResponse(this.activeSessionId, response);
  }

  /** Caches the latest VR state and persists it only when the scene, level or target stimuli change. */
  updateVrState(state) {
    this.vrState = state;
    const key = JSON.stringify([state.scene, state.level, state.targets ?? state.stimuli, state.sudsIntervalSeconds]);
    if (key === this.lastPersistedStateKey) return;
    this.lastPersistedStateKey = key;
    this.record('vr', 'state.update', state);
  }

  snapshot() {
    return {
      session: this.active,
      suds: this.active ? this.repository.listSudsResponses(this.active.id) : [],
      vrState: this.vrState,
    };
  }

  details(sessionId) {
    const session = this.repository.findSession(sessionId);
    if (!session) return null;
    const suds = this.repository.listSudsResponses(sessionId);
    return {
      session,
      summary: summarize(session, suds),
      suds,
      events: this.repository.listEvents(sessionId),
    };
  }
}

export function summarize(session, suds) {
  const values = suds.map((response) => response.value);
  const end = session.endedAt ?? Date.now();
  const byLevel = {};
  for (const response of suds) {
    const bucket = (byLevel[response.level] ??= { count: 0, first: response.value, last: response.value, peak: 0 });
    bucket.count += 1;
    bucket.last = response.value;
    bucket.peak = Math.max(bucket.peak, response.value);
  }
  return {
    durationMs: end - session.startedAt,
    sudsCount: values.length,
    sudsAverage: values.length ? Math.round((values.reduce((a, b) => a + b, 0) / values.length) * 10) / 10 : null,
    sudsPeak: values.length ? Math.max(...values) : null,
    sudsFirst: values.at(0) ?? null,
    sudsLast: values.at(-1) ?? null,
    maxLevel: suds.length ? Math.max(...suds.map((response) => response.level)) : null,
    byLevel,
  };
}

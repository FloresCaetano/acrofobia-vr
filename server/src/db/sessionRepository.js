import { randomUUID } from 'node:crypto';

/** Data access for sessions, events and SUDS responses. All methods are synchronous (better-sqlite3). */
export class SessionRepository {
  constructor(db) {
    this.statements = {
      insertSession: db.prepare('INSERT INTO sessions (id, patient_alias, started_at) VALUES (?, ?, ?)'),
      endSession: db.prepare('UPDATE sessions SET ended_at = ? WHERE id = ? AND ended_at IS NULL'),
      findOpen: db.prepare('SELECT * FROM sessions WHERE ended_at IS NULL ORDER BY started_at DESC LIMIT 1'),
      findById: db.prepare('SELECT * FROM sessions WHERE id = ?'),
      list: db.prepare(`
        SELECT s.*,
               (SELECT COUNT(*) FROM suds_responses r WHERE r.session_id = s.id) AS suds_count,
               (SELECT ROUND(AVG(value), 1) FROM suds_responses r WHERE r.session_id = s.id) AS suds_average,
               (SELECT MAX(level) FROM suds_responses r WHERE r.session_id = s.id) AS max_level
        FROM sessions s ORDER BY s.started_at DESC`),
      insertEvent: db.prepare('INSERT INTO events (session_id, ts, source, type, payload) VALUES (?, ?, ?, ?, ?)'),
      eventsBySession: db.prepare('SELECT * FROM events WHERE session_id = ? ORDER BY ts, id'),
      insertSuds: db.prepare(`
        INSERT OR IGNORE INTO suds_responses
          (id, session_id, answered_at, received_at, value, level, scene, trigger, queued, height, wind, sway, fog, sound)
        VALUES
          (@id, @sessionId, @answeredAt, @receivedAt, @value, @level, @scene, @trigger, @queued, @height, @wind, @sway, @fog, @sound)`),
      sudsBySession: db.prepare('SELECT * FROM suds_responses WHERE session_id = ? ORDER BY answered_at'),
    };
  }

  createSession(patientAlias, startedAt = Date.now()) {
    const id = randomUUID();
    this.statements.insertSession.run(id, patientAlias, startedAt);
    return this.findSession(id);
  }

  endSession(id, endedAt = Date.now()) {
    this.statements.endSession.run(endedAt, id);
    return this.findSession(id);
  }

  findOpenSession() {
    return mapSession(this.statements.findOpen.get());
  }

  findSession(id) {
    return mapSession(this.statements.findById.get(id));
  }

  listSessions() {
    return this.statements.list.all().map((row) => ({
      ...mapSession(row),
      sudsCount: row.suds_count,
      sudsAverage: row.suds_average,
      maxLevel: row.max_level,
    }));
  }

  addEvent({ sessionId = null, ts = Date.now(), source, type, payload = {} }) {
    this.statements.insertEvent.run(sessionId, ts, source, type, JSON.stringify(payload));
  }

  listEvents(sessionId) {
    return this.statements.eventsBySession.all(sessionId).map((row) => ({
      id: row.id,
      ts: row.ts,
      source: row.source,
      type: row.type,
      payload: JSON.parse(row.payload),
    }));
  }

  /** Stores a SUDS response. Returns false when it was already stored (duplicates are expected after reconnects). */
  addSudsResponse(sessionId, response, receivedAt = Date.now()) {
    const result = this.statements.insertSuds.run({
      id: response.id,
      sessionId,
      answeredAt: response.answeredAt,
      receivedAt,
      value: response.value,
      level: response.level,
      scene: response.scene,
      trigger: response.trigger,
      queued: response.queued ? 1 : 0,
      ...response.stimuli,
    });
    return result.changes > 0;
  }

  listSudsResponses(sessionId) {
    return this.statements.sudsBySession.all(sessionId).map(mapSuds);
  }
}

function mapSession(row) {
  if (!row) return null;
  return {
    id: row.id,
    patientAlias: row.patient_alias,
    startedAt: row.started_at,
    endedAt: row.ended_at ?? null,
  };
}

function mapSuds(row) {
  return {
    id: row.id,
    answeredAt: row.answered_at,
    receivedAt: row.received_at,
    value: row.value,
    level: row.level,
    scene: row.scene,
    trigger: row.trigger,
    queued: row.queued === 1,
    stimuli: { height: row.height, wind: row.wind, sway: row.sway, fog: row.fog, sound: row.sound },
  };
}

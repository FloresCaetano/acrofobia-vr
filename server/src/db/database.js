import fs from 'node:fs';
import path from 'node:path';
import Database from 'better-sqlite3';

const SCHEMA = `
CREATE TABLE IF NOT EXISTS sessions (
  id            TEXT PRIMARY KEY,
  patient_alias TEXT NOT NULL,
  started_at    INTEGER NOT NULL,
  ended_at      INTEGER
);

CREATE TABLE IF NOT EXISTS events (
  id         INTEGER PRIMARY KEY AUTOINCREMENT,
  session_id TEXT REFERENCES sessions(id),
  ts         INTEGER NOT NULL,
  source     TEXT NOT NULL,
  type       TEXT NOT NULL,
  payload    TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS suds_responses (
  id          TEXT PRIMARY KEY,
  session_id  TEXT REFERENCES sessions(id),
  answered_at INTEGER NOT NULL,
  received_at INTEGER NOT NULL,
  value       INTEGER NOT NULL,
  level       INTEGER NOT NULL,
  scene       TEXT NOT NULL,
  trigger     TEXT NOT NULL,
  queued      INTEGER NOT NULL DEFAULT 0,
  height      REAL,
  wind        REAL,
  sway        REAL,
  fog         REAL,
  sound       REAL
);

CREATE INDEX IF NOT EXISTS idx_events_session ON events(session_id, ts);
CREATE INDEX IF NOT EXISTS idx_suds_session ON suds_responses(session_id, answered_at);
`;

export function openDatabase(filePath) {
  if (filePath !== ':memory:') fs.mkdirSync(path.dirname(filePath), { recursive: true });
  const db = new Database(filePath);
  db.pragma('journal_mode = WAL');
  db.pragma('foreign_keys = ON');
  db.exec(SCHEMA);
  return db;
}

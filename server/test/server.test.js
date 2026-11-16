import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import http from 'node:http';
import express from 'express';
import { WebSocketServer } from 'ws';
import { openDatabase } from '../src/db/database.js';
import { SessionRepository } from '../src/db/sessionRepository.js';
import { SessionService } from '../src/sessionService.js';
import { Hub } from '../src/hub.js';
import { createApiRouter } from '../src/api.js';
import { normalizeCommand, ProtocolError } from '../src/protocol.js';

let server;
let baseUrl;

before(async () => {
  const repository = new SessionRepository(openDatabase(':memory:'));
  const sessions = new SessionService(repository);
  const app = express();
  app.use('/api', createApiRouter({ repository, sessions }));
  server = http.createServer(app);
  new Hub({ wss: new WebSocketServer({ server }), sessions, log: { info() {}, warn() {}, error() {} } });
  await new Promise((resolve) => server.listen(0, resolve));
  baseUrl = `localhost:${server.address().port}`;
});

after(() => new Promise((resolve) => server.close(resolve)));

/** Small test client: collects every message and lets the test wait for a given type. */
function client(role, extra = {}) {
  const socket = new WebSocket(`ws://${baseUrl}`);
  const messages = [];
  const waiters = [];
  socket.addEventListener('message', (event) => {
    const message = JSON.parse(event.data);
    messages.push(message);
    for (const waiter of [...waiters]) {
      if (waiter.type === message.type && waiter.match(message)) {
        waiters.splice(waiters.indexOf(waiter), 1);
        waiter.resolve(message);
      }
    }
  });
  const ready = new Promise((resolve) => socket.addEventListener('open', resolve));
  const api = {
    socket,
    messages,
    async send(type, payload = {}) {
      await ready;
      socket.send(JSON.stringify({ type, payload, ts: Date.now() }));
    },
    next(type, match = () => true) {
      const existing = messages.find((message) => message.type === type && match(message));
      if (existing) return Promise.resolve(existing);
      return new Promise((resolve, reject) => {
        waiters.push({ type, match, resolve });
        setTimeout(() => reject(new Error(`Timeout esperando ${type}`)), 3000);
      });
    },
    close: () => new Promise((resolve) => {
      socket.addEventListener('close', () => setTimeout(resolve, 50));
      socket.close();
    }),
  };
  api.send('hello', { role, ...extra });
  return api;
}

test('normalizeCommand recorta los valores y rechaza estímulos desconocidos', () => {
  assert.deepEqual(normalizeCommand('stimulus.set', { stimulus: 'wind', value: 140 }), { stimulus: 'wind', value: 100 });
  assert.deepEqual(normalizeCommand('suds.config', { intervalSeconds: 2 }), { intervalSeconds: 10 });
  assert.throws(() => normalizeCommand('stimulus.set', { stimulus: 'rain', value: 1 }), ProtocolError);
  assert.throws(() => normalizeCommand('level.set', { level: 9 }), ProtocolError);
});

test('flujo completo: sesión, comandos, SUDS con confirmación y exportación CSV', async () => {
  const vr = client('vr', { client: 'test-vr', levels: [{ number: 1, name: 'La orilla' }] });
  await vr.next('hello');
  const console_ = client('console');
  const hello = await console_.next('hello');
  assert.equal(hello.payload.vr.client, 'test-vr');

  await console_.send('session.start', { patientAlias: '  Paciente   A ' });
  const started = await console_.next('session.start');
  assert.equal(started.payload.patientAlias, 'Paciente A');
  await vr.next('session.start');

  await console_.send('level.set', { level: 2 });
  assert.deepEqual((await vr.next('level.set')).payload, { level: 2 });

  await vr.send('state.update', { scene: 'acrophobia', level: 2, stimuli: {}, targets: {} });
  assert.equal((await console_.next('state.update')).payload.level, 2);

  const answer = { id: 'abc', value: 64, level: 2, scene: 'acrophobia', trigger: 'auto', answeredAt: Date.now(), stimuli: { height: 45 } };
  await vr.send('suds.response', answer);
  assert.equal((await vr.next('suds.ack')).payload.id, 'abc');
  assert.equal((await console_.next('suds.response')).payload.value, 64);

  // A duplicate (resent after a reconnection) is acknowledged but not stored or forwarded twice.
  await vr.send('suds.response', answer);
  await new Promise((resolve) => setTimeout(resolve, 100));
  assert.equal(console_.messages.filter((message) => message.type === 'suds.response').length, 1);

  await console_.send('session.end');
  const ended = await console_.next('session.end');
  assert.equal(ended.payload.summary.sudsCount, 1);
  assert.equal(ended.payload.summary.sudsPeak, 64);

  const bytes = Buffer.from(await (await fetch(`http://${baseUrl}/api/sessions/${started.payload.id}/export.csv`)).arrayBuffer());
  assert.deepEqual([...bytes.subarray(0, 3)], [0xef, 0xbb, 0xbf], 'UTF-8 BOM so Excel detects the encoding');
  const csv = bytes.toString('utf8');
  assert.match(csv, /^﻿tipo;fecha_hora/);
  assert.match(csv, /suds;[^\n]*;2;acrophobia;64;45/);
  assert.match(csv, /level\.set;console/);

  await vr.close();
  await console_.close();
});

test('los comandos sin VR conectada devuelven vr_offline', async () => {
  const console_ = client('console');
  await console_.next('hello');
  await console_.send('calm.trigger');
  const error = await console_.next('error');
  assert.equal(error.payload.code, 'vr_offline');
  await console_.close();
});

test('una segunda VR reemplaza a la primera con el código 4000', async () => {
  const first = client('vr');
  await first.next('hello');
  const closed = new Promise((resolve) => first.socket.addEventListener('close', (event) => resolve(event.code)));
  const second = client('vr');
  await second.next('hello');
  assert.equal(await closed, 4000);
  await second.close();
});

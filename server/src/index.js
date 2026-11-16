import http from 'node:http';
import os from 'node:os';
import path from 'node:path';
import express from 'express';
import { WebSocketServer } from 'ws';
import { config, serverRoot } from './config.js';
import { openDatabase } from './db/database.js';
import { SessionRepository } from './db/sessionRepository.js';
import { SessionService } from './sessionService.js';
import { Hub } from './hub.js';
import { createApiRouter } from './api.js';

const db = openDatabase(config.databaseFile);
const repository = new SessionRepository(db);
const sessions = new SessionService(repository);

const app = express();
app.disable('x-powered-by');
app.use('/api', createApiRouter({ repository, sessions }));
// Chart.js is served from node_modules so the console works offline during the demo.
app.use('/vendor/chart.js', express.static(path.join(serverRoot, 'node_modules/chart.js/dist')));
app.use(express.static(path.join(serverRoot, 'public')));

const server = http.createServer(app);
const wss = new WebSocketServer({ server, maxPayload: 64 * 1024 });
new Hub({ wss, sessions });

server.listen(config.port, config.host, () => {
  console.log(`\nAcrofobia VR · servidor listo en el puerto ${config.port}`);
  console.log(`  Consola del terapeuta: http://localhost:${config.port}`);
  for (const address of lanAddresses()) {
    console.log(`  Desde otra PC:         http://${address}:${config.port}  (Unity: -server ws://${address}:${config.port})`);
  }
  if (sessions.active) console.log(`  Sesión en curso recuperada: "${sessions.active.patientAlias}"`);
  console.log('');
});

function lanAddresses() {
  return Object.values(os.networkInterfaces())
    .flat()
    .filter((net) => net && net.family === 'IPv4' && !net.internal)
    .map((net) => net.address);
}

function shutdown() {
  console.log('\nCerrando el servidor…');
  wss.clients.forEach((client) => client.close(1001, 'server shutdown'));
  server.close(() => {
    db.close();
    process.exit(0);
  });
  setTimeout(() => process.exit(0), 2000).unref();
}

process.on('SIGINT', shutdown);
process.on('SIGTERM', shutdown);

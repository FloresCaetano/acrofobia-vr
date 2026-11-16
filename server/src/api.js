import express from 'express';
import { sessionToCsv } from './csv.js';

export function createApiRouter({ repository, sessions }) {
  const router = express.Router();

  router.get('/sessions', (_req, res) => {
    res.json(repository.listSessions());
  });

  router.get('/sessions/:id', (req, res) => {
    const details = sessions.details(req.params.id);
    if (!details) return res.status(404).json({ error: 'Sesión no encontrada.' });
    res.json(details);
  });

  router.get('/sessions/:id/export.csv', (req, res) => {
    const details = sessions.details(req.params.id);
    if (!details) return res.status(404).json({ error: 'Sesión no encontrada.' });

    const date = new Date(details.session.startedAt).toISOString().slice(0, 10);
    const alias = details.session.patientAlias.replace(/[^\p{L}\p{N}_-]+/gu, '_');
    res.setHeader('Content-Type', 'text/csv; charset=utf-8');
    res.setHeader('Content-Disposition', `attachment; filename="sesion_${alias}_${date}.csv"`);
    res.send(sessionToCsv(details));
  });

  return router;
}

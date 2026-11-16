import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

export const serverRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');

// Optional .env next to package.json (Node's built-in loader, no dependency needed).
const envFile = path.join(serverRoot, '.env');
if (fs.existsSync(envFile)) process.loadEnvFile(envFile);

export const config = Object.freeze({
  port: Number(process.env.PORT ?? 8080),
  host: process.env.HOST ?? '0.0.0.0',
  databaseFile: path.resolve(serverRoot, process.env.DATABASE_FILE ?? 'data/acrofobia.db'),
});

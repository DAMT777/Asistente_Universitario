import { defineConfig, loadEnv, type ProxyOptions } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath, URL } from 'node:url';
import { autenticacionDev } from './dev/autenticacionDev';

/** Prefijo de la api en el navegador cuando se usa el backend real. */
const PREFIJO_API = '/api';
const raiz = (ruta: string) => fileURLToPath(new URL(ruta, import.meta.url));

/**
 * Modos:
 * - `npm run dev`: api simulada en memoria (sin backend).
 * - `npm run dev:backend`: backend real. Vite hace de gateway local (sin CORS):
 *   /api/mis-notas -> Evaluaciones (5002) y las rutas de entregas -> Entregas (5003),
 *   y un doble de Usuarios firma el login con infra/keys. Con GATEWAY_URL todo /api va al gateway.
 */
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const backend = mode === 'backend';
  if (backend) process.env.VITE_API_URL ??= PREFIJO_API;

  const quitarPrefijo = (p: string) => p.slice(PREFIJO_API.length);
  const destino = (target: string): ProxyOptions => ({ target, changeOrigin: true, rewrite: quitarPrefijo });
  const evaluaciones = env.EVALUACIONES_URL || 'http://localhost:5002';
  const entregas = env.ENTREGAS_URL || 'http://localhost:5003';

  const rutas: Array<[RegExp, string]> = env.GATEWAY_URL
    ? [[new RegExp(`^${PREFIJO_API}/`), env.GATEWAY_URL]]
    : [
        [new RegExp(`^${PREFIJO_API}/mis-notas(/|$)`), evaluaciones],
        [new RegExp(`^${PREFIJO_API}/mis-entregas(/|$)`), entregas],
        [new RegExp(`^${PREFIJO_API}/entregas/`), entregas],
        [new RegExp(`^${PREFIJO_API}/actividades/[^/]+/entregas$`), entregas],
      ];

  return {
    plugins: [
      react(),
      ...(backend && !env.GATEWAY_URL
        ? [autenticacionDev({ prefijo: PREFIJO_API, rutaLlavePrivada: raiz('../../../infra/keys/jwt-privada.pem'), rutasProxy: rutas.map(([r]) => r) })]
        : []),
    ],
    resolve: { alias: { '@': raiz('./src') } },
    server: { proxy: backend ? Object.fromEntries(rutas.map(([r, target]) => [r.source, destino(target)])) : undefined },
    test: { environment: 'node', include: ['src/**/*.test.ts', 'dev/**/*.test.ts'] },
  };
});

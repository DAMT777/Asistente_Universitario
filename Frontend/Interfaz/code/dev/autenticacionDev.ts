/**
 * Doble del servicio de Usuarios para DESARROLLO. Solo corre dentro de `vite` (nunca en el build):
 * firma JWT RS256 con la llave privada de desarrollo (infra/keys) para los usuarios semilla,
 * con los mismos claims que emitirá Usuarios. La llave se queda en el servidor de Vite.
 */
import { createSign } from 'node:crypto';
import { existsSync, readFileSync } from 'node:fs';
import type { IncomingMessage, ServerResponse } from 'node:http';
import type { Plugin } from 'vite';

export const CONTRASENA_DEV = 'Demo1234!';
const ISSUER = 'unillanos-usuarios';
const AUDIENCE = 'unillanos-notas';
const DURACION_SEGUNDOS = 60 * 60;

type RolJwt = 'ESTUDIANTE' | 'PROFESOR';
interface UsuarioSemilla { id: string; codigo: string; nombre: string; rol: RolJwt }

/** Mismos GUID que la semilla de Evaluaciones. */
export const USUARIOS_SEMILLA: UsuarioSemilla[] = [
  { id: 'a0000000-0000-0000-0000-000000000001', codigo: 'P0001', nombre: 'Laura Rincón', rol: 'PROFESOR' },
  { id: 'b0000000-0000-0000-0000-000000000001', codigo: 'E0001', nombre: 'Ana', rol: 'ESTUDIANTE' },
  { id: 'b0000000-0000-0000-0000-000000000002', codigo: 'E0002', nombre: 'Luis', rol: 'ESTUDIANTE' },
  { id: 'b0000000-0000-0000-0000-000000000003', codigo: 'E0003', nombre: 'Marta', rol: 'ESTUDIANTE' },
  { id: 'b0000000-0000-0000-0000-000000000099', codigo: 'E0099', nombre: 'Pedro (no inscrito)', rol: 'ESTUDIANTE' },
];

const ROL_FRONT: Record<RolJwt, 'estudiante' | 'docente'> = { ESTUDIANTE: 'estudiante', PROFESOR: 'docente' };

const b64url = (b: Buffer | string) => Buffer.from(b).toString('base64url');

export function firmarToken(u: UsuarioSemilla, llavePrivadaPem: string, ahora = Date.now()): string {
  const iat = Math.floor(ahora / 1000);
  const encabezado = b64url(JSON.stringify({ alg: 'RS256', typ: 'JWT' }));
  const carga = b64url(JSON.stringify({ sub: u.id, rol: u.rol, nombre: u.nombre, iss: ISSUER, aud: AUDIENCE, iat, nbf: iat, exp: iat + DURACION_SEGUNDOS }));
  const firma = createSign('RSA-SHA256').update(`${encabezado}.${carga}`).sign(llavePrivadaPem);
  return `${encabezado}.${carga}.${b64url(firma)}`;
}

export interface ResultadoLogin { status: number; cuerpo: unknown }

/** POST /auth/login: { usuario, contrasena, rol } -> { token, usuario } con el rol en minúsculas que usa el frontend. */
export function iniciarSesion(entrada: unknown, llavePrivadaPem: string): ResultadoLogin {
  const { usuario, contrasena, rol } = (entrada ?? {}) as Record<string, unknown>;
  const u = USUARIOS_SEMILLA.find((x) => x.codigo === String(usuario ?? '').trim().toUpperCase());
  if (!u || contrasena !== CONTRASENA_DEV || ROL_FRONT[u.rol] !== rol) {
    return { status: 401, cuerpo: error(401, 'NO_AUTENTICADO', 'Usuario o contraseña incorrectos.') };
  }
  return {
    status: 200,
    cuerpo: { token: firmarToken(u, llavePrivadaPem), usuario: { id: u.id, nombre: u.nombre, codigo: u.codigo, rol: ROL_FRONT[u.rol] } },
  };
}

const error = (status: number, codigo: string, mensaje: string) => ({ status, codigo, mensaje, traceId: 'vite-dev' });

function responder(res: ServerResponse, status: number, cuerpo?: unknown) {
  res.statusCode = status;
  if (cuerpo === undefined) return res.end();
  res.setHeader('Content-Type', status >= 400 ? 'application/problem+json' : 'application/json');
  res.end(JSON.stringify(cuerpo));
}

async function leerJson(req: IncomingMessage): Promise<unknown> {
  let texto = '';
  for await (const parte of req) texto += parte;
  try { return JSON.parse(texto); } catch { return null; }
}

export interface OpcionesAutenticacionDev {
  /** Prefijo de la api en el navegador, p. ej. "/api". */
  prefijo: string;
  rutaLlavePrivada: string;
  /** Rutas bajo el prefijo que atiende el proxy; las demás responden 501 en vez de devolver index.html. */
  rutasProxy: RegExp[];
}

export function autenticacionDev({ prefijo, rutaLlavePrivada, rutasProxy }: OpcionesAutenticacionDev): Plugin {
  return {
    name: 'unillanos-autenticacion-dev',
    apply: 'serve',
    configureServer(server) {
      // Registrado aquí corre antes que el proxy de Vite: atiende /auth y deja pasar las rutas proxificadas.
      server.middlewares.use(async (req, res, next) => {
        const ruta = (req.url ?? '').split('?')[0];
        if (!ruta.startsWith(`${prefijo}/`)) return next();
        const relativa = ruta.slice(prefijo.length);

        if (relativa === '/auth/login' && req.method === 'POST') {
          if (!existsSync(rutaLlavePrivada)) {
            return responder(res, 500, error(500, 'ERROR_INTERNO', 'Falta la llave de desarrollo: ejecute infra/scripts/generar-llaves.sh.'));
          }
          const r = iniciarSesion(await leerJson(req), readFileSync(rutaLlavePrivada, 'utf8'));
          return responder(res, r.status, r.cuerpo);
        }
        if (relativa === '/auth/logout' && req.method === 'POST') return responder(res, 204);
        if (rutasProxy.some((r) => r.test(ruta))) return next();
        return responder(res, 501, error(501, 'NO_IMPLEMENTADO', 'Esta función aún no está disponible en el backend.'));
      });
    },
  };
}

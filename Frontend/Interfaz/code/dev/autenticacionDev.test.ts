import { generateKeyPairSync, createVerify } from 'node:crypto';
import { describe, expect, it } from 'vitest';
import { CONTRASENA_DEV, iniciarSesion } from './autenticacionDev';

const { privateKey, publicKey } = generateKeyPairSync('rsa', { modulusLength: 2048 });
const privada = privateKey.export({ type: 'pkcs8', format: 'pem' }).toString();

const decodificar = (parte: string) => JSON.parse(Buffer.from(parte, 'base64url').toString());

describe('autenticacionDev', () => {
  it('emite un JWT RS256 con los claims de Usuarios, verificable con la llave pública', () => {
    const r = iniciarSesion({ usuario: 'e0001', contrasena: CONTRASENA_DEV, rol: 'estudiante' }, privada);
    expect(r.status).toBe(200);
    const { token, usuario } = r.cuerpo as { token: string; usuario: { rol: string; nombre: string } };
    expect(usuario).toMatchObject({ rol: 'estudiante', nombre: 'Ana' });

    const [h, p, f] = token.split('.');
    expect(decodificar(h)).toEqual({ alg: 'RS256', typ: 'JWT' });
    const claims = decodificar(p);
    expect(claims).toMatchObject({ sub: 'b0000000-0000-0000-0000-000000000001', rol: 'ESTUDIANTE', iss: 'unillanos-usuarios', aud: 'unillanos-notas' });
    expect(claims.exp - claims.iat).toBe(3600);
    expect(createVerify('RSA-SHA256').update(`${h}.${p}`).verify(publicKey, Buffer.from(f, 'base64url'))).toBe(true);
  });

  it.each([
    [{ usuario: 'E0001', contrasena: 'otra', rol: 'estudiante' }],
    [{ usuario: 'E0001', contrasena: CONTRASENA_DEV, rol: 'docente' }],
    [{ usuario: 'NADIE', contrasena: CONTRASENA_DEV, rol: 'estudiante' }],
    [null],
  ])('rechaza credenciales inválidas con 401 NO_AUTENTICADO (%j)', (entrada) => {
    const r = iniciarSesion(entrada, privada);
    expect(r.status).toBe(401);
    expect(r.cuerpo).toMatchObject({ codigo: 'NO_AUTENTICADO' });
  });
});

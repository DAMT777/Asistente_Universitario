import { useState, type FormEvent } from 'react';
import { Navigate } from 'react-router-dom';
import type { Rol } from '@/types';
import { useSesion } from '@/hooks';
import { colors, font } from '@/theme/tokens';

const USUARIO_DEMO: Record<Rol, string> = { estudiante: '160005017', docente: 'lrincon' };

export function LoginPage() {
  const { sesion, login } = useSesion();
  const [rol, setRol] = useState<Rol | null>(null);
  const [usuario, setUsuario] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [errores, setErrores] = useState<Record<string, string>>({});
  const [enviando, setEnviando] = useState(false);

  if (sesion) return <Navigate to={sesion.usuario.rol === 'docente' ? '/d' : '/e'} replace />;

  const elegir = (r: Rol) => { setRol(r); setUsuario(USUARIO_DEMO[r]); setErrores({}); };

  async function enviar(e: FormEvent) {
    e.preventDefault();
    if (!rol) return;
    setEnviando(true);
    const r = await login({ usuario, contrasena, rol });
    setEnviando(false);
    if (!r.ok) setErrores(r.errores);
  }

  const btn = { height: 48, borderRadius: 6, fontSize: 15, fontWeight: 600, letterSpacing: '0.06em', color: '#FFFFFF' } as const;
  const linea = { width: '100%', height: 40, padding: '0 4px', border: 0, borderBottom: '1px solid rgba(255,255,255,0.75)', background: 'transparent', color: '#FFFFFF', fontSize: 17, textAlign: 'center', outline: 0 } as const;
  const etiqueta = { display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 8, fontSize: 12, fontWeight: 500, letterSpacing: '0.1em', color: 'rgba(255,255,255,0.85)' } as const;
  const err = { color: '#FFD4D0', fontSize: 13, letterSpacing: 0 } as const;

  return (
    <div style={{ position: 'relative', minHeight: '100%', overflow: 'hidden', background: '#2A1A1A', color: '#FFFFFF' }}>
      <div style={{ position: 'absolute', inset: -24, backgroundImage: 'url(/campus.webp)', backgroundSize: 'cover', backgroundPosition: '62% center', filter: rol ? 'blur(14px)' : 'none', transition: 'filter 400ms ease' }} />
      <div style={{ position: 'absolute', inset: 0, background: 'linear-gradient(180deg,rgba(40,14,18,0.55) 0%,rgba(110,18,26,0.35) 45%,rgba(28,12,14,0.85) 100%)' }} />
      <div style={{ position: 'relative', minHeight: '100vh', display: 'flex', flexDirection: 'column', alignItems: 'center', padding: '72px 28px 32px' }}>
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 8, textAlign: 'center' }}>
          <h1 style={{ margin: 0, fontSize: 34, fontWeight: font.weight.light, letterSpacing: '0.08em', lineHeight: 1.1 }}>AULA UNILLANOS</h1>
          <p style={{ margin: 0, fontSize: 14, color: 'rgba(255,255,255,0.88)' }}>Actividades, entregas y notas en un solo lugar.</p>
        </div>

        {!rol ? (
          <div style={{ marginTop: 'auto', width: '100%', maxWidth: 320, display: 'flex', flexDirection: 'column', gap: 12 }}>
            <button onClick={() => elegir('estudiante')} style={{ ...btn, border: 0, background: colors.marca }}>INGRESAR COMO ESTUDIANTE</button>
            <button onClick={() => elegir('docente')} style={{ ...btn, border: '1px solid rgba(255,255,255,0.7)', background: 'rgba(255,255,255,0.08)' }}>INGRESAR COMO DOCENTE</button>
            <p style={{ margin: '8px 0 0', fontSize: 12, color: 'rgba(255,255,255,0.75)', textAlign: 'center' }}>Universidad de los Llanos · Villavicencio</p>
          </div>
        ) : (
          <form onSubmit={enviar} noValidate style={{ marginTop: 48, width: '100%', maxWidth: 320, flex: 1, display: 'flex', flexDirection: 'column', gap: 28 }}>
            <div style={{ textAlign: 'center', fontSize: 13, fontWeight: 600, letterSpacing: '0.08em', color: 'rgba(255,255,255,0.85)' }}>{rol === 'docente' ? 'INGRESO DOCENTE' : 'INGRESO ESTUDIANTE'}</div>
            <label style={etiqueta}>USUARIO
              <input value={usuario} onChange={(e) => setUsuario(e.target.value)} autoComplete="username" style={linea} />
              {errores.usuario && <span style={err}>{errores.usuario}</span>}
            </label>
            <label style={etiqueta}>CONTRASEÑA
              <input type="password" value={contrasena} onChange={(e) => setContrasena(e.target.value)} autoComplete="current-password" style={{ ...linea, letterSpacing: '0.2em' }} />
              {errores.contrasena && <span style={err}>{errores.contrasena}</span>}
            </label>
            {errores._ && <span style={{ ...err, textAlign: 'center' }}>{errores._}</span>}
            <div style={{ marginTop: 'auto', display: 'flex', flexDirection: 'column', gap: 12 }}>
              <button type="submit" disabled={enviando} style={{ ...btn, border: 0, background: colors.marca, opacity: enviando ? 0.7 : 1 }}>{enviando ? 'INGRESANDO…' : 'INGRESAR'}</button>
              <button type="button" onClick={() => { setRol(null); setContrasena(''); }} style={{ height: 40, border: 0, background: 'none', color: 'rgba(255,255,255,0.85)', fontSize: 14 }}>Volver</button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}

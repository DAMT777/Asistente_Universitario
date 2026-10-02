import { useEffect, useRef, useState, type CSSProperties, type FormEvent } from 'react';
import { Navigate, useSearchParams } from 'react-router-dom';
import { useSesion } from '@/hooks';
import { Icono, conOnda } from '@/ui';

const ic = {
  usuario: 'M20 21v-1a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v1M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8Z',
  candado: 'M5 11h14v10H5zM8 11V7a4 4 0 0 1 8 0v4',
  ojo: 'M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12ZM12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z',
  ojoCerrado: 'M3 3l18 18M10.6 10.6a3 3 0 0 0 2.8 2.8M9.9 5.2A10 10 0 0 1 12 5c6.5 0 10 7 10 7a17 17 0 0 1-3.2 4.2M6.1 6.1A16.7 16.7 0 0 0 2 12s3.5 7 10 7a9.7 9.7 0 0 0 4.2-.9',
} as const;

/** Índice de entrada escalonada (lo lee la animación de global.css). */
const orden = (n: number) => ({ '--i': n }) as CSSProperties;

/**
 * CU-01. Un solo formulario de usuario y contraseña: el rol no se elige, lo decide el servicio de usuarios
 * según la cuenta y con él se entra al espacio del docente o del estudiante.
 */
export function LoginPage() {
  const { sesion, login } = useSesion();
  const [usuario, setUsuario] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [verContrasena, setVerContrasena] = useState(false);
  const [errores, setErrores] = useState<Record<string, string>>({});
  const [enviando, setEnviando] = useState(false);
  const usuarioRef = useRef<HTMLInputElement>(null);
  const modoDemo = !import.meta.env.VITE_API_URL;
  const [params] = useSearchParams();
  const sesionVencida = params.get('sesion') === 'vencida';

  // Enfoca el usuario cuando termina de entrar el formulario.
  useEffect(() => {
    const t = window.setTimeout(() => usuarioRef.current?.focus(), 460);
    return () => window.clearTimeout(t);
  }, []);

  if (sesion) return <Navigate to={sesion.usuario.rol === 'docente' ? '/d' : '/e'} replace />;

  async function enviar(e: FormEvent) {
    e.preventDefault();
    setEnviando(true);
    const r = await login({ usuario, contrasena });
    setEnviando(false);
    if (!r.ok) setErrores(r.errores);
  }

  return (
    <div className="au-login au-escalable">
      <section className="au-login-hero" aria-label="Presentación de Aula Unillanos">
        <div className="au-login-foto" aria-hidden="true" />
        <div className="au-login-marca au-item" style={orden(0)}>
          <span className="au-login-logo"><img src="/logo-unillanos.png" alt="Universidad de los Llanos" width={44} height={42} /></span>
          <span className="au-login-inst">Universidad<br />de los Llanos</span>
        </div>
        <h1 className="au-login-titulo au-item" style={orden(1)}>AULA <strong>UNILLANOS</strong></h1>
        <p className="au-login-pie au-item" style={orden(2)}>Villavicencio · Meta</p>
      </section>

      <main className="au-login-panel">
        <div className="au-login-caja">
          <form className="au-paso au-form" data-activo="true" onSubmit={enviar} noValidate aria-label="Ingreso a Aula Unillanos">
            <h2 className="au-paso-titulo au-item" style={orden(0)}>Ingresa a <strong>tu cuenta</strong></h2>

            <div className="au-item" style={orden(1)}>
              <div className="au-campo-flotante" data-error={!!errores.usuario}>
                <span className="au-campo-icono"><Icono d={ic.usuario} tam={20} /></span>
                <input id="login-usuario" ref={usuarioRef} placeholder=" " value={usuario} onChange={(e) => setUsuario(e.target.value)} autoComplete="username" aria-invalid={!!errores.usuario} aria-describedby={errores.usuario ? 'login-usuario-error' : undefined} />
                <label htmlFor="login-usuario">Usuario</label>
              </div>
              {errores.usuario && <span id="login-usuario-error" role="alert" className="au-login-error" style={{ marginTop: 6 }}>{errores.usuario}</span>}
            </div>

            <div className="au-item" style={orden(2)}>
              <div className="au-campo-flotante" data-error={!!errores.contrasena}>
                <span className="au-campo-icono"><Icono d={ic.candado} tam={20} /></span>
                <input id="login-contrasena" placeholder=" " type={verContrasena ? 'text' : 'password'} value={contrasena} onChange={(e) => setContrasena(e.target.value)} autoComplete="current-password" aria-invalid={!!errores.contrasena} aria-describedby={errores.contrasena ? 'login-contrasena-error' : undefined} />
                <label htmlFor="login-contrasena">Contraseña</label>
                <button type="button" className="au-ojo" aria-label={verContrasena ? 'Ocultar contraseña' : 'Mostrar contraseña'} aria-pressed={verContrasena} onClick={() => setVerContrasena((v) => !v)}><Icono d={verContrasena ? ic.ojoCerrado : ic.ojo} tam={20} /></button>
              </div>
              {errores.contrasena && <span id="login-contrasena-error" role="alert" className="au-login-error" style={{ marginTop: 6 }}>{errores.contrasena}</span>}
            </div>

            {errores._ && <span role="alert" className="au-login-error au-login-error--general au-item" style={orden(3)}>{errores._}</span>}
            {sesionVencida && <p role="status" className="au-login-nota au-item" style={orden(3)}>Tu sesión expiró o ya no es válida. Inicia sesión de nuevo para continuar.</p>}
            {modoDemo && <div className="au-demo au-item" style={orden(3)}>Modo demostración · docente <strong>lrincon</strong>, estudiante <strong>160005017</strong> · contraseña <strong>demo123</strong></div>}

            <button type="submit" disabled={enviando} onPointerDown={conOnda()} data-onda="clara" className="au-onda-host au-enviar au-item" style={orden(4)}>
              {enviando ? <><span className="au-giro" aria-hidden="true" />INGRESANDO…</> : 'INGRESAR'}
            </button>
          </form>
        </div>
      </main>
    </div>
  );
}

import { useEffect, useRef, useState, type CSSProperties, type FormEvent, type PointerEvent } from 'react';
import { Navigate, useSearchParams } from 'react-router-dom';
import { useSesion } from '@/hooks';
import { Icono, icono, conOnda } from '@/ui';

const ic = {
  usuario: 'M20 21v-1a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v1M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8Z',
  candado: 'M5 11h14v10H5zM8 11V7a4 4 0 0 1 8 0v4',
  ojo: 'M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12ZM12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z',
  ojoCerrado: 'M3 3l18 18M10.6 10.6a3 3 0 0 0 2.8 2.8M9.9 5.2A10 10 0 0 1 12 5c6.5 0 10 7 10 7a17 17 0 0 1-3.2 4.2M6.1 6.1A16.7 16.7 0 0 0 2 12s3.5 7 10 7a9.7 9.7 0 0 0 4.2-.9',
  flecha: 'M5 12h14M13 6l6 6-6 6',
  tarea: 'M9 11l3 3 8-8M20 12v7a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h9',
  calculadora: 'M6 3h12v18H6zM9 7h6M9 12h.01M12 12h.01M15 12h.01M9 16h.01M12 16h.01M15 16h.01',
} as const;

/** Índice de entrada escalonada (lo lee la animación de global.css). */
const orden = (n: number) => ({ '--i': n }) as CSSProperties;

/** Garza en vuelo: silueta que cruza el panel de marca batiendo las alas. */
function Garza({ className }: { className: string }) {
  return (
    <svg className={className} viewBox="0 0 200 90" aria-hidden="true">
      <g className="au-garza-cuerpo">
        <path d="M24 56 L76 52 M26 60 L78 55" strokeWidth="2.2" strokeLinecap="round" fill="none" stroke="currentColor" />
        <path d="M70 52 C 88 43, 116 43, 132 50 C 120 59, 90 61, 70 52 Z" fill="currentColor" />
        <path d="M128 49 C 142 52, 136 37, 150 39" strokeWidth="5" strokeLinecap="round" fill="none" stroke="currentColor" />
        <circle cx="152" cy="39" r="4.5" fill="currentColor" />
        <path d="M155 37 L182 41 L155 42 Z" fill="currentColor" />
      </g>
      <path className="au-garza-ala" d="M90 49 C 84 30, 66 14, 34 6 C 54 22, 66 30, 74 37 C 64 35, 56 35, 48 37 C 70 43, 94 47, 116 48 Z" fill="currentColor" />
    </svg>
  );
}

export function LoginPage() {
  const { sesion, login } = useSesion();
  const [usuario, setUsuario] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [verContrasena, setVerContrasena] = useState(false);
  const [errores, setErrores] = useState<Record<string, string>>({});
  const [enviando, setEnviando] = useState(false);
  // El agua del botón sube la primera vez que se pasa encima y ya no se vacía.
  const [lleno, setLleno] = useState(false);
  const usuarioRef = useRef<HTMLInputElement>(null);
  const [params] = useSearchParams();
  const sesionVencida = params.get('sesion') === 'vencida';

  // Enfoca el usuario cuando termina de entrar el formulario.
  useEffect(() => {
    const t = window.setTimeout(() => usuarioRef.current?.focus(), 520);
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

  // Luz que sigue al puntero sobre el panel del formulario.
  const moverLuz = (e: PointerEvent<HTMLElement>) => {
    const r = e.currentTarget.getBoundingClientRect();
    e.currentTarget.style.setProperty('--mx', `${e.clientX - r.left}px`);
    e.currentTarget.style.setProperty('--my', `${e.clientY - r.top}px`);
  };

  return (
    <div className="au-login au-escalable">
      <section className="au-login-hero" aria-label="Presentación de Sirius Unillanos">
        <div className="au-login-foto" aria-hidden="true" />
        <div className="au-garzas" aria-hidden="true">
          <Garza className="au-garza" />
          <Garza className="au-garza au-garza--lejos" />
        </div>
        <div className="au-login-marca au-item" style={orden(0)}>
          <span className="au-login-logo"><img src="/logo-unillanos.png" alt="Universidad de los Llanos" width={44} height={42} /></span>
          <span className="au-login-inst">Universidad<br />de los Llanos</span>
        </div>
        <div>
          <h1 className="au-login-titulo au-item" style={orden(1)}>SIRIUS <strong>UNILLANOS</strong></h1>
          <p className="au-login-lema au-item" style={orden(2)}>Actividades, entregas y notas en un solo lugar.</p>
        </div>
        <ul className="au-login-lista">
          <li className="au-item" style={orden(3)}><span className="au-chip"><Icono d={ic.tarea} tam={20} /></span>Actividades y entregas con fecha límite</li>
          <li className="au-item" style={orden(4)}><span className="au-chip"><Icono d={icono.notas} tam={20} /></span>Notas publicadas por corte</li>
          <li className="au-item" style={orden(5)}><span className="au-chip"><Icono d={ic.calculadora} tam={20} /></span>Ponderado calculado por el sistema</li>
        </ul>
        <p className="au-login-pie au-item" style={orden(6)}>Villavicencio · Meta</p>
      </section>

      <main className="au-login-panel" onPointerMove={moverLuz}>
        <div className="au-login-luz" aria-hidden="true" />

        <div className="au-login-caja">
          <form className="au-form" onSubmit={enviar} noValidate aria-label="Iniciar sesión">
            <h2 className="au-paso-titulo au-item" style={orden(0)}>Bienvenido a <strong className="au-brillo-texto">tu aula</strong></h2>
            <p className="au-paso-sub au-item" style={orden(1)}>Ingresa con tu usuario institucional.</p>

            <div className="au-item" style={orden(2)}>
              <div className="au-campo-flotante" data-error={!!errores.usuario}>
                <span className="au-campo-icono"><Icono d={ic.usuario} tam={20} /></span>
                <input id="login-usuario" ref={usuarioRef} placeholder=" " value={usuario} onChange={(e) => setUsuario(e.target.value)} autoComplete="username" aria-invalid={!!errores.usuario} aria-describedby={errores.usuario ? 'login-usuario-error' : undefined} />
                <label htmlFor="login-usuario">Usuario o código</label>
                <span className="au-campo-linea" aria-hidden="true" />
              </div>
              {errores.usuario && <span id="login-usuario-error" role="alert" className="au-login-error" style={{ marginTop: 6 }}>{errores.usuario}</span>}
            </div>

            <div className="au-item" style={orden(3)}>
              <div className="au-campo-flotante" data-error={!!errores.contrasena}>
                <span className="au-campo-icono"><Icono d={ic.candado} tam={20} /></span>
                <input id="login-contrasena" placeholder=" " type={verContrasena ? 'text' : 'password'} value={contrasena} onChange={(e) => setContrasena(e.target.value)} autoComplete="current-password" aria-invalid={!!errores.contrasena} aria-describedby={errores.contrasena ? 'login-contrasena-error' : undefined} />
                <label htmlFor="login-contrasena">Contraseña</label>
                <button type="button" className="au-ojo" aria-label={verContrasena ? 'Ocultar contraseña' : 'Mostrar contraseña'} aria-pressed={verContrasena} onClick={() => setVerContrasena((v) => !v)}><Icono d={verContrasena ? ic.ojoCerrado : ic.ojo} tam={20} /></button>
                <span className="au-campo-linea" aria-hidden="true" />
              </div>
              {errores.contrasena && <span id="login-contrasena-error" role="alert" className="au-login-error" style={{ marginTop: 6 }}>{errores.contrasena}</span>}
            </div>

            {errores._ && <span role="alert" className="au-login-error au-login-error--general au-item" style={orden(4)}>{errores._}</span>}
            {sesionVencida && <p role="status" className="au-login-nota au-item" style={orden(4)}>Tu sesión expiró o ya no es válida. Inicia sesión de nuevo para continuar.</p>}

            {/* El contenedor lleva la animación de entrada para que el hover del botón no la reinicie. */}
            <div className="au-item" style={orden(5)}>
              <button type="submit" disabled={enviando} onPointerDown={conOnda()} onPointerEnter={() => setLleno(true)} onFocus={() => setLleno(true)} data-onda="clara" data-lleno={lleno || enviando} className="au-onda-host au-enviar">
                <span className="au-liquido" aria-hidden="true" />
                <span className="au-enviar-texto">
                  {enviando ? <><span className="au-giro" aria-hidden="true" />INGRESANDO…</> : <>INGRESAR<Icono d={ic.flecha} tam={20} /></>}
                </span>
              </button>
            </div>
          </form>
        </div>
      </main>
    </div>
  );
}

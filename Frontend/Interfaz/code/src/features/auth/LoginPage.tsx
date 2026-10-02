import { useCallback, useEffect, useRef, useState, type CSSProperties, type FormEvent } from 'react';
import { Navigate, useSearchParams } from 'react-router-dom';
import type { Rol } from '@/types';
import { useSesion } from '@/hooks';
import { Icono, icono, conOnda } from '@/ui';

type Paso = 'seleccion' | 'formulario';

const USUARIO_DEMO: Record<Rol, string> = { estudiante: '160005017', docente: 'lrincon' };

const ic = {
  estudiante: 'M22 10 12 5 2 10l10 5 10-5ZM6 12v5c3 2.5 9 2.5 12 0v-5',
  docente: 'M3 4h18v12H3zM8 20h8M12 16v4',
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

const ROLES: { rol: Rol; titulo: string; detalle: string }[] = [
  { rol: 'estudiante', titulo: 'Soy estudiante', detalle: 'Consulta tus actividades, sube entregas y revisa tus notas por corte.' },
  { rol: 'docente', titulo: 'Soy docente', detalle: 'Crea actividades, califica y publica las notas de cada corte.' },
];

export function LoginPage() {
  const { sesion, login } = useSesion();
  const [paso, setPaso] = useState<Paso>('seleccion');
  const [rol, setRol] = useState<Rol>('estudiante');
  const [usuario, setUsuario] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [verContrasena, setVerContrasena] = useState(false);
  const [errores, setErrores] = useState<Record<string, string>>({});
  const [params] = useSearchParams();
  const sesionVencida = params.get('sesion') === 'vencida';
  const [enviando, setEnviando] = useState(false);
  const usuarioRef = useRef<HTMLInputElement>(null);
  const contrasenaRef = useRef<HTMLInputElement>(null);
  const modoDemo = !import.meta.env.VITE_API_URL;

  const volver = useCallback(() => {
    setPaso('seleccion');
    setContrasena('');
    setVerContrasena(false);
    setErrores({});
  }, []);

  // Esc regresa a la selección de rol, igual que el botón Volver.
  useEffect(() => {
    if (paso !== 'formulario') return;
    const alPulsar = (e: KeyboardEvent) => { if (e.key === 'Escape') volver(); };
    document.addEventListener('keydown', alPulsar);
    return () => document.removeEventListener('keydown', alPulsar);
  }, [paso, volver]);

  // Enfoca el campo útil cuando termina de entrar el formulario.
  useEffect(() => {
    if (paso !== 'formulario') return;
    const t = window.setTimeout(() => (usuarioRef.current?.value ? contrasenaRef : usuarioRef).current?.focus(), 460);
    return () => window.clearTimeout(t);
  }, [paso]);

  if (sesion) return <Navigate to={sesion.usuario.rol === 'docente' ? '/d' : '/e'} replace />;

  const elegir = (r: Rol) => {
    setRol(r);
    setUsuario(modoDemo ? USUARIO_DEMO[r] : '');
    setErrores({});
    setPaso('formulario');
  };

  async function enviar(e: FormEvent) {
    e.preventDefault();
    setEnviando(true);
    const r = await login({ usuario, contrasena, rol });
    setEnviando(false);
    if (!r.ok) setErrores(r.errores);
  }

  const esDocente = rol === 'docente';
  const etiquetaRol = esDocente ? 'Docente' : 'Estudiante';

  return (
    <div className="au-login au-escalable" data-paso={paso}>
      <section className="au-login-hero" aria-label="Presentación de Aula Unillanos">
        <div className="au-login-foto" aria-hidden="true" />
        <div className="au-login-marca au-item" style={orden(0)}>
          <span className="au-login-logo"><img src="/logo-unillanos.png" alt="Universidad de los Llanos" width={44} height={42} /></span>
          <span className="au-login-inst">Universidad<br />de los Llanos</span>
        </div>
        <div>
          <h1 className="au-login-titulo au-item" style={orden(1)}>AULA <strong>UNILLANOS</strong></h1>
          <p className="au-login-lema au-item" style={orden(2)}>Actividades, entregas y notas en un solo lugar.</p>
        </div>
        <ul className="au-login-lista">
          <li className="au-item" style={orden(3)}><span className="au-chip"><Icono d={ic.tarea} tam={20} /></span>Actividades y entregas con fecha límite</li>
          <li className="au-item" style={orden(4)}><span className="au-chip"><Icono d={icono.notas} tam={20} /></span>Notas publicadas por corte</li>
          <li className="au-item" style={orden(5)}><span className="au-chip"><Icono d={ic.calculadora} tam={20} /></span>Ponderado calculado por el sistema</li>
        </ul>
        <p className="au-login-pie au-item" style={orden(6)}>Villavicencio · Meta</p>
      </section>

      <main className="au-login-panel">
        <div className="au-login-caja">
          {/* Paso 1: elegir cómo ingresar */}
          <div className="au-paso" data-activo={paso === 'seleccion'} data-lado="izq">
            <h2 className="au-paso-titulo au-item" style={orden(0)}>Bienvenido a <strong>tu aula</strong></h2>
            <p className="au-paso-sub au-item" style={orden(1)}>Elige cómo quieres ingresar.</p>
            {ROLES.map((r, i) => (
              <button key={r.rol} type="button" className="au-rol au-item" style={orden(2 + i)} onClick={() => elegir(r.rol)}>
                <span className="au-rol-icono"><Icono d={r.rol === 'docente' ? ic.docente : ic.estudiante} tam={26} /></span>
                <span className="au-rol-texto"><strong>{r.titulo}</strong><span>{r.detalle}</span></span>
                <span className="au-rol-flecha"><Icono d={ic.flecha} tam={22} /></span>
              </button>
            ))}
            {sesionVencida && <p role="status" className="au-login-nota au-item" style={orden(4)}>Tu sesión expiró o ya no es válida. Inicia sesión de nuevo para continuar.</p>}
            {modoDemo && <p className="au-login-nota au-item" style={orden(4)}>Demostración con datos semilla. Matrícula e integración con SIAU fuera del alcance. Contraseña de prueba: demo123.</p>}
          </div>

          {/* Paso 2: credenciales */}
          <form className="au-paso au-form" data-activo={paso === 'formulario'} onSubmit={enviar} noValidate aria-label={`Ingreso ${etiquetaRol.toLowerCase()}`}>
            <button type="button" className="au-volver au-item" style={orden(0)} onClick={volver}><Icono d={icono.atras} tam={18} />Volver</button>
            <span className="au-rol-chip au-item" style={orden(1)}><Icono d={esDocente ? ic.docente : ic.estudiante} tam={16} />{etiquetaRol}</span>
            <h2 className="au-paso-titulo au-item" style={orden(2)}>Ingresa a <strong>tu cuenta</strong></h2>

            <div className="au-item" style={orden(3)}>
              <div className="au-campo-flotante" data-error={!!errores.usuario}>
                <span className="au-campo-icono"><Icono d={ic.usuario} tam={20} /></span>
                <input id="login-usuario" ref={usuarioRef} placeholder=" " value={usuario} onChange={(e) => setUsuario(e.target.value)} autoComplete="username" aria-invalid={!!errores.usuario} aria-describedby={errores.usuario ? 'login-usuario-error' : undefined} />
                <label htmlFor="login-usuario">Usuario</label>
              </div>
              {errores.usuario && <span id="login-usuario-error" role="alert" className="au-login-error" style={{ marginTop: 6 }}>{errores.usuario}</span>}
            </div>

            <div className="au-item" style={orden(4)}>
              <div className="au-campo-flotante" data-error={!!errores.contrasena}>
                <span className="au-campo-icono"><Icono d={ic.candado} tam={20} /></span>
                <input id="login-contrasena" ref={contrasenaRef} placeholder=" " type={verContrasena ? 'text' : 'password'} value={contrasena} onChange={(e) => setContrasena(e.target.value)} autoComplete="current-password" aria-invalid={!!errores.contrasena} aria-describedby={errores.contrasena ? 'login-contrasena-error' : undefined} />
                <label htmlFor="login-contrasena">Contraseña</label>
                <button type="button" className="au-ojo" aria-label={verContrasena ? 'Ocultar contraseña' : 'Mostrar contraseña'} aria-pressed={verContrasena} onClick={() => setVerContrasena((v) => !v)}><Icono d={verContrasena ? ic.ojoCerrado : ic.ojo} tam={20} /></button>
              </div>
              {errores.contrasena && <span id="login-contrasena-error" role="alert" className="au-login-error" style={{ marginTop: 6 }}>{errores.contrasena}</span>}
            </div>

            {errores._ && <span role="alert" className="au-login-error au-login-error--general au-item" style={orden(5)}>{errores._}</span>}
            {modoDemo && <div className="au-demo au-item" style={orden(5)}>Modo demostración · contraseña: <strong>demo123</strong></div>}

            <button type="submit" disabled={enviando} onPointerDown={conOnda()} data-onda="clara" className="au-onda-host au-enviar au-item" style={orden(6)}>
              {enviando ? <><span className="au-giro" aria-hidden="true" />INGRESANDO…</> : 'INGRESAR'}
            </button>
          </form>
        </div>
      </main>
    </div>
  );
}

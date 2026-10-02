import type { CSSProperties } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useSesion, useUsuario } from '@/hooks';
import { iniciales } from '@/domain';
import { colors, font, size, space, web } from '@/theme/tokens';
import { Icono, icono, conOnda } from '@/ui';
import { useIsWide } from '@/ui/useIsWide';
import { useVidrio } from '@/ui/tema';

export function AppShell() {
  const wide = useIsWide();
  const u = useUsuario();
  const { logout } = useSesion();
  const nav = useNavigate();
  const [vidrio, alternarVidrio] = useVidrio();
  const base = u.rol === 'docente' ? '/d' : '/e';
  const items = [
    { to: base, end: true, label: 'Inicio', d: icono.inicio },
    { to: `${base}/cursos`, end: false, label: 'Cursos', d: icono.cursos },
    ...(u.rol === 'estudiante' ? [{ to: '/e/notas', end: false, label: 'Notas', d: icono.notas }] : []),
  ];

  const navStyle = (activo: boolean, movil: boolean): CSSProperties => ({
    display: 'flex', alignItems: 'center', gap: movil ? 4 : 12, flexDirection: movil ? 'column' : 'row', justifyContent: movil ? 'center' : 'flex-start',
    flex: movil ? 1 : undefined, height: movil ? 52 : 46, padding: movil ? 0 : '0 14px', borderRadius: 12,
    fontSize: movil ? 11 : 14, fontWeight: font.weight.semibold,
    background: activo ? `linear-gradient(135deg,${colors.marca},${colors.marcaHover})` : 'transparent',
    color: activo ? colors.sobreMarca : colors.textoMedio, boxShadow: activo && !movil ? web.sombraMarca : 'none',
  });

  const botonIcono: CSSProperties = { width: 38, height: 38, borderRadius: 10, border: `1px solid ${colors.lineaFuerte}`, backgroundColor: colors.blanco, color: colors.textoMedio, display: 'flex', alignItems: 'center', justifyContent: 'center' };

  return (
    <div style={{ position: 'relative', height: '100dvh', display: 'flex', flexDirection: 'column', background: colors.fondo, color: colors.texto, overflow: 'hidden' }}>
      <div style={{ position: 'absolute', inset: 0, background: web.fondoApp }} />
      {/* Modo vidrio: la foto del campus queda detrás y las superficies se vuelven translúcidas (global.css). */}
      <div aria-hidden="true" className="au-fondo-campus" />
      <div aria-hidden="true" style={{ position: 'absolute', inset: 0, pointerEvents: 'none', background: 'radial-gradient(900px 420px at 100% 0%, rgba(227,6,29,0.06), transparent 60%), radial-gradient(700px 380px at 0% 100%, rgba(28,26,27,0.035), transparent 60%)' }} />

      <header style={{ position: 'relative', height: size.header, flex: 'none', display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12, padding: wide ? '0 20px' : '0 12px', borderBottom: `1px solid ${colors.linea}`, background: colors.barra, backdropFilter: web.blur, WebkitBackdropFilter: web.blur, boxShadow: web.sombraSuave, zIndex: 2 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
          <img src="/logo-unillanos.png" alt="Universidad de los Llanos" width={40} height={38} style={{ width: wide ? 40 : 34, height: 'auto', flex: 'none' }} />
          <span style={{ fontSize: wide ? 15 : 12, fontWeight: 600, letterSpacing: wide ? '0.1em' : '0.04em' }}>AULA <span style={{ color: colors.marca, fontWeight: 700 }}>UNILLANOS</span></span>
          {wide && <><span style={{ width: 1, height: 24, background: colors.lineaFuerte }} /><span style={{ fontSize: 14, color: colors.textoTenue }}>Actividades y notas</span></>}
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: wide ? 12 : 8 }}>
          {wide && (
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end' }}>
              <span style={{ fontSize: 14, fontWeight: 600 }}>{u.nombre}</span>
              <span style={{ fontSize: 12, color: colors.textoTenue }}>{u.rol === 'docente' ? 'Docente' : 'Estudiante · Pregrado'}</span>
            </div>
          )}
          <div style={{ width: 38, height: 38, borderRadius: '50%', background: colors.marcaSuave, color: colors.marcaTexto, border: `1px solid ${colors.lineaMarca}`, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 13, fontWeight: 700 }}>{iniciales(u.nombre)}</div>
          <button type="button" aria-label="Modo vidrio" aria-pressed={vidrio} title={vidrio ? 'Quitar modo vidrio' : 'Modo vidrio: ver el campus de fondo'} onPointerDown={conOnda()} className="au-onda-host au-boton--contorno au-boton-vidrio" onClick={alternarVidrio} style={botonIcono}>
            <Icono d={icono.vidrio} tam={18} />
          </button>
          <button type="button" aria-label="Salir" title="Salir" onPointerDown={conOnda()} className="au-onda-host au-boton--contorno" onClick={async () => { await logout(); nav('/login'); }} style={botonIcono}>
            <Icono d={icono.salir} tam={18} />
          </button>
        </div>
      </header>

      <div style={{ position: 'relative', flex: 1, minHeight: 0, display: 'flex' }}>
        {wide && (
          <aside aria-label="Navegación principal" style={{ width: size.sidebar, flex: 'none', display: 'flex', flexDirection: 'column', gap: 6, padding: '20px 14px', borderRight: `1px solid ${colors.linea}`, background: colors.barra, backdropFilter: web.blur, WebkitBackdropFilter: web.blur }}>
            {items.map((i) => (
              <NavLink key={i.to} to={i.to} end={i.end} className="au-nav-link" style={({ isActive }) => navStyle(isActive, false)}><Icono d={i.d} />{i.label}</NavLink>
            ))}
            <div style={{ marginTop: 'auto', borderRadius: 14, overflow: 'hidden', border: `1px solid ${colors.linea}`, background: colors.blanco, boxShadow: web.sombraSuave }}>
              <div style={{ height: 96, backgroundImage: 'url(/campus.webp)', backgroundSize: 'cover', backgroundPosition: '60% 40%' }} />
              <div style={{ padding: '12px 14px', display: 'flex', flexDirection: 'column', gap: 2 }}>
                <span style={{ fontSize: 13, fontWeight: 600 }}>Universidad de los Llanos</span>
                <span style={{ fontSize: 12, color: colors.textoTenue }}>Villavicencio, Meta</span>
              </div>
            </div>
          </aside>
        )}
        <main style={{ flex: 1, minWidth: 0, overflow: 'auto' }}>
          <div className="au-escalable" style={{ maxWidth: 1280, margin: '0 auto', padding: wide ? '24px 28px 40px' : '20px 16px 32px', display: 'flex', flexDirection: 'column', gap: space[6] }}>
            <Outlet context={{ wide }} />
          </div>
        </main>
      </div>

      {!wide && (
        <nav aria-label="Navegación principal" style={{ position: 'relative', flex: 'none', display: 'flex', gap: 6, padding: '8px 10px calc(8px + env(safe-area-inset-bottom))', background: colors.barra, borderTop: `1px solid ${colors.linea}`, backdropFilter: web.blur, WebkitBackdropFilter: web.blur, boxShadow: web.sombraSuave }}>
          {items.map((i) => (
            <NavLink key={i.to} to={i.to} end={i.end} className="au-nav-link" style={({ isActive }) => navStyle(isActive, true)}><Icono d={i.d} />{i.label}</NavLink>
          ))}
        </nav>
      )}
    </div>
  );
}

import type { CSSProperties } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useSesion, useUsuario } from '@/hooks';
import { iniciales } from '@/domain';
import { colors, font, size, space, web } from '@/theme/tokens';
import { Icono, icono } from '@/ui';
import { useIsWide } from '@/ui/useIsWide';

export function AppShell() {
  const wide = useIsWide();
  const u = useUsuario();
  const { logout } = useSesion();
  const nav = useNavigate();
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
    background: activo ? 'linear-gradient(135deg,#D0142F,#9E0C22)' : 'transparent',
    color: activo ? '#FFFFFF' : colors.textoMedio, boxShadow: activo && !movil ? web.sombraMarca : 'none',
  });

  return (
    <div style={{ position: 'relative', height: '100dvh', display: 'flex', flexDirection: 'column', background: colors.fondo, overflow: 'hidden' }}>
      <div style={{ position: 'absolute', inset: -60, backgroundImage: 'url(/campus.webp)', backgroundSize: 'cover', backgroundPosition: '60% 30%', filter: 'blur(30px) saturate(1.2) brightness(0.55)' }} />
      <div style={{ position: 'absolute', inset: 0, background: 'linear-gradient(160deg,rgba(18,16,20,0.55) 0%,rgba(18,16,20,0.82) 55%,rgba(14,12,15,0.94) 100%)' }} />

      <header style={{ position: 'relative', height: size.header, flex: 'none', display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12, padding: '0 20px', borderBottom: `1px solid ${colors.linea}`, background: 'rgba(14,12,15,0.35)', backdropFilter: 'blur(10px)' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
          <div style={{ width: 34, height: 34, borderRadius: '50%', background: colors.marca, display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, fontSize: 15, boxShadow: '0 0 0 3px rgba(200,16,46,0.25)' }}>U</div>
          <span style={{ fontSize: 15, fontWeight: 600, letterSpacing: '0.1em' }}>AULA UNILLANOS</span>
          {wide && <><span style={{ width: 1, height: 24, background: 'rgba(255,255,255,0.16)' }} /><span style={{ fontSize: 14, color: colors.textoTenue }}>Campus Virtual</span></>}
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          {wide && (
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end' }}>
              <span style={{ fontSize: 14, fontWeight: 600 }}>{u.nombre}</span>
              <span style={{ fontSize: 12, color: colors.textoTenue }}>{u.rol === 'docente' ? 'Docente' : 'Estudiante · Pregrado'}</span>
            </div>
          )}
          <div style={{ width: 38, height: 38, borderRadius: '50%', background: 'rgba(255,255,255,0.1)', border: `1px solid ${colors.lineaFuerte}`, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 13, fontWeight: 700 }}>{iniciales(u.nombre)}</div>
          <button aria-label="Salir" title="Salir" onClick={async () => { await logout(); nav('/login'); }} style={{ width: 38, height: 38, borderRadius: 10, border: `1px solid ${colors.lineaFuerte}`, background: 'rgba(255,255,255,0.04)', color: colors.textoMedio, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <Icono d={icono.salir} tam={18} />
          </button>
        </div>
      </header>

      <div style={{ position: 'relative', flex: 1, minHeight: 0, display: 'flex' }}>
        {wide && (
          <aside style={{ width: size.sidebar, flex: 'none', display: 'flex', flexDirection: 'column', gap: 6, padding: '20px 14px', borderRight: `1px solid ${colors.linea}`, background: 'rgba(14,12,15,0.4)' }}>
            {items.map((i) => (
              <NavLink key={i.to} to={i.to} end={i.end} style={({ isActive }) => navStyle(isActive, false)}><Icono d={i.d} />{i.label}</NavLink>
            ))}
            <div style={{ marginTop: 'auto', borderRadius: 14, overflow: 'hidden', border: `1px solid ${colors.linea}`, background: 'rgba(255,255,255,0.03)' }}>
              <div style={{ height: 96, backgroundImage: 'url(/campus.webp)', backgroundSize: 'cover', backgroundPosition: '60% 40%' }} />
              <div style={{ padding: '12px 14px', display: 'flex', flexDirection: 'column', gap: 2 }}>
                <span style={{ fontSize: 13, fontWeight: 600 }}>Universidad de los Llanos</span>
                <span style={{ fontSize: 12, color: colors.textoTenue }}>Villavicencio, Meta</span>
              </div>
            </div>
          </aside>
        )}
        <main style={{ flex: 1, minWidth: 0, overflow: 'auto' }}>
          <div style={{ maxWidth: 1120, margin: '0 auto', padding: wide ? '36px 36px 56px' : '20px 16px 32px', display: 'flex', flexDirection: 'column', gap: space[6] }}>
            <Outlet context={{ wide }} />
          </div>
        </main>
      </div>

      {!wide && (
        <nav style={{ position: 'relative', flex: 'none', display: 'flex', gap: 6, padding: '8px 10px calc(8px + env(safe-area-inset-bottom))', background: 'rgba(14,12,15,0.82)', borderTop: `1px solid ${colors.linea}`, backdropFilter: web.blur }}>
          {items.map((i) => (
            <NavLink key={i.to} to={i.to} end={i.end} style={({ isActive }) => navStyle(isActive, true)}><Icono d={i.d} />{i.label}</NavLink>
          ))}
        </nav>
      )}
    </div>
  );
}

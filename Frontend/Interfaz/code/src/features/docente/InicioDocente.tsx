import { useNavigate } from 'react-router-dom';
import { useCursos, useActividades, usePendientesDocente, useUsuario } from '@/hooks';
import { colors, estados, font } from '@/theme/tokens';
import { BarraCortes, Cargando, Chevron, ErrorEstado, Fila, Monograma, Panel, Tarjeta, TituloPagina, Vacio, entrada } from '@/ui';
import { codigoCurso, useLayout } from '../cursos/compartido';

export function InicioDocente() {
  const { wide } = useLayout();
  const u = useUsuario();
  const nav = useNavigate();
  const p = usePendientesDocente();
  const cursos = useCursos();

  return (
    <>
      <div style={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'space-between', alignItems: 'flex-end', gap: 20, ...entrada(0) }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          <h1 style={{ margin: 0, fontSize: wide ? font.size.hero : font.size.heroMovil, fontWeight: font.weight.light, letterSpacing: '0.06em', lineHeight: 1.05 }}>HOLA, {u.nombre.split(' ')[0].toUpperCase()}</h1>
          <span style={{ fontSize: 16, color: colors.textoMedio }}>Esto es lo que tienes pendiente en tus cursos.</span>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end', gap: 2, paddingLeft: 20, borderLeft: `1px solid ${colors.lineaFuerte}` }}>
          <span style={{ fontSize: 40, fontWeight: 300, lineHeight: 1 }}>{p.totalSinCalificar}</span>
          <span style={{ fontSize: 13, color: colors.textoTenue }}>notas por registrar</span>
        </div>
      </div>

      <Panel indice={1}>
        <h2 style={{ margin: 0, fontSize: 19, fontWeight: 600 }}>Por calificar</h2>
        {p.isLoading ? <Cargando /> : p.error ? <ErrorEstado error={p.error} onReintentar={p.refetch} /> : p.data!.length === 0 ? <Vacio>No hay entregas pendientes.</Vacio> : p.data!.map(({ curso, actividad, conteo }) => (
          <Fila key={actividad.id} onClick={() => nav(`/d/cursos/${curso.id}?tab=calificar&actividad=${actividad.id}`)}>
            <Monograma texto={curso.monograma} acento={curso.acento} />
            <div style={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column', gap: 3 }}>
              <span style={{ fontSize: 15, fontWeight: 600 }}>{actividad.titulo}</span>
              <span style={{ fontSize: 13, color: colors.textoTenue }}>{curso.nombre} · Corte {actividad.corte}</span>
            </div>
            <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap', justifyContent: 'flex-end' }}>
              {conteo.sinCalificar > 0 && <span style={pill(estados.sin_calificar.fondo, estados.sin_calificar.texto)}>{conteo.sinCalificar} sin calificar</span>}
              {conteo.borradores > 0 && <span style={pill(estados.borrador.fondo, estados.borrador.texto)}>{conteo.borradores} en borrador</span>}
            </div>
            <Chevron />
          </Fila>
        ))}
      </Panel>

      <section style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
        <h2 style={{ margin: 0, fontSize: 19, fontWeight: 600 }}>Mis cursos</h2>
        {cursos.isLoading ? <Cargando /> : cursos.error ? <ErrorEstado error={cursos.error} onReintentar={cursos.refetch} /> : <GrillaCursosDocente />}
      </section>
    </>
  );
}

const pill = (bg: string, fg: string) => ({ display: 'inline-flex', alignItems: 'center', height: 26, padding: '0 12px', borderRadius: 999, fontSize: 12, fontWeight: 600, whiteSpace: 'nowrap' as const, background: bg, color: fg });

function GrillaCursosDocente() {
  const nav = useNavigate();
  const cursosQ = useCursos();
  const actsQ = useActividades();
  if (cursosQ.error || actsQ.error) return <ErrorEstado error={cursosQ.error ?? actsQ.error} onReintentar={() => { cursosQ.refetch(); actsQ.refetch(); }} />;
  if (cursosQ.isLoading || actsQ.isLoading) return <Cargando />;
  const cursos = cursosQ.data ?? [];
  const acts = actsQ.data ?? [];
  if (!cursos.length) return <Vacio>No tienes cursos asignados.</Vacio>;
  return (
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill,minmax(270px,1fr))', gap: 16 }}>
      {cursos.map((c, i) => (
        <Tarjeta key={c.id} indice={i + 1} onClick={() => nav(`/d/cursos/${c.id}`)}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <Monograma texto={c.monograma} acento={c.acento} />
            <div style={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column', gap: 2 }}>
              <span style={{ fontSize: 12, color: colors.textoTenue }}>{codigoCurso(c)}</span>
              <span style={{ fontSize: 15, fontWeight: 600 }}>{c.nombre}</span>
            </div>
            <Chevron />
          </div>
          <BarraCortes pesos={c.pesos} acento={c.acento} />
          <div style={{ display: 'flex', gap: 18, paddingTop: 12, borderTop: `1px solid ${colors.linea}`, fontSize: 13, color: colors.textoTenue }}>
            <span>{acts.filter((a) => a.cursoId === c.id).length} actividades</span>
            <span>Cortes {c.pesos.join(' / ')}</span>
          </div>
        </Tarjeta>
      ))}
    </div>
  );
}

export function CursosDocente() {
  const { wide } = useLayout();
  return (<><TituloPagina wide={wide}>Cursos</TituloPagina><GrillaCursosDocente /></>);
}

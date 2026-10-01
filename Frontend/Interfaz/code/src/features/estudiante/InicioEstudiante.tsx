import { useNavigate } from 'react-router-dom';
import { useMisNotas, useProximasEntregas, useUsuario, useApi } from '@/hooks';
import { fechaCorta, formatoNota, relativo } from '@/domain';
import { acento, colors, font } from '@/theme/tokens';
import { Barra, Boton, Cargando, Chevron, Chip, ErrorEstado, Fila, Icono, icono, Monograma, Panel, Tarjeta, entrada } from '@/ui';
import { codigoCurso, useLayout } from '../cursos/compartido';
import type { NotasCurso } from '@/hooks';

export function InicioEstudiante() {
  const { wide } = useLayout();
  const u = useUsuario();
  const { hoy } = useApi();
  const nav = useNavigate();
  const notas = useMisNotas();
  const proximas = useProximasEntregas();
  const nombre = u.nombre.split(' ')[0].toUpperCase();

  return (
    <>
      <div style={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'space-between', alignItems: 'flex-end', gap: 20, ...entrada(0) }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          <h1 style={{ margin: 0, fontSize: wide ? font.size.hero : font.size.heroMovil, fontWeight: font.weight.light, letterSpacing: '0.06em', lineHeight: 1.05 }}>HOLA, {nombre}</h1>
          <span style={{ fontSize: 16, color: colors.textoMedio }}>Aquí tienes un resumen de tu avance académico.</span>
          <div style={{ alignSelf: 'flex-start', marginTop: 6, display: 'flex', alignItems: 'center', gap: 10, height: 44, padding: '0 16px', borderRadius: 12, background: 'rgba(255,255,255,0.06)', border: '1px solid rgba(255,255,255,0.1)', fontSize: 15 }}>
            <strong style={{ fontWeight: 600 }}>Semestre {notas.data?.[0]?.curso.periodo ?? '—'}</strong><span style={{ color: colors.textoApagado }}>·</span><span style={{ color: colors.textoMedio }}>{notas.data?.length ?? 0} cursos</span>
          </div>
        </div>
      </div>

      <Panel indice={1}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h2 style={{ margin: 0, fontSize: 19, fontWeight: 600 }}>Próximas entregas</h2>
          <Boton variante="texto" onClick={() => nav('/e/cursos')}>Ver cursos →</Boton>
        </div>
        {proximas.isLoading ? <Cargando /> : proximas.error ? <ErrorEstado error={proximas.error} onReintentar={proximas.refetch} /> : proximas.data!.map((p) => {
          const vence = `${fechaCorta(p.actividad.vence)} · ${relativo(p.actividad.vence, hoy())}`;
          return (
            <Fila key={p.actividad.id} onClick={() => nav(`/e/cursos/${p.curso.id}/actividades/${p.actividad.id}`)}>
              <Monograma texto={p.curso.monograma} acento={p.curso.acento} />
              <div style={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column', gap: 3 }}>
                <span style={{ fontSize: 15, fontWeight: 600 }}>{p.actividad.titulo}</span>
                <span style={{ fontSize: 13, color: colors.textoTenue }}>{p.curso.nombre} · {wide ? `Corte ${p.actividad.corte}` : vence}</span>
              </div>
              {wide && <span style={{ width: 170, flex: 'none', display: 'flex', alignItems: 'center', gap: 8, fontSize: 14, color: colors.textoMedio }}><Icono d={icono.calendario} tam={16} />{vence}</span>}
              <Chip estado={p.estado} />
              <Chevron />
            </Fila>
          );
        })}
      </Panel>

      <section style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
        <h2 style={{ margin: 0, fontSize: 19, fontWeight: 600 }}>Mis cursos</h2>
        {notas.isLoading ? <Cargando /> : <GrillaCursos notas={notas.data ?? []} />}
      </section>
    </>
  );
}

export function GrillaCursos({ notas, conDocente }: { notas: NotasCurso[]; conDocente?: boolean }) {
  const nav = useNavigate();
  return (
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill,minmax(270px,1fr))', gap: 16 }}>
      {notas.map((n, i) => (
        <Tarjeta key={n.curso.id} indice={i + 1} onClick={() => nav(`/e/cursos/${n.curso.id}`)}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <Monograma texto={n.curso.monograma} acento={n.curso.acento} />
            <div style={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column', gap: 2 }}>
              <span style={{ fontSize: 12, color: colors.textoTenue }}>{codigoCurso(n.curso)}</span>
              <span style={{ fontSize: 15, fontWeight: 600 }}>{n.curso.nombre}</span>
            </div>
            <Chevron />
          </div>
          {conDocente && <span style={{ fontSize: 13, color: colors.textoTenue }}>{n.curso.docente} · {n.curso.creditos} créditos</span>}
          <div style={{ display: 'flex', alignItems: 'baseline', gap: 10 }}>
            <span style={{ fontSize: 34, fontWeight: 600, lineHeight: 1 }}>{formatoNota(n.resumen.acumulado)}</span>
            <span style={{ fontSize: 13, color: colors.textoTenue }}>acumulado · evaluado {Math.round(n.resumen.evaluado)}%</span>
          </div>
          <Barra valor={n.resumen.evaluado} color={acento(n.curso.acento)} />
          <div style={{ display: 'flex', gap: 18, paddingTop: 12, borderTop: '1px solid rgba(255,255,255,0.07)', fontSize: 13, color: colors.textoTenue }}>
            <span>{n.actividades.length} actividades</span>
            <span>{n.entregas} {n.entregas === 1 ? 'entrega' : 'entregas'}</span>
          </div>
        </Tarjeta>
      ))}
    </div>
  );
}

import { useNavigate } from 'react-router-dom';
import { useApi, useMisNotas } from '@/hooks';
import { formatoNota } from '@/domain';
import { acento, colors, font } from '@/theme/tokens';
import { Barra, Cargando, ErrorEstado, Monograma, Panel, TituloPagina, entrada } from '@/ui';
import { codigoCurso, textoCreditos, useLayout } from '../cursos/compartido';
import { GrillaCursos } from './InicioEstudiante';

export function CursosEstudiante() {
  const { wide } = useLayout();
  const q = useMisNotas();
  return (
    <>
      <TituloPagina wide={wide}>Cursos</TituloPagina>
      {q.isLoading ? <Cargando /> : q.error ? <ErrorEstado error={q.error} onReintentar={q.refetch} /> : <GrillaCursos notas={q.data!} conDocente />}
    </>
  );
}

const colorNota = (n: number | null, ap: number) => (n == null ? colors.textoApagado : n < ap ? colors.error : colors.texto);

/** Móvil: tarjetas por curso. Escritorio: tabla. Mismos datos del hook useMisNotas. */
export function MisNotas() {
  const { wide } = useLayout();
  const nav = useNavigate();
  const q = useMisNotas();
  const AP = useApi().aprobatoria;

  return (
    <>
      <TituloPagina wide={wide} descripcion="Cada corte muestra el promedio ponderado de las notas publicadas. El acumulado suma el aporte de cada corte según su peso.">Mis notas</TituloPagina>
      {q.isLoading ? <Cargando /> : q.error ? <ErrorEstado error={q.error} onReintentar={q.refetch} /> : !wide ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
          {q.data!.map((n, i) => (
            <Panel key={n.curso.id} indice={i + 1} padding={16}>
              <button onClick={() => nav(`/e/cursos/${n.curso.id}?tab=notas`)} style={{ display: 'flex', alignItems: 'center', gap: 12, border: 0, background: 'none', padding: 0, textAlign: 'left' }}>
                <Monograma texto={n.curso.monograma} acento={n.curso.acento} />
                <div style={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column', gap: 2 }}>
                  <span style={{ fontSize: 15, fontWeight: 600 }}>{n.curso.nombre}</span>
                  <span style={{ fontSize: 12, color: colors.textoTenue }}>{[codigoCurso(n.curso), textoCreditos(n.curso)].filter(Boolean).join(' · ')}</span>
                </div>
                <span style={{ fontSize: 28, fontWeight: 600 }}>{formatoNota(n.resumen.acumulado)}</span>
              </button>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,minmax(0,1fr))', gap: 8 }}>
                {n.resumen.cortes.map((k) => (
                  <div key={k.corte} style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.06)', borderRadius: 12, padding: 10, display: 'flex', flexDirection: 'column', gap: 6 }}>
                    <span style={{ fontSize: 11, color: colors.textoTenue }}>Corte {k.corte} · {k.peso}%</span>
                    <span style={{ fontSize: 19, fontWeight: 600, color: colorNota(k.nota, AP) }}>{formatoNota(k.nota)}</span>
                    <Barra valor={k.nota == null ? 0 : (k.nota / 5) * 100} color={acento(n.curso.acento)} alto={4} />
                  </div>
                ))}
              </div>
              <span style={{ fontSize: 13, color: colors.textoMedio }}>{n.mensaje}</span>
            </Panel>
          ))}
        </div>
      ) : (
        <div style={{ background: colors.superficie, border: '1px solid rgba(255,255,255,0.09)', borderRadius: 18, backdropFilter: 'blur(14px)', overflow: 'hidden', ...entrada(1) }}>
          <div role="row" style={{ display: 'grid', gridTemplateColumns: COLS, gap: 16, padding: '14px 20px', fontSize: 12, fontWeight: 600, letterSpacing: '0.06em', color: colors.textoTenue, borderBottom: `1px solid ${colors.linea}` }}>
            <span>CURSO</span><span>CORTE 1</span><span>CORTE 2</span><span>CORTE 3</span><span>ACUMULADO</span><span style={{ textAlign: 'right' }}>CRÉD.</span>
          </div>
          {q.data!.map((n) => (
            <button key={n.curso.id} role="row" className="au-hover-row" onClick={() => nav(`/e/cursos/${n.curso.id}?tab=notas`)} style={{ display: 'grid', gridTemplateColumns: COLS, gap: 16, alignItems: 'center', padding: '16px 20px', border: 0, borderBottom: '1px solid rgba(255,255,255,0.06)', background: 'transparent', textAlign: 'left', width: '100%' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 12, minWidth: 0 }}>
                <Monograma texto={n.curso.monograma} acento={n.curso.acento} />
                <div style={{ display: 'flex', flexDirection: 'column', gap: 3, minWidth: 0 }}>
                  <span style={{ fontSize: 15, fontWeight: 600 }}>{n.curso.nombre}</span>
                  <span style={{ fontSize: 12, color: colors.textoTenue }}>{n.mensaje}</span>
                </div>
              </div>
              {n.resumen.cortes.map((k) => (
                <div key={k.corte} style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                  <div style={{ display: 'flex', alignItems: 'baseline', gap: 6 }}>
                    <span style={{ fontSize: 17, fontWeight: 600, color: colorNota(k.nota, AP) }}>{formatoNota(k.nota)}</span>
                    <span style={{ fontSize: 11, color: colors.textoApagado }}>{k.peso}%</span>
                  </div>
                  <Barra valor={k.nota == null ? 0 : (k.nota / 5) * 100} color={acento(n.curso.acento)} alto={4} max={90} />
                </div>
              ))}
              <span style={{ fontSize: 22, fontWeight: 600 }}>{formatoNota(n.resumen.acumulado)}</span>
              <span style={{ fontSize: 15, textAlign: 'right', color: colors.textoMedio }}>{n.curso.creditos ?? '—'}</span>
            </button>
          ))}
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, padding: '14px 20px', fontSize: 13, color: colors.textoTenue }}>
            <span>Total créditos</span><strong style={{ color: colors.texto, fontWeight: font.weight.bold }}>{q.totalCreditos ?? '—'}</strong>
          </div>
        </div>
      )}
    </>
  );
}

const COLS = 'minmax(0,2.6fr) repeat(3,minmax(0,1fr)) minmax(0,1fr) 72px';

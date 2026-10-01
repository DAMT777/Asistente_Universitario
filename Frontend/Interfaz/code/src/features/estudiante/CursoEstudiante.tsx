import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import type { Corte } from '@/types';
import { useNotasCurso, useApi } from '@/hooks';
import { ETIQUETA_ESTADO, formatoNota } from '@/domain';
import { acento, acentos, colors, estados } from '@/theme/tokens';
import { Cargando, Chip, ErrorEstado, FechaTile, Fila, Panel, Pestanas, Vacio } from '@/ui';
import { BannerCurso, Volver } from '../cursos/compartido';

type Tab = 'actividades' | 'notas' | 'retro';
const CORTES: Corte[] = [1, 2, 3];

export function CursoEstudiante() {
  const { cursoId } = useParams();
  const [params, setParams] = useSearchParams();
  const tab = (params.get('tab') as Tab) ?? 'actividades';
  const nav = useNavigate();
  const { aprobatoria } = useApi();
  const q = useNotasCurso(cursoId);

  if (q.isLoading) return <Cargando />;
  if (q.error || !q.data) return <ErrorEstado error={q.error ?? new Error('Curso no encontrado.')} onReintentar={q.refetch} />;
  const { curso, resumen, mensaje, actividades } = q.data;
  const colorNota = (n: number | null) => (n == null ? colors.textoApagado : n < aprobatoria ? colors.error : colors.texto);
  const retros = actividades.filter((a) => a.estado === 'calificada' && a.calificacion?.retro);

  return (
    <>
      <Volver a="/e/cursos" etiqueta="Cursos" />
      <BannerCurso curso={curso} cifra={formatoNota(resumen.acumulado)} cifraEtiqueta={`acumulado · evaluado ${Math.round(resumen.evaluado)}%`} notasCortes={resumen.cortes.map((k) => formatoNota(k.nota))} />
      <Pestanas<Tab> valor={tab} onCambio={(t) => setParams({ tab: t }, { replace: true })} opciones={[{ id: 'actividades', etiqueta: 'Actividades' }, { id: 'notas', etiqueta: 'Notas' }, { id: 'retro', etiqueta: 'Retroalimentación' }]} />

      {tab === 'actividades' && CORTES.map((k, i) => (
        <Panel key={k} indice={i}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
            <h2 style={{ margin: 0, fontSize: 17, fontWeight: 600 }}>Corte {k}</h2>
            <span style={{ fontSize: 13, color: colors.textoTenue }}>{curso.pesos[k - 1]}% de la nota</span>
          </div>
          {actividades.filter((a) => a.actividad.corte === k).map((a) => (
            <Fila key={a.actividad.id} onClick={() => nav(`actividades/${a.actividad.id}`)}>
              <FechaTile fecha={a.actividad.vence} />
              <div style={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column', gap: 3 }}>
                <span style={{ fontSize: 15, fontWeight: 600 }}>{a.actividad.titulo}</span>
                <span style={{ fontSize: 13, color: colors.textoTenue }}>{a.actividad.peso}% del corte</span>
              </div>
              {a.estado === 'calificada' && <span style={{ fontSize: 18, fontWeight: 600 }}>{formatoNota(a.calificacion?.nota)}</span>}
              <Chip estado={a.estado} />
            </Fila>
          ))}
        </Panel>
      ))}

      {tab === 'notas' && (
        <>
          <Panel indice={0} style={{ flexDirection: 'row', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between' }}>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
              <span style={{ fontSize: 12, letterSpacing: '0.06em', color: colors.textoTenue }}>ACUMULADO · EVALUADO {Math.round(resumen.evaluado)}%</span>
              <span style={{ fontSize: 36, fontWeight: 600, lineHeight: 1 }}>{formatoNota(resumen.acumulado)}</span>
            </div>
            <span style={{ fontSize: 14, color: colors.textoMedio, maxWidth: 360 }}>{mensaje}</span>
          </Panel>
          {resumen.cortes.map((k, i) => (
            <Panel key={k.corte} indice={i + 1} padding={0} style={{ gap: 0, overflow: 'hidden' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '14px 18px', borderBottom: '1px solid rgba(255,255,255,0.07)' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                  <span style={{ fontSize: 15, fontWeight: 600 }}>Corte {k.corte}</span>
                  <span style={{ fontSize: 12, fontWeight: 600, padding: '3px 8px', borderRadius: 999, background: colors.linea, color: colors.textoMedio }}>{k.peso}%</span>
                </div>
                <span style={{ fontSize: 20, fontWeight: 600, color: colorNota(k.nota) }}>{formatoNota(k.nota)}</span>
              </div>
              {actividades.filter((a) => a.actividad.corte === k.corte).map((a) => (
                <div key={a.actividad.id} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 12, padding: '12px 18px', borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: 2, minWidth: 0 }}>
                    <span style={{ fontSize: 14 }}>{a.actividad.titulo}</span>
                    <span style={{ fontSize: 12, color: colors.textoTenue }}>{a.actividad.peso}% del corte</span>
                  </div>
                  <span style={{ fontSize: 15, fontWeight: 600, whiteSpace: 'nowrap', color: a.estado === 'calificada' ? colorNota(a.calificacion!.nota) : estados[a.estado].texto }}>
                    {a.estado === 'calificada' ? formatoNota(a.calificacion!.nota) : ETIQUETA_ESTADO[a.estado]}
                  </span>
                </div>
              ))}
            </Panel>
          ))}
        </>
      )}

      {tab === 'retro' && (retros.length === 0 ? <Vacio>Aún no hay comentarios del docente en este curso.</Vacio> : retros.map((a, i) => (
        <Panel key={a.actividad.id} indice={i} style={{ flexDirection: 'row', alignItems: 'flex-start', gap: 16 }}>
          <div style={{ width: 52, height: 52, flex: 'none', borderRadius: 12, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 18, fontWeight: 700, background: acento(curso.acento, 0.2), color: acentos[curso.acento].texto }}>{formatoNota(a.calificacion!.nota)}</div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 6, minWidth: 0 }}>
            <span style={{ fontSize: 15, fontWeight: 600 }}>{a.actividad.titulo}</span>
            <span style={{ fontSize: 12, color: colors.textoTenue }}>Corte {a.actividad.corte}</span>
            <p style={{ margin: 0, fontSize: 14, lineHeight: 1.55, color: colors.textoSuave }}>{a.calificacion!.retro}</p>
          </div>
        </Panel>
      )))}
    </>
  );
}

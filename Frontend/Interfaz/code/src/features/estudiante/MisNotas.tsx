import { useId, useState, type CSSProperties } from 'react';
import { useNavigate } from 'react-router-dom';
import { useApi, useMisNotas, type ActividadEstudiante, type NotasCurso } from '@/hooks';
import { describirNecesidad, ETIQUETA_ESTADO, formatoNota, NOTA_MAX } from '@/domain';
import type { ResumenCorte } from '@/types';
import { Cargando, Chip, ErrorEstado, Icono, icono, Monograma, TituloPagina, Vacio } from '@/ui';
import { useLayout } from '../cursos/compartido';
import { GrillaCursos } from './InicioEstudiante';

export function CursosEstudiante() {
  const { wide } = useLayout();
  const q = useMisNotas();
  return <><TituloPagina wide={wide}>Cursos</TituloPagina>{q.isLoading ? <Cargando /> : q.error ? <ErrorEstado error={q.error} onReintentar={q.refetch} /> : !q.data?.length ? <Vacio>No tienes cursos asignados.</Vacio> : <GrillaCursos notas={q.data} conDocente />}</>;
}

const porcentaje = (nota: number | null) => (nota == null ? 0 : Math.max(0, Math.min(100, (nota / NOTA_MAX) * 100)));
const retardo = (ms: number) => ({ ['--au-retardo' as string]: `${ms}ms` }) as CSSProperties;

/** Mis notas: una fila por materia con la nota de cada corte. Al abrirla muestra qué actividades forman cada corte. */
export function MisNotas() {
  const { wide } = useLayout();
  const q = useMisNotas();
  const [abierto, setAbierto] = useState<string | null>(null);

  return <>
    <TituloPagina wide={wide} descripcion="Cada corte muestra la nota que publicó el docente. Toca una materia para ver las actividades y notas que la forman. El acumulado suma el aporte de cada corte según su peso.">Mis notas</TituloPagina>
    {q.isLoading ? <Cargando /> : q.error ? <ErrorEstado error={q.error} onReintentar={q.refetch} /> : !q.data?.length ? <Vacio>No tienes cursos asignados.</Vacio> : (
      <div className="au-notas">
        <div className="au-notas-encabezado" aria-hidden="true">
          <span>Curso</span><span>Corte 1</span><span>Corte 2</span><span>Corte 3</span><span>Acumulado</span><span>Créd.</span><span />
        </div>
        {q.data.map((n, i) => (
          <FilaMateria key={n.curso.id} notas={n} indice={i} abierta={abierto === n.curso.id} onAlternar={() => setAbierto((a) => (a === n.curso.id ? null : n.curso.id))} />
        ))}
        <div className="au-notas-pie">Total créditos <strong>{q.totalCreditos || '—'}</strong></div>
      </div>
    )}
  </>;
}

function FilaMateria({ notas: n, indice, abierta, onAlternar }: { notas: NotasCurso; indice: number; abierta: boolean; onAlternar: () => void }) {
  const { aprobatoria } = useApi();
  const nav = useNavigate();
  const idDetalle = useId();
  const parcial = n.resumen.cortes.some((k) => k.nota === null);

  return (
    <div className="au-notas-item" data-abierto={abierta} style={{ ['--i' as string]: indice } as CSSProperties}>
      <button type="button" className="au-notas-fila" aria-expanded={abierta} aria-controls={idDetalle} onClick={onAlternar}>
        <span className="au-notas-curso">
          <Monograma texto={n.curso.monograma} acento={n.curso.acento} />
          <span className="au-notas-curso-texto">
            <strong>{n.curso.nombre}</strong>
            <span>{describirNecesidad(n.resumen.necesidad, aprobatoria)}</span>
          </span>
        </span>
        {n.resumen.cortes.map((k, j) => (
          <span key={k.corte} className="au-notas-corte-celda">
            <span className="au-notas-etiqueta-movil">Corte {k.corte}</span>
            <span className="au-sr">Corte {k.corte}: </span>
            <span className="au-notas-valor">
              <strong>{k.nota === null ? '—' : formatoNota(k.nota)}</strong>
              <small>{k.peso}%</small>
            </span>
            <span className="au-notas-pista" role="presentation">
              <span className="au-barra-relleno au-notas-relleno" style={{ width: `${porcentaje(k.nota)}%`, ...retardo(200 + indice * 90 + j * 140) }} />
            </span>
            {k.nota === null && <span className="au-sr">sin publicar</span>}
          </span>
        ))}
        <span className="au-notas-acumulado">
          <span className="au-notas-etiqueta-movil">Acumulado</span>
          <strong>{formatoNota(n.resumen.acumulado)}</strong>
          <small>{parcial ? 'Parcial' : 'Definitiva'}</small>
        </span>
        <span className="au-notas-creditos"><span className="au-sr">Créditos: </span>{n.curso.creditos || '—'}</span>
        <span className="au-notas-chevron" aria-hidden="true"><Icono d={icono.desplegar} tam={20} /></span>
      </button>

      <div id={idDetalle} className="au-notas-detalle" role="region" aria-label={`Detalle de notas de ${n.curso.nombre}`} {...(abierta ? {} : { inert: '' })}>
        <div className="au-notas-detalle-interno">
          <div className="au-notas-cortes">
            {n.resumen.cortes.map((k, j) => (
              <DetalleCorte key={k.corte} corte={k} actividades={n.actividades.filter((a) => a.actividad.corte === k.corte)} orden={j} />
            ))}
          </div>
          <button type="button" className="au-link-button au-notas-ver-curso" onClick={() => nav(`/e/cursos/${n.curso.id}?tab=notas`)}>Ver el curso completo →</button>
        </div>
      </div>
    </div>
  );
}

function DetalleCorte({ corte: k, actividades, orden }: { corte: ResumenCorte; actividades: ActividadEstudiante[]; orden: number }) {
  const calificadas = actividades.filter((a) => a.estado === 'calificada' && a.calificacion?.nota != null);
  const suma = calificadas.reduce((s, a) => s + a.calificacion!.nota! * a.actividad.peso / 100, 0);

  return (
    <section className="au-notas-corte" style={{ ['--j' as string]: orden } as CSSProperties} aria-label={`Corte ${k.corte}`}>
      <header>
        <div>
          <h3>Corte {k.corte}</h3>
          <span>{k.peso}% del curso</span>
        </div>
        <div className="au-notas-corte-nota" data-publicado={k.nota !== null}>
          <strong>{k.nota === null ? '—' : formatoNota(k.nota)}</strong>
          <span>{k.nota === null ? 'Sin publicar' : 'Publicado'}</span>
        </div>
      </header>

      {actividades.length === 0 ? <p className="au-notas-vacio">Aún no hay actividades en este corte.</p> : (
        <ul>
          {actividades.map((a, i) => {
            const nota = a.estado === 'calificada' ? a.calificacion?.nota ?? null : null;
            return (
              <li key={a.actividad.id}>
                <div className="au-notas-act-cabeza">
                  <span className="au-notas-act-titulo">{a.actividad.titulo}<small>{a.actividad.peso}% del corte</small></span>
                  {nota !== null
                    ? <span className="au-notas-act-nota"><strong>{formatoNota(nota)}</strong><small>aporta {formatoNota(nota * a.actividad.peso / 100)}</small></span>
                    : <Chip estado={a.estado} />}
                </div>
                <span className="au-notas-mini" role="presentation">
                  <span style={{ width: `${porcentaje(nota)}%`, transitionDelay: `${120 + orden * 90 + i * 70}ms` }} />
                </span>
                {nota !== null && a.calificacion?.retro && <p className="au-notas-retro">“{a.calificacion.retro}”</p>}
                {nota === null && <span className="au-sr">{ETIQUETA_ESTADO[a.estado]}</span>}
              </li>
            );
          })}
        </ul>
      )}

      <footer>
        {calificadas.length === 0
          ? 'Ninguna nota publicada todavía en este corte.'
          : <>Suma de aportes: {calificadas.map((a) => formatoNota(a.calificacion!.nota! * a.actividad.peso / 100)).join(' + ')} = <strong>{formatoNota(suma)}</strong></>}
      </footer>
    </section>
  );
}

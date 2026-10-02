import { useEffect, useState } from 'react';
import { useApi, useCalificaciones, type FilaCalificacion } from '@/hooks';
import { fechaCompleta, formatoNota } from '@/domain';
import { colors } from '@/theme/tokens';
import { AreaTexto, Boton, Cargando, Chip, ErrorEstado, Panel, Vacio } from '@/ui';
import { useToast } from '@/ui/Toast';
import { ArchivoLink } from '../cursos/ArchivoLink';
import { useLayout } from '../cursos/compartido';

export function CalificarTab({ cursoId, actividadId, onElegir }: { cursoId: string; actividadId?: string; onElegir: (id: string) => void }) {
  const cal = useCalificaciones(cursoId, actividadId);
  const { wide } = useLayout();
  const { hoy } = useApi();
  const toast = useToast();
  const [busqueda, setBusqueda] = useState('');
  const [filtro, setFiltro] = useState('todos');
  const [error, setError] = useState<string | null>(null);
  if (cal.isLoading) return <Cargando filas={4} />;
  if (cal.error) return <ErrorEstado error={cal.error} onReintentar={cal.refetch} />;
  const a = cal.actividad;
  if (!a) return <Vacio>Crea una actividad para comenzar a calificar.</Vacio>;
  const n = cal.conteo!;
  const vencida = !!a.vence && Date.parse(a.vence) < Date.parse(hoy());
  /** CU-05 (con la entrega), CU-06 (sin entrega) y CU-07 (modificar). Siempre queda en borrador. */
  async function guardarFila(f: FilaCalificacion, nota: string, retro: string): Promise<string | null> {
    const errores = await cal.guardar({ actividadId: a!.id, estudianteId: f.estudiante.id, nota, retro, publicar: false, entregaId: f.calificacion?.entregaId });
    if (!errores) toast(f.calificacion?.estado === 'publicada' ? 'Nota modificada · queda en borrador hasta publicarla' : 'Borrador guardado');
    return errores?.nota ?? errores?.retro ?? errores?._ ?? null;
  }
  const filas = (cal.filas ?? []).filter(f => `${f.estudiante.nombre} ${f.estudiante.codigo}`.toLowerCase().includes(busqueda.toLowerCase()) && (filtro === 'todos' || (filtro === 'con_entrega' ? !!f.calificacion?.entregado : filtro === 'sin_entrega' ? !f.calificacion?.entregado : filtro === 'sin_calificar' ? !f.calificacion?.estado : f.calificacion?.estado === filtro)));
  return <>
    <label className="au-toolbar">Actividad
      <select value={a.id} onChange={e => { onElegir(e.target.value); setError(null); setFiltro('todos'); }}>
        {(cal.actividades ?? []).map(x => <option key={x.id} value={x.id}>Corte {x.corte} · {x.titulo}</option>)}
      </select>
    </label>
    <div className="au-toolbar" style={{ justifyContent: 'space-between' }}>
      <div><h2 style={{ margin: '0 0 6px' }}>{a.titulo}</h2><span style={{ color: colors.textoTenue }}>Corte {a.corte} · {a.requiereEntrega ? `${n.entregas}/${cal.totalEstudiantes} entregas` : 'No requiere archivo'} · {n.publicadas} publicadas · {n.borradores} borradores</span></div>
      {n.borradores > 0 && <Boton disabled={cal.guardando} onClick={async () => {
        setError(null);
        try { const k = await cal.publicarBorradores(); toast(k === 1 ? '1 nota de actividad publicada' : `${k} notas de actividad publicadas`); }
        catch(e) { setError(e instanceof Error ? e.message : 'No se pudieron publicar las notas.'); }
      }}>Publicar notas de actividad ({n.borradores})</Boton>}
    </div>
    <p style={{ margin: 0, color: colors.textoMedio }}>Guardar deja la nota en borrador. Usa Publicar notas de actividad para mostrar al estudiante todos los borradores de la actividad. La nota del corte se publica por separado en Notas por corte.</p>
    <div className="au-toolbar">
      <input aria-label="Buscar estudiante para calificar" placeholder="Buscar por nombre o código" value={busqueda} onChange={e => setBusqueda(e.target.value)} />
      <label>Estado <select value={filtro} onChange={e => setFiltro(e.target.value)}><option value="todos">Todos</option><option value="sin_calificar">Sin calificar</option><option value="borrador">Borradores</option><option value="publicada">Publicadas</option>{a.requiereEntrega && <><option value="con_entrega">Con entrega</option><option value="sin_entrega">Sin entrega</option></>}</select></label>
    </div>
    {error && <p role="alert" style={{ color: colors.error }}>{error}</p>}
    {!filas.length ? <Vacio>No hay estudiantes que coincidan con estos filtros.</Vacio> : wide ? <div className="au-table-wrap"><table className="au-table"><caption>Calificaciones de {a.titulo}</caption><thead><tr><th>Estudiante y entrega</th><th>Estado</th><th>Nota y retroalimentación</th></tr></thead><tbody>{filas.map(f => <FilaEditor key={f.estudiante.id + a.id} fila={f} tabla requiereEntrega={a.requiereEntrega} vencida={vencida} onGuardar={(nota, retro) => guardarFila(f, nota, retro)} />)}</tbody></table></div> : <div className="au-grade-cards">{filas.map(f => <FilaEditor key={f.estudiante.id + a.id} fila={f} tabla={false} requiereEntrega={a.requiereEntrega} vencida={vencida} onGuardar={(nota, retro) => guardarFila(f, nota, retro)} />)}</div>}
  </>;
}

function FilaEditor({ fila, tabla, requiereEntrega, vencida, onGuardar }: { fila: FilaCalificacion; tabla: boolean; requiereEntrega: boolean; vencida: boolean; onGuardar: (nota: string, retro: string) => Promise<string | null> }) {
  const c = fila.calificacion;
  const [nota, setNota] = useState(c?.nota != null ? formatoNota(c.nota) : '');
  const [retro, setRetro] = useState(c?.retro ?? '');
  const [error, setError] = useState<string | null>(null);
  const [estado, setEstado] = useState('');
  const [guardando, setGuardando] = useState(false);
  useEffect(() => { setNota(c?.nota != null ? formatoNota(c.nota) : ''); setRetro(c?.retro ?? ''); }, [c?.nota, c?.retro]);
  const publicada = c?.estado === 'publicada';
  async function guardar() {
    setGuardando(true); setError(null); setEstado('Guardando…');
    try { const err = await onGuardar(nota, retro); setError(err); setEstado(err ? 'Error al guardar' : 'Borrador guardado'); }
    catch(e) { setError(e instanceof Error ? e.message : 'No se pudo guardar.'); setEstado('Error al guardar'); }
    finally { setGuardando(false); }
  }
  const identidad = <><strong>{fila.estudiante.nombre}</strong><small>{fila.estudiante.codigo}</small><small>{!requiereEntrega ? 'No requiere archivo' : c?.entregado ? `Entregada · ${fechaCompleta(c.entregado)}` : vencida ? 'Sin entrega · plazo cerrado' : 'Sin entrega · plazo abierto'}</small>{c?.archivo && <ArchivoLink calificacion={c} />}</>;
  const editor = <><div className="au-grade-fields"><label>Nota · 0.0 a 5.0<input aria-label={`Nota de ${fila.estudiante.nombre}`} aria-invalid={!!error} aria-describedby={error ? `error-${fila.estudiante.id}` : undefined} inputMode="decimal" value={nota} onChange={e => { setNota(e.target.value); setEstado('Cambios sin guardar'); }} placeholder="Sin nota" disabled={guardando} /></label><details><summary>Retroalimentación{retro ? ' · con comentario' : ''}</summary><AreaTexto etiqueta={`Comentario para ${fila.estudiante.nombre}`} rows={3} value={retro} onChange={e => { setRetro(e.target.value); setEstado('Cambios sin guardar'); }} disabled={guardando} /></details></div>{publicada && estado === 'Cambios sin guardar' && <small style={{ color: colors.textoMedio }}>Esta nota ya la ve el estudiante. Al modificarla vuelve a borrador hasta que publiques la actividad de nuevo; si el corte ya estaba publicado, corrígelo después en Notas por corte.</small>}{error && <p role="alert" id={`error-${fila.estudiante.id}`} style={{ color: colors.error }}>{error}</p>}<div className="au-toolbar"><Boton variante="contorno" disabled={guardando} onClick={() => guardar()}>{publicada ? 'Modificar nota' : c?.estado ? 'Actualizar borrador' : 'Guardar borrador'}</Boton><small role="status">{estado}</small></div></>;
  return tabla ? <tr><th scope="row">{identidad}</th><td><Chip estado={fila.estado} /></td><td>{editor}</td></tr> : <Panel><div className="au-student-identity">{identidad}</div><Chip estado={fila.estado} />{editor}</Panel>;
}

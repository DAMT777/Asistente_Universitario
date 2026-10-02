import { useState, type ChangeEvent } from 'react';
import { useParams } from 'react-router-dom';
import type { ArchivoEntrega } from '@/types';
import { useEntrega } from '@/hooks';
import { fechaCompleta, formatoNota } from '@/domain';
import { EXTENSIONES_ENTREGA, MAX_ARCHIVOS_ENTREGA, TAMANO_MAX_ENTREGA, TAMANO_MAX_TOTAL_ENTREGA, archivoEntregaSchema, seleccionEntregaSchema } from '@/schemas';
import { colors } from '@/theme/tokens';
import { Boton, Cargando, Chip, ErrorEstado, Panel } from '@/ui';
import { useToast } from '@/ui/Toast';
import { ArchivoLink } from '../cursos/ArchivoLink';
import { Volver } from '../cursos/compartido';

const mb = (bytes: number) => `${(bytes / 1024 / 1024).toFixed(2)} MB`;
const aArchivo = (f: File): ArchivoEntrega => ({ nombre: f.name, tamano: f.size, datos: f });
const errorDe = (f: File) => {
  const v = archivoEntregaSchema.safeParse(aArchivo(f));
  return v.success ? null : v.error.issues[0].message;
};

export function ActividadPage() {
  const { cursoId, actividadId } = useParams();
  const e = useEntrega(actividadId);
  const toast = useToast();
  const [error, setError] = useState<string | null>(null);
  const [confirmar, setConfirmar] = useState(false);
  const [seleccion, setSeleccion] = useState<File[]>([]);
  if (e.isLoading) return <Cargando />;
  if (!e.actividad || !e.curso || !e.estado) return <ErrorEstado error={e.error ?? new Error('Actividad no encontrada.')} />;
  const { actividad: a, calificacion: c, curso, estado } = e;

  const total = seleccion.reduce((s, f) => s + f.size, 0);
  const validacion = seleccion.length ? seleccionEntregaSchema.safeParse(seleccion.map(aArchivo)) : null;
  const errorSeleccion = validacion && !validacion.success ? validacion.error.issues[0].message : null;
  // Los errores de un archivo se ven en su fila; abajo solo los del conjunto (cantidad o total).
  const errorConjunto = errorSeleccion && !seleccion.some(f => errorDe(f) === errorSeleccion) ? errorSeleccion : null;

  /** Agrega a la lista sin enviar. Un archivo repetido (mismo nombre y tamaño) no se duplica. */
  function elegir(ev: ChangeEvent<HTMLInputElement>) {
    const nuevos = Array.from(ev.target.files ?? []); ev.target.value = '';
    setError(null); setConfirmar(false);
    setSeleccion(actual => [...actual, ...nuevos.filter(n => !actual.some(x => x.name === n.name && x.size === n.size))]);
  }

  async function entregar() {
    setError(null);
    const err = await e.entregar(seleccion.map(aArchivo));
    setError(err);
    if (!err) { toast(c?.entregado ? 'Entrega reemplazada' : 'Entrega recibida'); setSeleccion([]); }
  }

  return <>
    <Volver a={`/e/cursos/${cursoId}`} etiqueta={curso.nombre} />
    <Panel><span style={{ color: colors.textoTenue }}>{curso.nombre} · Corte {a.corte} · {a.peso}% del corte</span><h1 style={{ margin: 0, fontSize: 26 }}>{a.titulo}</h1><span><Chip estado={estado} /></span><p style={{ margin: 0 }}>{a.requiereEntrega ? (a.vence ? `Fecha límite: ${fechaCompleta(a.vence)}` : 'Sin fecha límite.') : 'No requiere entrega de archivo. El docente registrará la calificación.'}</p></Panel>
    {estado === 'calificada' && c && <Panel><div style={{ fontSize: 36, fontWeight: 600 }}>{formatoNota(c.nota)} <span style={{ fontSize: 16 }}>de 5.0 · nota de actividad publicada</span></div><h2 style={{ fontSize: 17 }}>Retroalimentación</h2><p style={{ margin: 0, lineHeight: 1.6 }}>{c.retro || 'Sin comentarios del docente.'}</p></Panel>}
    {c?.entregado && <Panel>
      <h2 style={{ fontSize: 18, margin: 0 }}>Entrega recibida</h2>
      <span>{fechaCompleta(c.entregado)}{c.archivos && ` · ${c.archivos.length} ${c.archivos.length === 1 ? 'archivo' : 'archivos'}`}{c.tamano !== undefined && ` · ${mb(c.tamano)}`}</span>
      <ArchivoLink calificacion={c} />
      {e.abierta && <><Boton variante="contorno" disabled={e.enviando} onClick={() => setConfirmar(true)}>Anular entrega</Boton>{confirmar && <div><p>La entrega dejará de estar activa. Tu calificación se conserva y podrás volver a entregar mientras el plazo siga abierto.</p><div className="au-toolbar"><Boton disabled={e.enviando} onClick={async () => { const err = await e.anular(); setError(err); if (!err) { setConfirmar(false); toast('Entrega anulada'); } }}>Confirmar anulación</Boton><Boton variante="contorno" onClick={() => setConfirmar(false)}>Cancelar</Boton></div></div>}</>}
    </Panel>}
    {e.abierta && <Panel>
      <h2 style={{ fontSize: 18, margin: 0 }}>{c?.entregado ? 'Reemplazar entrega' : 'Enviar entrega'}</h2>
      <p style={{ margin: 0 }}>Formatos: {EXTENSIONES_ENTREGA.join(', ').toUpperCase()} · hasta {MAX_ARCHIVOS_ENTREGA} archivos · máximo {TAMANO_MAX_ENTREGA / 1024 / 1024} MB por archivo y {TAMANO_MAX_TOTAL_ENTREGA / 1024 / 1024} MB en total.</p>
      <label htmlFor="archivo-entrega">{seleccion.length ? 'Agregar más archivos' : 'Seleccionar archivos'}</label>
      <input id="archivo-entrega" type="file" multiple accept={EXTENSIONES_ENTREGA.map(x => '.' + x).join(',')} onChange={elegir} disabled={e.enviando} />
      {seleccion.length > 0 && <>
        <ul style={{ listStyle: 'none', margin: 0, padding: 0, display: 'flex', flexDirection: 'column', gap: 6 }}>
          {seleccion.map(f => { const err = errorDe(f); return <li key={`${f.name}-${f.size}`} style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
            <strong style={{ overflowWrap: 'anywhere' }}>{f.name}</strong><span style={{ color: err ? colors.error : colors.textoTenue }}>{err ?? mb(f.size)}</span>
            <Boton variante="texto" disabled={e.enviando} aria-label={`Quitar ${f.name}`} onClick={() => setSeleccion(s => s.filter(x => x !== f))}>Quitar</Boton>
          </li>; })}
        </ul>
        <span role="status" style={{ color: total > TAMANO_MAX_TOTAL_ENTREGA ? colors.error : colors.textoTenue }}>{seleccion.length} de {MAX_ARCHIVOS_ENTREGA} archivos · {mb(total)} de {TAMANO_MAX_TOTAL_ENTREGA / 1024 / 1024} MB</span>
        {c?.entregado && <span>Al entregar, estos archivos reemplazan los de tu entrega actual.</span>}
        {errorConjunto && <span role="alert" style={{ color: colors.error }}>{errorConjunto}</span>}
        <div className="au-toolbar"><Boton disabled={e.enviando || !!errorSeleccion} onClick={entregar}>{e.enviando ? 'Entregando…' : 'Entregar'}</Boton><Boton variante="contorno" disabled={e.enviando} onClick={() => { setSeleccion([]); setError(null); }}>Cancelar</Boton></div>
      </>}
    </Panel>}
    {a.requiereEntrega && !e.abierta && <Panel><p style={{ margin: 0 }}>El plazo cerró el {fechaCompleta(a.vence)}. Ya no puedes enviar, reemplazar o anular la entrega.</p></Panel>}
    {error && <p role="alert" style={{ color: colors.error }}>{error}</p>}
  </>;
}

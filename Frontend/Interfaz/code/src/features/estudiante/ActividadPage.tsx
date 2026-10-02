import { useState, type ChangeEvent } from 'react';
import { useParams } from 'react-router-dom';
import { useEntrega } from '@/hooks';
import { fechaCompleta, formatoNota } from '@/domain';
import { EXTENSIONES_ENTREGA, TAMANO_MAX_ENTREGA } from '@/schemas';
import { colors } from '@/theme/tokens';
import { Boton, Cargando, Chip, ErrorEstado, Panel } from '@/ui';
import { useToast } from '@/ui/Toast';
import { ArchivoLink } from '../cursos/ArchivoLink';
import { Volver } from '../cursos/compartido';

export function ActividadPage() {
  const { cursoId, actividadId } = useParams();
  const e = useEntrega(actividadId);
  const toast = useToast();
  const [error, setError] = useState<string | null>(null);
  const [confirmar, setConfirmar] = useState(false);
  if (e.isLoading) return <Cargando />;
  if (!e.actividad || !e.curso || !e.estado) return <ErrorEstado error={e.error ?? new Error('Actividad no encontrada.')} />;
  const { actividad: a, calificacion: c, curso, estado } = e;
  async function elegir(ev: ChangeEvent<HTMLInputElement>) {
    const archivo = ev.target.files?.[0]; ev.target.value = '';
    if (!archivo) return;
    setError(null); setConfirmar(false);
    const err = await e.entregar({ nombre: archivo.name, tamano: archivo.size, datos: archivo });
    setError(err); if (!err) toast('Entrega recibida');
  }
  return <>
    <Volver a={`/e/cursos/${cursoId}`} etiqueta={curso.nombre} />
    <Panel><span style={{ color: colors.textoTenue }}>{curso.nombre} · Corte {a.corte} · {a.peso}% del corte</span><h1 style={{ margin: 0, fontSize: 26 }}>{a.titulo}</h1><span><Chip estado={estado} /></span><p style={{ margin: 0 }}>{a.requiereEntrega ? `Fecha límite: ${fechaCompleta(a.vence)}` : 'No requiere entrega de archivo. El docente registrará la calificación.'}</p></Panel>
    {estado === 'calificada' && c && <Panel><div style={{ fontSize: 36, fontWeight: 600 }}>{formatoNota(c.nota)} <span style={{ fontSize: 16 }}>de 5.0 · nota de actividad publicada</span></div><h2 style={{ fontSize: 17 }}>Retroalimentación</h2><p style={{ margin: 0, lineHeight: 1.6 }}>{c.retro || 'Sin comentarios del docente.'}</p></Panel>}
    {c?.entregado && <Panel><h2 style={{ fontSize: 18, margin: 0 }}>Entrega recibida</h2><strong style={{ overflowWrap: 'anywhere' }}>{c.archivo}</strong><span>{fechaCompleta(c.entregado)}</span>{c.tamano !== undefined && <span>{(c.tamano / 1024 / 1024).toFixed(2)} MB</span>}<ArchivoLink calificacion={c} />{e.abierta && <><Boton variante="contorno" disabled={e.enviando} onClick={() => setConfirmar(true)}>Anular entrega</Boton>{confirmar && <div><p>La entrega dejará de estar activa. Tu calificación se conserva y podrás volver a entregar mientras el plazo siga abierto.</p><div className="au-toolbar"><Boton disabled={e.enviando} onClick={async () => { const err = await e.anular(); setError(err); if (!err) { setConfirmar(false); toast('Entrega anulada'); } }}>Confirmar anulación</Boton><Boton variante="contorno" onClick={() => setConfirmar(false)}>Cancelar</Boton></div></div>}</>}</Panel>}
    {e.abierta && <Panel><h2 style={{ fontSize: 18, margin: 0 }}>{c?.entregado ? 'Reemplazar archivo' : 'Enviar entrega'}</h2><p style={{ margin: 0 }}>Formatos: {EXTENSIONES_ENTREGA.join(', ').toUpperCase()} · máximo {TAMANO_MAX_ENTREGA / 1024 / 1024} MB.</p><label htmlFor="archivo-entrega">Seleccionar archivo</label><input id="archivo-entrega" type="file" accept={EXTENSIONES_ENTREGA.map(x => '.' + x).join(',')} onChange={elegir} disabled={e.enviando} /><span role="status">{e.enviando ? 'Enviando archivo…' : 'Al seleccionar un archivo se enviará tu entrega.'}</span></Panel>}
    {a.requiereEntrega && !e.abierta && <Panel><p style={{ margin: 0 }}>El plazo cerró el {fechaCompleta(a.vence)}. Ya no puedes enviar, reemplazar o anular la entrega.</p></Panel>}
    {error && <p role="alert" style={{ color: colors.error }}>{error}</p>}
  </>;
}

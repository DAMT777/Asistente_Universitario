import { useState, type ChangeEvent } from 'react';
import { useParams } from 'react-router-dom';
import type { ArchivoDeEntrega, ArchivoEntrega } from '@/types';
import { useEntrega, validarSeleccion } from '@/hooks';
import { EXTENSIONES_ENTREGA, MAX_ARCHIVOS_ENTREGA, TAMANO_MAX_ARCHIVO, TAMANO_MAX_ENTREGA, archivoEntregaSchema } from '@/schemas';
import { extension, fechaCorta, fechaLocal, formatoNota, formatoTamano } from '@/domain';
import { acento, colors } from '@/theme/tokens';
import { Barra, Boton, Cargando, Chip, ErrorEstado, FechaTile, Icono, icono, Panel, entrada } from '@/ui';
import { guardarArchivo } from '@/ui/guardarArchivo';
import { useToast } from '@/ui/Toast';
import { Volver } from '../cursos/compartido';

export function ActividadPage() {
  const { cursoId, actividadId } = useParams();
  const e = useEntrega(actividadId);
  const toast = useToast();
  const [seleccion, setSeleccion] = useState<File[]>([]);
  const [error, setError] = useState<string | null>(null);

  if (e.isLoading) return <Cargando />;
  if (!e.actividad || !e.curso || !e.estado) return <ErrorEstado error={e.error ?? new Error('Actividad no encontrada.')} />;
  const { actividad: a, calificacion: c, curso, estado, entrega } = e;
  const vigente = entrega?.estado === 'ENVIADA';

  const aArchivo = (f: File): ArchivoEntrega => ({ nombre: f.name, tamano: f.size, datos: f });
  const errorDe = (f: File) => {
    const v = archivoEntregaSchema.safeParse(aArchivo(f));
    return v.success ? undefined : v.error.issues[0].message;
  };
  const errorSeleccion = seleccion.length ? validarSeleccion(seleccion.map(aArchivo)) : null;
  // Los errores de un archivo ya se ven en su fila; abajo van los del conjunto y los del servidor.
  const errorGeneral = errorSeleccion && !seleccion.some((f) => errorDe(f) === errorSeleccion) ? errorSeleccion : error;
  const totalSeleccion = seleccion.reduce((s, f) => s + f.size, 0);

  /** Agrega a la selección (sin enviar). Un archivo repetido (mismo nombre y tamaño) no se duplica. */
  function alElegir(ev: ChangeEvent<HTMLInputElement>) {
    const nuevos = Array.from(ev.target.files ?? []);
    ev.target.value = '';
    setError(null);
    setSeleccion((actual) => [...actual, ...nuevos.filter((n) => !actual.some((x) => x.name === n.name && x.size === n.size))]);
  }

  async function alEntregar() {
    const err = await e.entregar(seleccion.map(aArchivo));
    setError(err);
    if (!err) {
      toast(vigente ? 'Entrega reemplazada' : 'Entrega enviada');
      setSeleccion([]);
    }
  }

  async function alAnular() {
    const err = await e.anular();
    setError(err);
    if (!err) toast('Entrega anulada');
  }

  async function alDescargar(archivo: ArchivoDeEntrega) {
    const r = await e.descargar(archivo.id);
    if ('error' in r) toast(r.error);
    else guardarArchivo(r.archivo.datos, r.archivo.nombre);
  }

  return (
    <>
      <Volver a={`/e/cursos/${cursoId}`} etiqueta={curso.nombre} />
      <div style={{ position: 'relative', overflow: 'hidden', background: colors.superficie, border: '1px solid rgba(255,255,255,0.09)', borderRadius: 20, backdropFilter: 'blur(14px)', padding: 22, display: 'flex', alignItems: 'flex-start', gap: 16, ...entrada(0) }}>
        <div style={{ position: 'absolute', inset: 0, background: `radial-gradient(circle at 0% 0%, ${acento(curso.acento, 0.32)} 0%, transparent 55%)`, pointerEvents: 'none' }} />
        <div style={{ position: 'relative' }}><FechaTile fecha={a.vence} grande /></div>
        <div style={{ position: 'relative', display: 'flex', flexDirection: 'column', gap: 8, minWidth: 0 }}>
          <span style={{ fontSize: 13, color: colors.textoTenue }}>{curso.nombre} · Corte {a.corte} · {a.peso}% del corte</span>
          <h1 style={{ margin: 0, fontSize: 24, fontWeight: 600 }}>{a.titulo}</h1>
          <span style={{ alignSelf: 'flex-start' }}><Chip estado={estado} /></span>
        </div>
      </div>

      {estado === 'calificada' && c && (
        <Panel indice={1} style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 20 }}>
          <div style={{ display: 'flex', alignItems: 'baseline', gap: 8 }}>
            <span style={{ fontSize: 48, fontWeight: 600, lineHeight: 1 }}>{formatoNota(c.nota)}</span>
            <span style={{ fontSize: 14, color: colors.textoTenue }}>de 5.0</span>
          </div>
          <div style={{ flex: 1, minWidth: 220, display: 'flex', flexDirection: 'column', gap: 6 }}>
            <span style={{ fontSize: 11, fontWeight: 600, letterSpacing: '0.06em', color: colors.textoTenue }}>RETROALIMENTACIÓN</span>
            <p style={{ margin: 0, fontSize: 15, lineHeight: 1.55, color: colors.textoSuave }}>{c.retro || 'Sin comentarios del docente.'}</p>
          </div>
        </Panel>
      )}

      {entrega && (
        <Panel padding={16}>
          <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', gap: 12 }}>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <span style={{ fontSize: 16, fontWeight: 600 }}>{vigente ? 'Tu entrega' : 'Entrega anulada'}</span>
              <span style={{ fontSize: 13, color: colors.textoTenue }}>
                {vigente ? 'Enviada' : 'Enviada antes'} el {fechaCorta(fechaLocal(entrega.fechaEnvio))} · {entrega.archivos.length} {entrega.archivos.length === 1 ? 'archivo' : 'archivos'} · {formatoTamano(entrega.tamanoTotal)}
              </span>
            </div>
            {e.puedeAnular && <Boton variante="contorno" onClick={alAnular} disabled={e.enviando}>Anular entrega</Boton>}
          </div>
          {entrega.archivos.map((x) => (
            <FilaArchivo key={x.id} nombre={x.nombreArchivo} tamano={x.tamano} atenuado={!vigente} onClick={() => alDescargar(x)} titulo={`Descargar ${x.nombreArchivo}`}>
              <Icono d={icono.descargar} tam={18} />
            </FilaArchivo>
          ))}
        </Panel>
      )}

      {e.abierta && (
        <Panel padding={16}>
          <span style={{ fontSize: 16, fontWeight: 600 }}>{vigente ? 'Reemplazar entrega' : 'Nueva entrega'}</span>
          <label style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 6, minHeight: 120, border: '1.5px dashed rgba(255,255,255,0.22)', borderRadius: 16, background: 'rgba(26,24,28,0.5)', cursor: e.enviando ? 'default' : 'pointer', padding: 20, textAlign: 'center', opacity: e.enviando ? 0.6 : 1 }}>
            <div style={{ width: 44, height: 44, borderRadius: '50%', background: 'rgba(200,16,46,0.18)', color: colors.marcaTexto, display: 'flex', alignItems: 'center', justifyContent: 'center' }}><Icono d={icono.subir} tam={20} /></div>
            <span style={{ fontSize: 15, fontWeight: 600 }}>{seleccion.length ? 'Agregar más archivos' : 'Selecciona tus archivos'}</span>
            <span style={{ fontSize: 13, color: colors.textoTenue }}>
              PDF, Word, Excel, PowerPoint, ZIP o imagen · hasta {MAX_ARCHIVOS_ENTREGA} archivos · {formatoTamano(TAMANO_MAX_ARCHIVO)} por archivo · {formatoTamano(TAMANO_MAX_ENTREGA)} en total
            </span>
            <input type="file" multiple accept={ACEPTA} onChange={alElegir} disabled={e.enviando} style={{ display: 'none' }} />
          </label>

          {seleccion.map((f) => (
            <FilaArchivo key={`${f.name}-${f.size}`} nombre={f.name} tamano={f.size} error={errorDe(f)}
              onClick={() => setSeleccion((s) => s.filter((x) => x !== f))} titulo={`Quitar ${f.name}`} deshabilitado={e.enviando}>
              <Icono d={icono.quitar} tam={16} />
            </FilaArchivo>
          ))}

          {seleccion.length > 0 && (
            <>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13, color: totalSeleccion > TAMANO_MAX_ENTREGA ? colors.error : colors.textoTenue }}>
                  <span>{seleccion.length} de {MAX_ARCHIVOS_ENTREGA} archivos</span>
                  <span>{formatoTamano(totalSeleccion)} de {formatoTamano(TAMANO_MAX_ENTREGA)}</span>
                </div>
                <Barra valor={Math.min(100, (totalSeleccion / TAMANO_MAX_ENTREGA) * 100)} color={totalSeleccion > TAMANO_MAX_ENTREGA ? colors.error : acento(curso.acento)} alto={4} />
              </div>
              {vigente && <span style={{ fontSize: 13, color: colors.textoMedio }}>Al entregar, estos archivos reemplazan los {entrega!.archivos.length} de tu entrega actual.</span>}
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10, justifyContent: 'flex-end' }}>
                <Boton variante="contorno" onClick={() => { setSeleccion([]); setError(null); }} disabled={e.enviando}>Cancelar</Boton>
                <Boton onClick={alEntregar} disabled={e.enviando || !!errorSeleccion}>{e.enviando ? 'Entregando…' : 'Entregar'}</Boton>
              </div>
            </>
          )}
          {errorGeneral && <span role="alert" style={{ fontSize: 13, color: colors.error, fontWeight: 500 }}>{errorGeneral}</span>}
        </Panel>
      )}

      {estado === 'vencida' && (
        <div style={{ background: 'rgba(224,41,58,0.14)', border: '1px solid rgba(224,41,58,0.3)', color: '#FFC2C7', borderRadius: 14, padding: '14px 16px', fontSize: 14 }}>
          El plazo cerró el {fechaCorta(a.vence)}. Ya no se reciben entregas.
        </div>
      )}

      {estado === 'calificada' && !e.abierta && (
        <div style={{ background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.12)', color: colors.textoMedio, borderRadius: 14, padding: '14px 16px', fontSize: 14 }}>
          La actividad ya está calificada: no se reciben más entregas.
        </div>
      )}
    </>
  );
}

/** Un archivo con su tipo y tamaño. Toda la fila es el botón de la acción (descargar o quitar). */
function FilaArchivo({ nombre, tamano, error, atenuado, deshabilitado, onClick, titulo, children }: {
  nombre: string; tamano: number; error?: string; atenuado?: boolean; deshabilitado?: boolean;
  onClick: () => void; titulo: string; children: React.ReactNode;
}) {
  return (
    <button type="button" onClick={onClick} disabled={deshabilitado} title={titulo} aria-label={titulo} className="au-hover-row"
      style={{ display: 'flex', alignItems: 'center', gap: 12, width: '100%', padding: '10px 12px', border: `1px solid ${error ? 'rgba(224,41,58,0.4)' : 'rgba(255,255,255,0.08)'}`, borderRadius: 12, background: 'rgba(255,255,255,0.03)', color: colors.texto, textAlign: 'left', cursor: deshabilitado ? 'default' : 'pointer', opacity: atenuado ? 0.65 : 1 }}>
      <span style={{ width: 40, height: 40, flex: 'none', borderRadius: 10, background: 'rgba(47,197,155,0.16)', color: colors.exito, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 10, fontWeight: 700 }}>{extension(nombre) || '—'}</span>
      <span style={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column', gap: 2 }}>
        <span style={{ fontSize: 14, fontWeight: 600, wordBreak: 'break-all' }}>{nombre}</span>
        <span style={{ fontSize: 12, color: error ? colors.error : colors.textoTenue }}>{error ?? formatoTamano(tamano)}</span>
      </span>
      <span style={{ flex: 'none', color: colors.textoMedio, display: 'flex' }}>{children}</span>
    </button>
  );
}

const ACEPTA = EXTENSIONES_ENTREGA.map((x) => `.${x}`).join(',');

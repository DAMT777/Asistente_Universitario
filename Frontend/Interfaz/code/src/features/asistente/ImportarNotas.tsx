import { useRef, useState, type CSSProperties, type DragEvent, type FormEvent, type ReactNode, type RefObject } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import type { NotaParaImportar } from '@/api/contratos';
import { useApi, useCursos } from '@/hooks';
import { colors, font, space } from '@/theme/tokens';
import { Boton, Icono, icono } from '@/ui';

import { leerNotas } from './leerNotas';

type Fila = NotaParaImportar & { codigo: string; nombre: string; anterior?: number };
type Aviso = { tipo: 'ok' | 'error'; texto: string };

const etiqueta: CSSProperties = { display: 'flex', flexDirection: 'column', gap: 4, minWidth: 0, fontSize: font.size.xxs, fontWeight: font.weight.semibold, letterSpacing: '0.06em', textTransform: 'uppercase', color: colors.textoTenue };
const botonIcono: CSSProperties = { width: 30, height: 30, border: 0, borderRadius: 8, background: 'transparent', color: colors.textoMedio, display: 'flex', alignItems: 'center', justifyContent: 'center', flex: 'none' };

function tamano(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} KB`;
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}

export function ImportarNotas({ id, abierto, amplio, campo, onCerrar }: {
  id: string;
  abierto: boolean;
  /** Ocupa todo el alto libre del chat (cuando aún no hay conversación). */
  amplio?: boolean;
  campo: RefObject<HTMLInputElement>;
  onCerrar: () => void;
}) {
  const { api } = useApi();
  const cache = useQueryClient();
  const cursos = useCursos();
  const materia = useRef<HTMLSelectElement>(null);
  const [curso, setCurso] = useState('');
  const [actividad, setActividad] = useState('');
  const actividades = useQuery({ queryKey: ['importar-actividades', curso], queryFn: () => api.actividades.listar(curso), enabled: !!curso });
  const [archivo, setArchivo] = useState<File>();
  const [filas, setFilas] = useState<Fila[]>([]);
  const [aviso, setAviso] = useState<Aviso>();
  const [ocupado, setOcupado] = useState(false);
  const [arrastrando, setArrastrando] = useState(false);

  const { notasParaImportar, importarNotas } = api.asistente;

  if (!notasParaImportar || !importarNotas) {
    return (
      <Contenedor id={id} abierto={abierto} amplio={amplio} titulo="Importar notas desde Excel" onCerrar={onCerrar}>
        <div className="au-excel-aviso" data-tipo="info">
          <Icono d={icono.alerta} tam={16} />
          <span>Conecta la aplicación al backend e inicia sesión como profesor para importar calificaciones.</span>
        </div>
      </Contenedor>
    );
  }

  function limpiar() {
    setFilas([]);
    setAviso(undefined);
  }

  function elegir(f: File) {
    if (!f.name.toLowerCase().endsWith('.xlsx')) {
      setAviso({ tipo: 'error', texto: 'Solo se admiten archivos de Excel (.xlsx).' });
      return;
    }
    setArchivo(f);
    limpiar();
    materia.current?.focus();
  }

  function soltar(e: DragEvent<HTMLElement>) {
    e.preventDefault();
    setArrastrando(false);
    const f = e.dataTransfer.files?.[0];
    if (f && !ocupado) elegir(f);
  }

  async function revisar(e: FormEvent) {
    e.preventDefault();
    if (!archivo || !curso || !actividad || !notasParaImportar) return;
    limpiar();
    setOcupado(true);
    try {
      const [datos, estudiantes, existentes] = await Promise.all([
        leerNotas(archivo), api.cursos.estudiantes(curso), notasParaImportar(actividad),
      ]);
      const inscritos = new Map(estudiantes.map(e => [e.codigo.toUpperCase(), e]));
      const notas = new Map(existentes.map(n => [n.estudianteId, n]));
      setFilas(datos.map(({ codigo, nota, numero }) => {
        const estudiante = inscritos.get(codigo);
        if (!estudiante) throw new Error(`Fila ${numero}: ${codigo} no está inscrito en la materia.`);
        const anterior = notas.get(estudiante.id);
        return { codigo, nombre: estudiante.nombre, nota, estudianteId: estudiante.id,
          existia: !!anterior, version: anterior?.version ?? null, anterior: anterior?.valor };
      }));
    } catch (error) {
      setAviso({ tipo: 'error', texto: error instanceof Error ? error.message : 'No se pudo revisar el archivo.' });
    } finally {
      setOcupado(false);
    }
  }

  async function confirmar() {
    if (!importarNotas || !filas.length) return;
    setOcupado(true);
    try {
      const total = await importarNotas(actividad, filas.map(({ estudianteId, nota, existia, version }) =>
        ({ estudianteId, nota, existia, version })));
      limpiar();
      setArchivo(undefined);
      if (campo.current) campo.current.value = '';
      setAviso({ tipo: 'ok', texto: total ? `Notas guardadas en borrador: ${total}.` : 'Las notas ya tenían esos valores.' });
      void cache.invalidateQueries();
    } catch (error) {
      setFilas([]);
      setAviso({ tipo: 'error', texto: error instanceof Error ? error.message : 'No se pudo guardar la importación.' });
    } finally {
      setOcupado(false);
    }
  }

  const cambian = filas.filter(f => f.anterior !== f.nota).length;

  return (
    <Contenedor id={id} abierto={abierto} amplio={amplio} titulo="Importar notas desde Excel" onCerrar={onCerrar} ocupado={ocupado}>
      <input ref={campo} type="file" accept=".xlsx" hidden onChange={e => { const f = e.target.files?.[0]; if (f) elegir(f); }} />

      {archivo ? (
        <div className="au-excel-archivo">
          <span className="au-excel-icono" data-tipo="hoja"><Icono d={icono.hoja} tam={20} /></span>
          <div style={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column', lineHeight: 1.3 }}>
            <strong style={{ fontSize: font.size.sm, fontWeight: font.weight.semibold, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }} title={archivo.name}>{archivo.name}</strong>
            <span style={{ fontSize: font.size.xxs, color: colors.textoTenue }}>Excel · {tamano(archivo.size)}</span>
          </div>
          <button type="button" className="au-excel-enlace" disabled={ocupado} onClick={() => campo.current?.click()}>Cambiar</button>
        </div>
      ) : (
        <button type="button" className="au-excel-zona" data-arrastrando={arrastrando || undefined} disabled={ocupado}
          onClick={() => campo.current?.click()}
          onDragOver={e => { e.preventDefault(); setArrastrando(true); }}
          onDragLeave={() => setArrastrando(false)}
          onDrop={soltar}>
          <span className="au-excel-icono"><Icono d={icono.subir} tam={20} /></span>
          <span style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-start', lineHeight: 1.3, textAlign: 'left' }}>
            <strong style={{ fontSize: font.size.sm, fontWeight: font.weight.semibold, color: colors.texto }}>Elige un archivo .xlsx</strong>
            <span style={{ fontSize: font.size.xxs, color: colors.textoTenue }}>o arrástralo aquí</span>
          </span>
        </button>
      )}

      <form onSubmit={e => void revisar(e)}>
        <fieldset disabled={ocupado} style={{ display: 'flex', flexDirection: 'column', gap: space[3], border: 0, padding: 0, margin: 0, minWidth: 0 }}>
          <label style={etiqueta}>Materia
            <Selector>
              <select ref={materia} aria-label="Materia" required value={curso} onChange={e => { setCurso(e.target.value); setActividad(''); limpiar(); }}>
                <option value="">Selecciona la materia</option>
                {cursos.data?.map(c => <option key={c.id} value={c.id}>{c.codigo} · {c.nombre} · Grupo {c.grupo}</option>)}
              </select>
            </Selector>
          </label>
          <label style={etiqueta}>Actividad
            <Selector>
              <select aria-label="Actividad" required disabled={!curso} value={actividad} onChange={e => { setActividad(e.target.value); limpiar(); }}>
                <option value="">{curso ? 'Selecciona la actividad' : 'Primero elige la materia'}</option>
                {curso && actividades.data?.map(a => <option key={a.id} value={a.id}>{a.titulo} · Corte {a.corte}</option>)}
              </select>
            </Selector>
          </label>
          <p className="au-excel-ayuda">
            Primera hoja con columnas <code>codigo</code> y <code>nota</code>, hasta 250 filas.
          </p>
          {filas.length === 0 && (
            <Boton type="submit" disabled={!archivo || !actividad || ocupado} style={{ width: '100%', height: 38 }}>
              {ocupado ? 'Revisando…' : 'Revisar notas'}
            </Boton>
          )}
        </fieldset>
      </form>

      {filas.length > 0 && (
        <div className="au-excel-revision">
          <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', gap: space[2] }}>
            <strong style={{ fontSize: font.size.sm, fontWeight: font.weight.semibold }}>Revisa {filas.length} {filas.length === 1 ? 'nota' : 'notas'}</strong>
            <span style={{ fontSize: font.size.xxs, color: colors.textoTenue }}>{cambian} {cambian === 1 ? 'cambia' : 'cambian'}</span>
          </div>
          <div className="au-excel-tabla">
            <table>
              <thead><tr><th scope="col">Estudiante</th><th scope="col">Actual</th><th scope="col">Nueva</th></tr></thead>
              <tbody>{filas.map(f => {
                const cambia = f.anterior !== f.nota;
                return (
                  <tr key={f.estudianteId}>
                    <td>
                      <span style={{ display: 'block', fontWeight: font.weight.medium }}>{f.nombre}</span>
                      <span style={{ fontSize: font.size.xxs, color: colors.textoTenue }}>{f.codigo}</span>
                    </td>
                    <td style={{ color: colors.textoTenue }}>{f.anterior ?? '—'}</td>
                    <td><span className="au-excel-nota" data-cambia={cambia || undefined}>{f.nota}</span></td>
                  </tr>
                );
              })}</tbody>
            </table>
          </div>
          <div style={{ display: 'flex', gap: space[2] }}>
            <Boton disabled={ocupado} onClick={() => void confirmar()} style={{ flex: 1, minWidth: 0, height: 38, padding: '0 10px' }}>
              {ocupado ? 'Guardando…' : 'Guardar en borrador'}
            </Boton>
            <Boton variante="contorno" disabled={ocupado} onClick={limpiar} style={{ flex: 1, minWidth: 0, height: 38, padding: '0 10px' }}>Cancelar</Boton>
          </div>
        </div>
      )}

      {aviso && (
        <div role="status" className="au-excel-aviso" data-tipo={aviso.tipo}>
          <Icono d={aviso.tipo === 'ok' ? icono.revisado : icono.alerta} tam={16} />
          <span>{aviso.texto}</span>
        </div>
      )}
    </Contenedor>
  );
}

function Contenedor({ id, abierto, amplio, titulo, onCerrar, ocupado, children }: {
  id: string; abierto: boolean; amplio?: boolean; titulo: string; onCerrar: () => void; ocupado?: boolean; children: ReactNode;
}) {
  return (
    <section id={id} hidden={!abierto} aria-label="Adjuntar Excel de notas" aria-busy={ocupado || undefined} data-amplio={amplio || undefined} className="au-excel">
      <div style={{ display: 'flex', alignItems: 'center', gap: space[2] }}>
        <span style={{ flex: 1, fontSize: font.size.xs, fontWeight: font.weight.semibold, letterSpacing: '0.06em', textTransform: 'uppercase', color: colors.textoTenue }}>{titulo}</span>
        <button type="button" className="au-chat-accion" disabled={ocupado} onClick={onCerrar} aria-label="Cerrar el panel de Excel" title="Cerrar" style={botonIcono}>
          <Icono d={icono.cerrar} tam={16} />
        </button>
      </div>
      {children}
    </section>
  );
}

function Selector({ children }: { children: ReactNode }) {
  return (
    <span className="au-excel-select">
      {children}
      <Icono d={icono.desplegar} tam={16} />
    </span>
  );
}

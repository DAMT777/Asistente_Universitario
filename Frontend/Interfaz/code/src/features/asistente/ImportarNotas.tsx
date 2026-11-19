import { useRef, useState, type FormEvent, type RefObject } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import type { NotaParaImportar } from '@/api/contratos';
import { useApi, useCursos } from '@/hooks';
import { colors } from '@/theme/tokens';

import { leerNotas } from './leerNotas';

type Fila = NotaParaImportar & { codigo: string; nombre: string; anterior?: number };

export function ImportarNotas({ id, abierto, campo, onCerrar }: {
  id: string;
  abierto: boolean;
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
  const [mensaje, setMensaje] = useState('');
  const [ocupado, setOcupado] = useState(false);

  const { notasParaImportar, importarNotas } = api.asistente;

  if (!notasParaImportar || !importarNotas) {
    return (
      <section id={id} hidden={!abierto} aria-label="Adjuntar Excel de notas" style={{ padding: '6px 12px' }}>
        <button type="button" onClick={onCerrar}>Cerrar adjunto</button>
        <p>Conecta la aplicación al backend e inicia sesión como profesor para importar calificaciones.</p>
      </section>
    );
  }

  function limpiar() {
    setFilas([]);
    setMensaje('');
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
      setMensaje(error instanceof Error ? error.message : 'No se pudo revisar el archivo.');
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
      setMensaje(total ? `Notas guardadas en borrador: ${total}.` : 'Las notas ya tenían esos valores.');
      void cache.invalidateQueries();
    } catch (error) {
      setFilas([]);
      setMensaje(error instanceof Error ? error.message : 'No se pudo guardar la importación.');
    } finally {
      setOcupado(false);
    }
  }

  return (
    <section id={id} hidden={!abierto} aria-label="Adjuntar Excel de notas" style={{ padding: '6px 12px', flexShrink: 0, maxHeight: '54%', overflow: 'auto', borderTop: '1px solid var(--au-linea)' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 8 }}>
        <strong style={{ overflowWrap: 'anywhere', color: colors.marcaTexto }}>{archivo?.name ?? 'Adjuntar Excel de notas (.xlsx)'}</strong>
        <button type="button" disabled={ocupado} onClick={onCerrar}>Cerrar</button>
      </div>
      <form onSubmit={e => void revisar(e)}>
        <fieldset disabled={ocupado} style={{ display: 'grid', gap: 8, border: 0, padding: '8px 0', margin: 0, minWidth: 0 }}>
          <label>Materia <select ref={materia} aria-label="Materia" required value={curso} onChange={e => { setCurso(e.target.value); setActividad(''); limpiar(); }}>
            <option value="">Seleccionar</option>
            {cursos.data?.map(c => <option key={c.id} value={c.id}>{c.codigo} · {c.nombre} · Grupo {c.grupo}</option>)}
          </select></label>
          <label>Actividad <select aria-label="Actividad" required disabled={!curso} value={actividad} onChange={e => { setActividad(e.target.value); limpiar(); }}>
            <option value="">Seleccionar</option>
            {curso && actividades.data?.map(a => <option key={a.id} value={a.id}>{a.titulo} · Corte {a.corte}</option>)}
          </select></label>
          <input ref={campo} type="file" accept=".xlsx" hidden onChange={e => { setArchivo(e.target.files?.[0]); limpiar(); materia.current?.focus(); }} />
          <small>Primera hoja con encabezados <strong>codigo</strong> y <strong>nota</strong>. Hasta 250 filas.</small>
          <button type="submit" disabled={!archivo || !actividad}>Revisar notas</button>
        </fieldset>
      </form>
      {filas.length > 0 && (
        <div>
          <p>Revisa las {filas.length} notas antes de guardar.</p>
          <div style={{ maxHeight: 140, overflow: 'auto' }}>
            <table>
              <thead><tr><th>Estudiante</th><th>Actual</th><th>Nueva</th></tr></thead>
              <tbody>{filas.map(f => <tr key={f.estudianteId}>
                <td>{f.nombre} ({f.codigo})</td><td>{f.anterior ?? '—'}</td><td>{f.nota}</td>
              </tr>)}</tbody>
            </table>
          </div>
          <button type="button" disabled={ocupado} onClick={() => void confirmar()}>Guardar en borrador</button>
          <button type="button" disabled={ocupado} onClick={limpiar}>Cancelar</button>
        </div>
      )}
      {ocupado && <small role="status">Procesando…</small>}
      {mensaje && <p role="status">{mensaje}</p>}
    </section>
  );
}

import { useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import type { Corte } from '@/types';
import { useApi, useCalificaciones, usePonderado } from '@/hooks';
import { usePublicaciones } from '@/hooks/usePublicaciones';
import { fechaCompleta, formatoNota } from '@/domain';
import { Boton, Cargando, ErrorEstado, Panel, Vacio } from '@/ui';
import { colors } from '@/theme/tokens';
import { useToast } from '@/ui/Toast';

export function NotasCorteTab({ cursoId }: { cursoId: string }) {
  const p = usePonderado(cursoId);
  const cal = useCalificaciones(cursoId, undefined);
  const publicaciones = usePublicaciones(cursoId);
  const { api } = useApi();
  const qc = useQueryClient();
  const toast = useToast();
  const [corte, setCorte] = useState<Corte>(1);
  const [busqueda, setBusqueda] = useState('');
  const [omitir, setOmitir] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const fallo = p.error ?? cal.error ?? publicaciones.error;
  const reintentar = () => Promise.all([p.refetch(), cal.refetch(), publicaciones.refetch()]);
  if (fallo) return <ErrorEstado error={fallo} onReintentar={reintentar} />;
  if (!p.resumenEstudiantes || !publicaciones.data || cal.isLoading) return <Cargando />;
  const filas = p.resumenEstudiantes.filter(f => `${f.estudiante.nombre} ${f.estudiante.codigo}`.toLowerCase().includes(busqueda.toLowerCase()));

  async function publicar(estudianteId: string, corregir: boolean) {
    setEnviando(true); setError(null);
    try {
      await api.cortes.publicar({ cursoId, estudianteId, corte, omitirBorradores: omitir, corregir });
      await qc.invalidateQueries({ queryKey: ['publicaciones'] });
      toast(corregir ? 'Publicación del estudiante corregida' : 'Corte publicado para el estudiante');
    } catch (e) { setError(e instanceof Error ? e.message : 'No se pudo publicar el corte.'); }
    finally { setEnviando(false); }
  }

  return <>
    <Panel>
      <h2>Notas por corte</h2>
      <p style={{ margin: 0, color: colors.textoMedio }}>Revisa el avance calculado y publícalo por estudiante. Publicar una actividad no publica automáticamente el corte. Las correcciones actualizan únicamente al estudiante elegido.</p>
      <div className="au-toolbar">
        <label>Corte <select value={corte} onChange={e => { setCorte(Number(e.target.value) as Corte); setOmitir(false); setError(null); }}><option value={1}>1</option><option value={2}>2</option><option value={3}>3</option></select></label>
        <input aria-label="Buscar estudiante" placeholder="Nombre o código del estudiante" value={busqueda} onChange={e => setBusqueda(e.target.value)} />
      </div>
      <label><input type="checkbox" checked={omitir} onChange={e => setOmitir(e.target.checked)} /> Omitir expresamente las notas en borrador al publicar. Solo se incluirán las actividades publicadas.</label>
      {error && <p role="alert" style={{ color: colors.error }}>{error}</p>}
    </Panel>
    {!filas.length ? <Vacio>No hay estudiantes que coincidan con la búsqueda.</Vacio> : <div className="au-table-wrap"><table className="au-table">
      <caption>Publicación del corte {corte}</caption>
      <thead><tr><th>Estudiante</th><th>Avance del corte</th><th>Nota publicada</th><th>Estado</th><th>Acción</th></tr></thead>
      <tbody>{filas.map(({ estudiante, resumen }) => {
        const pub = publicaciones.data.find(x => x.estudianteId === estudiante.id && x.corte === corte);
        const avance = resumen.cortes[corte - 1];
        const ids = new Set((cal.actividades ?? []).filter(a => a.corte === corte).map(a => a.id));
        const borradores = (cal.calificaciones ?? []).filter(c => c.estudianteId === estudiante.id && ids.has(c.actividadId) && c.estado === 'borrador').length;
        return <tr key={estudiante.id}>
          <th scope="row">{estudiante.nombre}<small>{estudiante.codigo}</small></th>
          <td>{formatoNota(avance.nota)}<small>{Math.round(avance.evaluado / (avance.peso / 100))}% del corte evaluado</small></td>
          <td>{pub ? formatoNota(pub.nota) : 'Sin publicar'}{pub?.fechaPublicacion && <small>{fechaCompleta(pub.fechaPublicacion)}</small>}</td>
          <td>{pub ? 'Publicado' : 'Pendiente'}{borradores > 0 && <small>{borradores} notas en borrador. Publícalas u omítelas expresamente.</small>}</td>
          <td><Boton disabled={enviando || avance.nota === null || (borradores > 0 && !omitir)} onClick={() => publicar(estudiante.id, !!pub)}>{enviando ? 'Procesando…' : pub ? 'Corregir publicación' : 'Publicar corte'}</Boton></td>
        </tr>;
      })}</tbody>
    </table></div>}
  </>;
}

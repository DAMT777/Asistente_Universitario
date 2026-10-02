import { useParams, useSearchParams } from 'react-router-dom';
import { useCurso, useActividades, useEstudiantes } from '@/hooks';
import { Cargando, ErrorEstado, Pestanas } from '@/ui';
import { BannerCurso, Volver } from '../cursos/compartido';
import { ActividadesTab } from './ActividadesTab';
import { CalificarTab } from './CalificarTab';
import { NotasCorteTab } from './NotasCorteTab';
import { PonderadoTab } from './PonderadoTab';

type Tab = 'actividades' | 'calificar' | 'ponderado' | 'cortes';

export function CursoDocente() {
  const { cursoId } = useParams();
  const [params, setParams] = useSearchParams();
  const tab = (params.get('tab') as Tab) ?? 'actividades';
  const curso = useCurso(cursoId);
  const acts = useActividades(cursoId);
  const ests = useEstudiantes(cursoId);

  const refetch = () => Promise.all([curso.refetch(), acts.refetch(), ests.refetch()]);
  if (curso.error || acts.error || ests.error) return <ErrorEstado error={curso.error ?? acts.error ?? ests.error} onReintentar={refetch} />;
  if (curso.isLoading || acts.isLoading || ests.isLoading) return <Cargando />;
  if (!curso.data) return <ErrorEstado error={curso.error ?? new Error('Curso no encontrado.')} />;

  const irA = (t: Tab, actividad?: string) => setParams(actividad ? { tab: t, actividad } : { tab: t }, { replace: true });

  return (
    <>
      <Volver a="/d/cursos" etiqueta="Cursos" />
      <BannerCurso curso={curso.data} cifra={String(ests.data?.length ?? '—')} cifraEtiqueta={`estudiantes · ${acts.data?.length ?? 0} actividades`} />
      <Pestanas<Tab> valor={tab} onCambio={(t) => irA(t)} opciones={[{ id: 'actividades', etiqueta: 'Actividades' }, { id: 'calificar', etiqueta: 'Calificar' }, { id: 'ponderado', etiqueta: 'Pesos y avance' }, { id: 'cortes', etiqueta: 'Notas por corte' }]} />
      {tab === 'actividades' && <ActividadesTab curso={curso.data} onCalificar={(id) => irA('calificar', id)} />}
      {tab === 'calificar' && <CalificarTab cursoId={curso.data.id} actividadId={params.get('actividad') ?? undefined} onElegir={(id) => irA('calificar', id)} />}
      {tab === 'cortes' && <NotasCorteTab cursoId={curso.data.id} />}
      {tab === 'ponderado' && <PonderadoTab cursoId={curso.data.id} />}
    </>
  );
}

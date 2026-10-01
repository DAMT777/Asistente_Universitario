import { useQuery } from '@tanstack/react-query';
import { useApi } from './ApiContext';
import { qk } from './queryKeys';

export function useCursos() {
  const { api } = useApi();
  return useQuery({ queryKey: qk.cursos, queryFn: () => api.cursos.listar() });
}

export function useCurso(cursoId: string | undefined) {
  const q = useCursos();
  return { ...q, data: q.data?.find((c) => c.id === cursoId) };
}

export function useActividades(cursoId?: string) {
  const { api } = useApi();
  return useQuery({ queryKey: qk.actividades(cursoId), queryFn: () => api.actividades.listar(cursoId) });
}

export function useEstudiantes(cursoId: string | undefined) {
  const { api } = useApi();
  return useQuery({ queryKey: qk.estudiantes(cursoId ?? ''), queryFn: () => api.cursos.estudiantes(cursoId!), enabled: !!cursoId });
}

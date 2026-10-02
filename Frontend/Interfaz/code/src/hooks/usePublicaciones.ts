import { useQuery } from '@tanstack/react-query';
import { useApi } from './ApiContext';
import { useUsuario } from './useSesion';

export function usePublicaciones(cursoId?: string) {
  const { api } = useApi();
  const usuario = useUsuario();
  return useQuery({ queryKey: ['publicaciones', usuario.id, cursoId ?? 'todos'], queryFn: () => api.cortes.listar(cursoId) });
}

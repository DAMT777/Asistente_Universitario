import { Navigate, Outlet, createBrowserRouter } from 'react-router-dom';
import type { Rol } from '@/types';
import { useSesion } from '@/hooks';
import { Cargando } from '@/ui';
import { LoginPage } from '@/features/auth/LoginPage';
import { AppShell } from '@/features/layout/AppShell';
import { InicioEstudiante } from '@/features/estudiante/InicioEstudiante';
import { CursosEstudiante, MisNotas } from '@/features/estudiante/MisNotas';
import { CursoEstudiante } from '@/features/estudiante/CursoEstudiante';
import { ActividadPage } from '@/features/estudiante/ActividadPage';
import { CursosDocente, InicioDocente } from '@/features/docente/InicioDocente';
import { CursoDocente } from '@/features/docente/CursoDocente';

function RequiereRol({ rol }: { rol: Rol }) {
  const { sesion, cargando } = useSesion();
  if (cargando) return <div style={{ padding: 24 }}><Cargando /></div>;
  if (!sesion) return <Navigate to="/login" replace />;
  if (sesion.usuario.rol !== rol) return <Navigate to={sesion.usuario.rol === 'docente' ? '/d' : '/e'} replace />;
  return <Outlet />;
}

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  {
    element: <RequiereRol rol="estudiante" />,
    children: [{
      path: '/e', element: <AppShell />,
      children: [
        { index: true, element: <InicioEstudiante /> },
        { path: 'cursos', element: <CursosEstudiante /> },
        { path: 'cursos/:cursoId', element: <CursoEstudiante /> },
        { path: 'cursos/:cursoId/actividades/:actividadId', element: <ActividadPage /> },
        { path: 'notas', element: <MisNotas /> },
      ],
    }],
  },
  {
    element: <RequiereRol rol="docente" />,
    children: [{
      path: '/d', element: <AppShell />,
      children: [
        { index: true, element: <InicioDocente /> },
        { path: 'cursos', element: <CursosDocente /> },
        { path: 'cursos/:cursoId', element: <CursoDocente /> },
      ],
    }],
  },
  { path: '*', element: <Navigate to="/login" replace /> },
]);

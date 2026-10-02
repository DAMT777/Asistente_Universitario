export const qk = {
  cursos: ['cursos'] as const,
  estudiantes: (cursoId: string) => ['estudiantes', cursoId] as const,
  actividades: (cursoId?: string) => ['actividades', cursoId ?? 'todas'] as const,
  califsActividad: (id: string) => ['califs', 'actividad', id] as const,
  califsCurso: (id: string) => ['califs', 'curso', id] as const,
  califsEstudiante: (id: string) => ['califs', 'estudiante', id] as const,
  califs: ['califs'] as const,
};

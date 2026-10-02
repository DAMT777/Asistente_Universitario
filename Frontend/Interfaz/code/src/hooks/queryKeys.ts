export const qk = {
  cursos: ['cursos'] as const,
  estudiantes: (cursoId: string) => ['estudiantes', cursoId] as const,
  actividades: (cursoId?: string) => ['actividades', cursoId ?? 'todas'] as const,
  califsActividad: (id: string) => ['califs', 'actividad', id] as const,
  califsCurso: (id: string) => ['califs', 'curso', id] as const,
  califs: ['califs'] as const,
  // Rol estudiante (backend real).
  estudiante: ['estudiante'] as const,
  matriz: ['estudiante', 'matriz'] as const,
  notasCurso: (cursoId: string) => ['estudiante', 'notas', cursoId] as const,
  misEntregas: ['estudiante', 'entregas'] as const,
};

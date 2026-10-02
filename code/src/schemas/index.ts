import { z } from 'zod';
import { NOTA_MAX, NOTA_MIN } from '@/domain/reglas';

export const loginSchema = z.object({
  usuario: z.string().trim().min(1, 'Escribe tu usuario.'),
  contrasena: z.string().min(6, 'La contraseña tiene al menos 6 caracteres.'),
  rol: z.enum(['estudiante', 'docente']),
});
export type LoginInput = z.infer<typeof loginSchema>;

/** Acepta "3.5" o "3,5"; una cifra entera y un decimal opcional. */
export const notaSchema = z
  .string()
  .trim()
  .min(1, 'Escribe una nota.')
  .transform((s) => s.replace(',', '.'))
  .refine((s) => /^\d(\.\d)?$/.test(s), 'Usa una cifra con un decimal, por ejemplo 3.5.')
  .transform(Number)
  .refine((n) => n >= NOTA_MIN && n <= NOTA_MAX, `La nota va de ${NOTA_MIN.toFixed(1)} a ${NOTA_MAX.toFixed(1)}.`);

export const calificacionInputSchema = z.object({
  actividadId: z.string(),
  estudianteId: z.string(),
  nota: notaSchema,
  retro: z.string().max(2000, 'Máximo 2000 caracteres.').default(''),
  publicar: z.boolean(),
});
export type CalificacionInput = z.input<typeof calificacionInputSchema>;

const pesoEntero = z.coerce.number().int('Usa números enteros.').min(1, 'Mayor que 0.').max(100);

export const actividadInputSchema = (hoy: string) =>
  z.object({
    cursoId: z.string(),
    titulo: z.string().trim().min(3, 'El título necesita al menos 3 caracteres.').max(120),
    corte: z.coerce.number().int().min(1).max(3) as unknown as z.ZodType<1 | 2 | 3>,
    peso: pesoEntero,
    vence: z
      .string()
      .min(1, 'Elige una fecha.')
      .refine((f) => f >= hoy, 'No puede ser anterior a hoy.'),
  });
export type ActividadInput = z.infer<ReturnType<typeof actividadInputSchema>>;

const sumaCien = (arr: number[]) => arr.reduce((a, b) => a + b, 0) === 100;

export const pesosCursoSchema = z.object({
  cortes: z.tuple([pesoEntero, pesoEntero, pesoEntero]).refine(sumaCien, 'Los cortes deben sumar 100%.'),
  actividades: z.record(z.string(), pesoEntero),
});
export type PesosCursoInput = z.infer<typeof pesosCursoSchema>;

export const EXTENSIONES_ENTREGA = ['pdf', 'docx', 'zip'] as const;
export const TAMANO_MAX_ENTREGA = 10 * 1024 * 1024;

export const archivoEntregaSchema = z.object({
  nombre: z
    .string()
    .refine((n) => EXTENSIONES_ENTREGA.some((e) => n.toLowerCase().endsWith('.' + e)), 'Formato no admitido. Usa PDF, DOCX o ZIP.'),
  tamano: z.number().max(TAMANO_MAX_ENTREGA, 'El archivo supera 10 MB.'),
  datos: z.unknown().optional(),
});

// Contratos de respuesta del backend (validación en el borde de la api).
export const cursoSchema = z.object({
  id: z.string(), codigo: z.string(), nombre: z.string(), grupo: z.number(), creditos: z.number(),
  periodo: z.string(), docente: z.string(), pesos: z.tuple([z.number(), z.number(), z.number()]),
  monograma: z.string(), acento: z.enum(['rojo', 'violeta', 'verde']),
});
export const actividadSchema = z.object({
  id: z.string(), cursoId: z.string(), corte: z.union([z.literal(1), z.literal(2), z.literal(3)]),
  titulo: z.string(), peso: z.number(), vence: z.string(),
});
export const calificacionSchema = z.object({
  actividadId: z.string(), estudianteId: z.string(), entregado: z.string().nullable(), archivo: z.string().nullable(),
  nota: z.number().nullable(), estado: z.enum(['borrador', 'publicada']).nullable(), retro: z.string(),
});
/** Calificación tal como la devuelve el servicio de evaluaciones (guía técnica, 8.7). */
export const calificacionApiSchema = z.object({
  id: z.string(), actividadId: z.string(), estudianteId: z.string(), entregaId: z.string().nullable(),
  valor: z.number(), retroalimentacion: z.string().nullable(), estado: z.enum(['BORRADOR', 'PUBLICADA']), version: z.string(),
});
export type CalificacionApi = z.infer<typeof calificacionApiSchema>;
export const usuarioSchema = z.object({ id: z.string(), nombre: z.string(), codigo: z.string(), rol: z.enum(['estudiante', 'docente']) });
export const sesionSchema = z.object({ token: z.string(), usuario: usuarioSchema });

/** Devuelve el primer mensaje de error por campo, útil para formularios web y móviles. */
export function erroresPorCampo(error: z.ZodError): Record<string, string> {
  const out: Record<string, string> = {};
  for (const i of error.issues) {
    const k = i.path.join('.') || '_';
    if (!out[k]) out[k] = i.message;
  }
  return out;
}

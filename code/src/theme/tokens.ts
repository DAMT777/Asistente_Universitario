/**
 * Tokens de diseño. Única fuente de colores, espaciados, tipografía y movimiento.
 * Solo valores planos (números y strings) para poder usarlos tal cual en
 * StyleSheet.create de React Native. Los valores de sombra web y blur están
 * aparte, en `web`, porque RN los expresa distinto.
 */
export const colors = {
  fondo: '#121014',
  fondoProfundo: '#0E0C0F',
  texto: '#F4F1EE',
  textoSuave: '#E2DDD9',
  textoMedio: '#CFC9C5',
  textoTenue: '#A9A3A0',
  textoApagado: '#7F7976',
  marca: '#C8102E',
  marcaHover: '#A80D26',
  marcaBrillante: '#E0293A',
  marcaTexto: '#FF8A94',
  superficie: 'rgba(26,24,28,0.62)',
  superficieFuerte: 'rgba(26,24,28,0.78)',
  fila: 'rgba(255,255,255,0.035)',
  filaHover: 'rgba(255,255,255,0.075)',
  campo: 'rgba(255,255,255,0.05)',
  linea: 'rgba(255,255,255,0.08)',
  lineaFuerte: 'rgba(255,255,255,0.14)',
  exito: '#5FDDB5',
  error: '#FF8A94',
  aviso: '#F5B544',
  textoSobreClaro: '#141016',
} as const;

export const acentos = {
  rojo: { rgb: '240,71,90', texto: '#FF9AA5' },
  violeta: { rgb: '155,107,255', texto: '#C7AEFF' },
  verde: { rgb: '47,197,155', texto: '#7FE3C4' },
} as const;

export const acento = (k: keyof typeof acentos, alpha = 1) => `rgba(${acentos[k].rgb},${alpha})`;
/** Opacidad de cada corte en las barras de pesos. */
export const alphaCortes = [1, 0.6, 0.32] as const;

export const estados = {
  pendiente: { fondo: 'rgba(245,166,35,0.16)', texto: '#F5B544' },
  vencida: { fondo: 'rgba(224,41,58,0.18)', texto: '#FF8A94' },
  entregada: { fondo: 'rgba(96,165,250,0.16)', texto: '#9CC8FF' },
  sin_calificar: { fondo: 'rgba(255,255,255,0.08)', texto: '#E2DDD9' },
  borrador: { fondo: 'rgba(251,146,60,0.16)', texto: '#FDAA6B' },
  publicada: { fondo: 'rgba(47,197,155,0.16)', texto: '#5FDDB5' },
  calificada: { fondo: 'rgba(47,197,155,0.16)', texto: '#5FDDB5' },
} as const;

export const space = { 0: 0, 1: 4, 2: 8, 3: 12, 4: 16, 5: 20, 6: 24, 7: 28, 8: 32, 10: 40, 12: 48, 14: 56 } as const;

export const radius = { sm: 8, md: 10, lg: 12, xl: 18, xxl: 20, pill: 999 } as const;

export const font = {
  familia: "'Public Sans', system-ui, sans-serif",
  familiaNativa: 'PublicSans',
  size: { xxs: 10, xs: 11, sm: 12, md: 13, base: 14, lg: 15, xl: 17, xxl: 19, h3: 22, h2: 24, h1: 34, nota: 36, hero: 46, heroMovil: 32 },
  weight: { light: '300', regular: '400', medium: '500', semibold: '600', bold: '700' },
  tracking: { normal: 0, ancho: 0.06, muyAncho: 0.1 },
  lineHeight: { ajustado: 1.05, normal: 1.4, lectura: 1.55 },
} as const;

export const size = { toque: 44, toqueGrande: 48, monograma: 44, monogramaGrande: 56, sidebar: 240, header: 64 } as const;

export const motion = { rapido: 160, base: 300, entrada: 500, escalonado: 60, easing: 'cubic-bezier(.2,.7,.2,1)' } as const;

export const breakpoints = { ancho: 860 } as const;

/** Solo web. */
export const web = {
  blur: 'blur(14px)',
  sombraMarca: '0 10px 24px -10px rgba(200,16,46,0.8)',
  sombraToast: '0 12px 32px rgba(0,0,0,0.4)',
} as const;

export const tokens = { colors, acentos, estados, space, radius, font, size, motion, breakpoints } as const;
export type Tokens = typeof tokens;

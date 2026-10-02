/**
 * Tokens de diseño. Única fuente de colores, espaciados, tipografía y movimiento.
 *
 * Los colores se definen dos veces con las mismas claves: `paleta` (tema claro) y `paletaOscura`
 * (modo nocturno). Son valores planos, así que React Native puede usarlos tal cual en StyleSheet.create.
 * En web, `colors` apunta a variables CSS con el valor claro como respaldo; `ui/tema.ts` declara esas
 * variables para cada tema y el cambio de tema no obliga a volver a renderizar.
 */
export const paleta = {
  /** Paleta institucional Unillanos (Manual MN-GCOM-001): rojo 485C · RGB 227,6,29, blanco y grises tenues. */
  fondo: '#FAFAF8',
  fondoProfundo: '#F2F1EE',
  texto: '#1C1A1B',
  textoSuave: '#2E2B2C',
  textoMedio: '#4A4547',
  textoTenue: '#625C5E',
  textoApagado: '#8A8486',
  marca: '#E3061D',
  marcaHover: '#B9051A',
  marcaBrillante: '#FF2A40',
  marcaTexto: '#C00518',
  marcaSuave: 'rgba(227,6,29,0.08)',
  /** Superficie sólida (tarjetas, botones claros). En modo nocturno deja de ser blanca. */
  blanco: '#FFFFFF',
  /** Texto e iconos sobre el rojo de marca. Siempre blanco. */
  sobreMarca: '#FFFFFF',
  superficie: 'rgba(255,255,255,0.92)',
  superficieFuerte: '#FFFFFF',
  barra: 'rgba(255,255,255,0.82)',
  fila: 'rgba(28,26,27,0.025)',
  filaHover: 'rgba(227,6,29,0.045)',
  campo: '#FFFFFF',
  linea: 'rgba(28,26,27,0.08)',
  lineaFuerte: 'rgba(28,26,27,0.18)',
  lineaMarca: 'rgba(227,6,29,0.32)',
  overlay: 'rgba(255,255,255,0.6)',
  exito: '#0B7A58',
  error: '#C00518',
  aviso: '#9A5B00',
  textoSobreClaro: '#141016',
} as const;

export type ClaveColor = keyof typeof paleta;

export const paletaOscura: Record<ClaveColor, string> = {
  fondo: '#121012',
  fondoProfundo: '#1B181A',
  texto: '#F4F1F2',
  textoSuave: '#E6E1E3',
  textoMedio: '#C9C2C5',
  textoTenue: '#A69EA1',
  textoApagado: '#857D80',
  marca: '#F0203A',
  marcaHover: '#FF4A5C',
  marcaBrillante: '#FF5A6C',
  marcaTexto: '#FF6B7C',
  marcaSuave: 'rgba(240,32,58,0.16)',
  blanco: '#221E20',
  sobreMarca: '#FFFFFF',
  superficie: 'rgba(34,30,32,0.92)',
  superficieFuerte: '#221E20',
  barra: 'rgba(24,21,23,0.86)',
  fila: 'rgba(255,255,255,0.035)',
  filaHover: 'rgba(240,32,58,0.10)',
  campo: '#1B181A',
  linea: 'rgba(255,255,255,0.09)',
  lineaFuerte: 'rgba(255,255,255,0.20)',
  lineaMarca: 'rgba(255,107,124,0.40)',
  overlay: 'rgba(10,8,9,0.6)',
  exito: '#3CCB9A',
  error: '#FF6B7C',
  aviso: '#F2B34C',
  textoSobreClaro: '#141016',
};

/**
 * Modo vidrio (solo web): las superficies se vuelven translúcidas para dejar ver la foto del campus.
 * Vidrio blanco sobre el tema claro y vidrio oscuro si además está activo el modo nocturno.
 */
export const vidrioClaro: Partial<Record<ClaveColor, string>> = {
  blanco: 'rgba(255,255,255,0.62)',
  superficie: 'rgba(255,255,255,0.58)',
  superficieFuerte: 'rgba(255,255,255,0.74)',
  barra: 'rgba(255,255,255,0.55)',
  fila: 'rgba(255,255,255,0.42)',
  fondoProfundo: 'rgba(255,255,255,0.45)',
  campo: 'rgba(255,255,255,0.82)',
  linea: 'rgba(255,255,255,0.65)',
};

export const vidrioOscuro: Partial<Record<ClaveColor, string>> = {
  blanco: 'rgba(28,24,26,0.62)',
  superficie: 'rgba(28,24,26,0.58)',
  superficieFuerte: 'rgba(28,24,26,0.76)',
  barra: 'rgba(20,17,19,0.6)',
  fila: 'rgba(255,255,255,0.05)',
  fondoProfundo: 'rgba(0,0,0,0.25)',
  campo: 'rgba(20,17,19,0.82)',
  linea: 'rgba(255,255,255,0.12)',
};

/** Nombre de la variable CSS de cada color: fondoProfundo → --au-c-fondoProfundo. */
export const variableColor = (k: string) => `--au-c-${k}`;

export const colors = Object.fromEntries(
  (Object.keys(paleta) as ClaveColor[]).map((k) => [k, `var(${variableColor(k)}, ${paleta[k]})`]),
) as Record<ClaveColor, string>;

export const acentos = {
  rojo: { rgb: '227,6,29', texto: 'var(--au-acento-rojo, #B0041A)' },
  violeta: { rgb: '112,72,214', texto: 'var(--au-acento-violeta, #5130AE)' },
  verde: { rgb: '14,150,110', texto: 'var(--au-acento-verde, #0A6B4E)' },
} as const;

/** Texto de cada acento en modo nocturno (más claro para mantener el contraste). */
export const acentosOscuros = { rojo: '#FF7A89', violeta: '#B9A4FF', verde: '#5FD9AE' } as const;

export const acento = (k: keyof typeof acentos, alpha = 1) => `rgba(${acentos[k].rgb},${alpha})`;
/** Opacidad de cada corte en las barras de pesos. */
export const alphaCortes = [1, 0.55, 0.28] as const;

export const estadosClaros = {
  pendiente: { fondo: 'rgba(245,158,11,0.14)', texto: '#8A5300' },
  vencida: { fondo: 'rgba(227,6,29,0.10)', texto: '#B0041A' },
  entregada: { fondo: 'rgba(37,99,235,0.10)', texto: '#1D4ED8' },
  sin_calificar: { fondo: 'rgba(28,26,27,0.06)', texto: '#4A4547' },
  borrador: { fondo: 'rgba(234,88,12,0.12)', texto: '#9A3D06' },
  publicada: { fondo: 'rgba(14,150,110,0.12)', texto: '#0A6B4E' },
  calificada: { fondo: 'rgba(14,150,110,0.12)', texto: '#0A6B4E' },
} as const;

export type ClaveEstado = keyof typeof estadosClaros;

export const estadosOscuros: Record<ClaveEstado, { fondo: string; texto: string }> = {
  pendiente: { fondo: 'rgba(245,158,11,0.18)', texto: '#F5C26B' },
  vencida: { fondo: 'rgba(240,32,58,0.18)', texto: '#FF8C99' },
  entregada: { fondo: 'rgba(96,140,255,0.18)', texto: '#9DB8FF' },
  sin_calificar: { fondo: 'rgba(255,255,255,0.08)', texto: '#C9C2C5' },
  borrador: { fondo: 'rgba(251,146,60,0.18)', texto: '#FDB07A' },
  publicada: { fondo: 'rgba(60,203,154,0.16)', texto: '#6FE0B8' },
  calificada: { fondo: 'rgba(60,203,154,0.16)', texto: '#6FE0B8' },
};

export const estados = Object.fromEntries(
  (Object.keys(estadosClaros) as ClaveEstado[]).map((k) => [k, {
    fondo: `var(--au-estado-${k}-fondo, ${estadosClaros[k].fondo})`,
    texto: `var(--au-estado-${k}-texto, ${estadosClaros[k].texto})`,
  }]),
) as Record<ClaveEstado, { fondo: string; texto: string }>;

export const space = { 0: 0, 1: 4, 2: 8, 3: 12, 4: 16, 5: 20, 6: 24, 7: 28, 8: 32, 10: 40, 12: 48, 14: 56 } as const;

export const radius = { sm: 8, md: 10, lg: 12, xl: 18, xxl: 20, pill: 999 } as const;

export const font = {
  familia: "'Public Sans', system-ui, sans-serif",
  familiaNativa: 'PublicSans',
  size: { xxs: 12, xs: 13, sm: 13, md: 13, base: 14, lg: 15, xl: 17, xxl: 19, h3: 22, h2: 24, h1: 34, nota: 36, hero: 34, heroMovil: 28 },
  weight: { light: '300', regular: '400', medium: '500', semibold: '600', bold: '700' },
  tracking: { normal: 0, ancho: 0.06, muyAncho: 0.1 },
  lineHeight: { ajustado: 1.05, normal: 1.4, lectura: 1.55 },
} as const;

export const size = { toque: 44, toqueGrande: 48, monograma: 44, monogramaGrande: 56, sidebar: 240, header: 64 } as const;

/** Una sola curva para todo el sistema: salida suave, sin rebote. */
export const motion = { rapido: 180, base: 320, entrada: 520, escalonado: 60, llenado: 1100, onda: 650, easing: 'cubic-bezier(.22,1,.36,1)' } as const;

export const breakpoints = { ancho: 860 } as const;

/** Solo web. Las sombras y el fondo también cambian con el tema (variables de global.css). */
export const web = {
  blur: 'blur(14px)',
  blurOverlay: 'blur(10px) saturate(1.1)',
  sombraMarca: 'var(--au-sombra-marca)',
  sombraSuave: 'var(--au-sombra-suave)',
  sombraHover: 'var(--au-sombra-hover)',
  sombraElevada: 'var(--au-sombra-elevada)',
  sombraToast: 'var(--au-sombra-toast)',
  fondoApp: 'var(--au-fondo-app)',
  fondoLogin: 'linear-gradient(180deg,rgba(255,255,255,0.55) 0%,rgba(250,250,248,0.78) 50%,rgba(242,241,238,0.94) 100%)',
} as const;

export const tokens = { colors, paleta, paletaOscura, acentos, estados, space, radius, font, size, motion, breakpoints } as const;
export type Tokens = typeof tokens;

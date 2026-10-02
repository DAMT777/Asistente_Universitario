import { describe, expect, it } from 'vitest';
import { MAX_ARCHIVOS_ENTREGA, TAMANO_MAX_ARCHIVO, TAMANO_MAX_ENTREGA, seleccionEntregaSchema } from './index';
import { formatoTamano } from '@/domain/archivos';

const archivo = (nombre: string, tamano = 1000) => ({ nombre, tamano });
const primerError = (xs: Array<{ nombre: string; tamano: number }>) => {
  const r = seleccionEntregaSchema.safeParse(xs);
  return r.success ? null : r.error.issues[0].message;
};

describe('seleccionEntregaSchema', () => {
  it('acepta varios archivos de tipos permitidos dentro de los límites', () => {
    expect(primerError([archivo('a.pdf'), archivo('b.docx'), archivo('c.PNG'), archivo('d.zip')])).toBeNull();
  });

  it('pide al menos un archivo', () => {
    expect(primerError([])).toBe('Elige al menos un archivo.');
  });

  it(`no acepta más de ${MAX_ARCHIVOS_ENTREGA} archivos`, () => {
    expect(primerError(Array.from({ length: MAX_ARCHIVOS_ENTREGA + 1 }, (_, i) => archivo(`a${i}.pdf`)))).toMatch(/hasta 10 archivos/);
  });

  it('rechaza un archivo que supera el máximo por archivo y dice cuál es', () => {
    expect(primerError([archivo('ok.pdf'), archivo('grande.pdf', TAMANO_MAX_ARCHIVO + 1)])).toBe('"grande.pdf" supera 20 MB.');
  });

  it('rechaza si la suma supera el máximo total aunque cada uno quepa', () => {
    const tres = Array.from({ length: 3 }, (_, i) => archivo(`p${i}.pdf`, TAMANO_MAX_ARCHIVO));
    expect(tres.reduce((s, a) => s + a.tamano, 0)).toBeGreaterThan(TAMANO_MAX_ENTREGA);
    expect(primerError(tres)).toBe('Los archivos suman más de 50 MB.');
  });

  it('rechaza extensiones no permitidas y archivos vacíos', () => {
    expect(primerError([archivo('virus.exe')])).toMatch(/"virus.exe": formato no admitido/);
    expect(primerError([archivo('vacio.pdf', 0)])).toBe('"vacio.pdf" está vacío.');
  });
});

describe('formatoTamano', () => {
  it.each([[500, '500 B'], [2048, '2 KB'], [1572864, '1.5 MB'], [20 * 1024 * 1024, '20 MB']])('%d bytes → %s', (b, txt) => {
    expect(formatoTamano(b)).toBe(txt);
  });
});

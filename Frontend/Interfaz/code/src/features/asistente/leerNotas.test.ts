import { Workbook } from 'exceljs';
import { describe, expect, it } from 'vitest';
import { leerNotas } from './leerNotas';

async function archivo(filas: unknown[][]) {
  const libro = new Workbook();
  const hoja = libro.addWorksheet('Notas');
  hoja.addRow(['Código', 'Nota']);
  hoja.addRows(filas);
  const contenido = await libro.xlsx.writeBuffer();
  return new File([contenido], 'notas.xlsx');
}

describe('leerNotas', () => {
  it('lee códigos con ceros y acepta notas con coma decimal', async () => {
    const datos = await archivo([['00123', 0], ['E0002', '4,2']]);
    await expect(leerNotas(datos)).resolves.toEqual([
      { codigo: '00123', nota: 0, numero: 2 },
      { codigo: 'E0002', nota: 4.2, numero: 3 },
    ]);
  });

  it('rechaza fórmulas y códigos duplicados', async () => {
    await expect(leerNotas(await archivo([['E0001', { formula: '2+2', result: 4 }]]))).rejects.toThrow('Fila 2');
    await expect(leerNotas(await archivo([['E0001', 3], ['e0001', 4]]))).rejects.toThrow('código repetido');
  });
});

export async function leerNotas(archivo: File) {
  if (!/\.xlsx$/i.test(archivo.name) || archivo.size > 256 * 1024)
    throw new Error('Selecciona un archivo .xlsx de hasta 256 KB.');

  const { Workbook } = await import('exceljs');
  const libro = new Workbook();
  try {
    await libro.xlsx.load(await archivo.arrayBuffer());
  } catch {
    throw new Error('El archivo no es un Excel .xlsx válido.');
  }

  const hoja = libro.worksheets[0];
  const encabezado = (col: number) => hoja?.getRow(1).getCell(col).text.trim().toLowerCase().normalize('NFD').replace(/\p{Diacritic}/gu, '');
  if (encabezado(1) !== 'codigo' || encabezado(2) !== 'nota')
    throw new Error('La primera fila debe contener codigo y nota, en ese orden.');

  const resultados: { codigo: string; nota: number; numero: number }[] = [];
  const vistos = new Set<string>();
  hoja.eachRow((fila, numero) => {
    if (numero === 1) return;
    const codigo = fila.getCell(1).value;
    const nota = fila.getCell(2).value;
    if (codigo === null && nota === null) return;
    const codigoTexto = typeof codigo === 'string' || typeof codigo === 'number' ? String(codigo).trim().toUpperCase() : '';
    const notaTexto = typeof nota === 'string' || typeof nota === 'number' ? String(nota).trim().replace(',', '.') : '';
    if (!/^[A-Z0-9-]{1,40}$/.test(codigoTexto) || !/^[0-5](?:\.[0-9])?$/.test(notaTexto) || Number(notaTexto) > 5)
      throw new Error(`Fila ${numero}: usa un código válido y una nota entre 0 y 5 con máximo un decimal.`);
    if (vistos.has(codigoTexto)) throw new Error(`Fila ${numero}: código repetido.`);
    if (resultados.length === 250) throw new Error('El archivo supera las 250 filas.');
    vistos.add(codigoTexto);
    resultados.push({ codigo: codigoTexto, nota: Number(notaTexto), numero });
  });
  if (!resultados.length) throw new Error('La hoja no contiene notas.');
  return resultados;
}

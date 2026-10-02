import { useState } from 'react';
import type { Calificacion } from '@/types';
import { useApi } from '@/hooks';
import { Boton } from '@/ui';
import { colors } from '@/theme/tokens';

export function ArchivoLink({ calificacion: c }: { calificacion: Calificacion }) {
  const { api } = useApi();
  const [error, setError] = useState<string | null>(null);
  const [cargando, setCargando] = useState(false);
  if (!c.archivo) return null;
  if (c.urlArchivo) return <a href={c.urlArchivo} download={c.archivo}>Descargar {c.archivo}</a>;
  if (!c.entregaId) return <small>{c.archivo} · archivo de ejemplo sin documento descargable</small>;
  return <><Boton variante="texto" disabled={cargando} onClick={async () => {
    setCargando(true); setError(null);
    try {
      const url = await api.calificaciones.descargarArchivo(c.entregaId!);
      const enlace = document.createElement('a'); enlace.href = url; enlace.download = c.archivo!;
      enlace.click(); setTimeout(() => URL.revokeObjectURL(url), 30_000);
    } catch(e) { setError(e instanceof Error ? e.message : 'No se pudo descargar el archivo.'); }
    finally { setCargando(false); }
  }}>{cargando ? 'Descargando…' : `Descargar ${c.archivo}`}</Boton>{error && <span role="alert" style={{ color: colors.error }}>{error}</span>}</>;
}

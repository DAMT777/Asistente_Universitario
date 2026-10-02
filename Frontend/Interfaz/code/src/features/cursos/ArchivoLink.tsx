import { useState } from 'react';
import type { ArchivoEntregado, Calificacion } from '@/types';
import { useApi } from '@/hooks';
import { Boton } from '@/ui';
import { colors } from '@/theme/tokens';

const tamanoLegible = (bytes: number) => (bytes >= 1024 * 1024 ? `${(bytes / 1024 / 1024).toFixed(2)} MB` : `${Math.max(1, Math.round(bytes / 1024))} KB`);

/** Archivos de la entrega: cada uno se descarga con su nombre original, sea del tipo que sea. */
export function ArchivoLink({ calificacion: c }: { calificacion: Calificacion }) {
  if (!c.archivo) return null;
  if (c.urlArchivo) return <a href={c.urlArchivo} download={c.archivo}>Descargar {c.archivo}</a>;
  if (!c.entregaId || !c.archivos?.length) return <small>{c.archivo} · archivo de ejemplo sin documento descargable</small>;
  return <ul style={{ listStyle: 'none', margin: 0, padding: 0, display: 'flex', flexDirection: 'column', gap: 4 }}>
    {c.archivos.map(a => <li key={a.id}><Descarga entregaId={c.entregaId!} archivo={a} /></li>)}
  </ul>;
}

function Descarga({ entregaId, archivo }: { entregaId: string; archivo: ArchivoEntregado }) {
  const { api } = useApi();
  const [error, setError] = useState<string | null>(null);
  const [cargando, setCargando] = useState(false);
  return <><Boton variante="texto" disabled={cargando} aria-label={`Descargar ${archivo.nombre}`} onClick={async () => {
    setCargando(true); setError(null);
    try {
      const url = await api.calificaciones.descargarArchivo(entregaId, archivo.id);
      const enlace = document.createElement('a'); enlace.href = url; enlace.download = archivo.nombre;
      enlace.click(); setTimeout(() => URL.revokeObjectURL(url), 30_000);
    } catch(e) { setError(e instanceof Error ? e.message : 'No se pudo descargar el archivo.'); }
    finally { setCargando(false); }
  }}>{cargando ? 'Descargando…' : `Descargar ${archivo.nombre}`}</Boton> <small style={{ color: colors.textoTenue }}>{tamanoLegible(archivo.tamano)}</small>{error && <span role="alert" style={{ color: colors.error }}> {error}</span>}</>;
}

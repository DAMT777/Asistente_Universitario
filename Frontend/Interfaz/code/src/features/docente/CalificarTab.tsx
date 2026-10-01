import { useState } from 'react';
import { useApi, useCalificaciones, type FilaCalificacion } from '@/hooks';
import { fechaCorta, formatoNota, iniciales } from '@/domain';
import { colors } from '@/theme/tokens';
import { AreaTexto, Boton, Cargando, Chip, ErrorEstado, Panel } from '@/ui';
import { useToast } from '@/ui/Toast';

export function CalificarTab({ cursoId, actividadId, onElegir }: { cursoId: string; actividadId?: string; onElegir: (id: string) => void }) {
  const cal = useCalificaciones(cursoId, actividadId);
  const toast = useToast();
  const { hoy: hoyFn } = useApi();
  if (cal.isLoading) return <Cargando filas={4} />;
  if (cal.error) return <ErrorEstado error={cal.error} />;
  const a = cal.actividad;
  if (!a) return null;
  const n = cal.conteo!;
  const hoy = hoyFn();

  return (
    <>
      <div style={{ display: 'flex', gap: 8, overflowX: 'auto', paddingBottom: 4, marginTop: -8 }}>
        {cal.actividades!.map((x) => {
          const on = x.id === a.id;
          return <button key={x.id} onClick={() => onElegir(x.id)} style={{ flex: 'none', height: 38, padding: '0 16px', borderRadius: 999, fontSize: 13, fontWeight: 600, whiteSpace: 'nowrap', background: on ? colors.marca : colors.campo, color: on ? '#FFFFFF' : colors.textoMedio, border: `1px solid ${on ? colors.marca : 'rgba(255,255,255,0.12)'}` }}>{x.titulo}</button>;
        })}
      </div>
      <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', gap: 12 }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
          <span style={{ fontSize: 19, fontWeight: 600 }}>{a.titulo}</span>
          <span style={{ fontSize: 13, color: colors.textoTenue }}>Corte {a.corte} · {n.entregas}/{cal.totalEstudiantes} entregas · {n.publicadas} publicadas · {n.borradores} en borrador</span>
        </div>
        {n.borradores > 0 && <Boton disabled={cal.guardando} onClick={async () => { const k = await cal.publicarBorradores(); toast(`${k} notas publicadas`); }}>Publicar borradores ({n.borradores})</Boton>}
      </div>
      <p style={{ margin: '-12px 0 0', fontSize: 13, color: colors.textoTenue }}>Los estudiantes solo ven las notas publicadas. Si editas una nota publicada, vuelve a borrador.</p>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill,minmax(320px,1fr))', gap: 14 }}>
        {cal.filas!.map((f, i) => (
          <TarjetaCalificacion key={f.estudiante.id + a.id} fila={f} indice={i} vencida={a.vence < hoy} vence={a.vence}
            onGuardar={async (nota, retro, publicar) => {
              const err = await cal.guardar({ actividadId: a.id, estudianteId: f.estudiante.id, nota, retro, publicar });
              if (!err) toast(publicar ? 'Nota publicada' : 'Guardada como borrador');
              return err?.nota ?? err?.retro ?? null;
            }} />
        ))}
      </div>
    </>
  );
}

function TarjetaCalificacion({ fila, indice, vencida, vence, onGuardar }: { fila: FilaCalificacion; indice: number; vencida: boolean; vence: string; onGuardar: (nota: string, retro: string, publicar: boolean) => Promise<string | null> }) {
  const c = fila.calificacion;
  const [nota, setNota] = useState(c?.nota != null ? formatoNota(c.nota) : '');
  const [retro, setRetro] = useState(c?.retro ?? '');
  const [error, setError] = useState<string | null>(null);
  const guardar = async (publicar: boolean) => setError(await onGuardar(nota, retro, publicar));
  const entrega = c?.entregado ? `${c.archivo} · ${fechaCorta(c.entregado)}` : vencida ? 'Sin entrega' : `Sin entrega · vence ${fechaCorta(vence)}`;

  return (
    <Panel padding={16} indice={indice} style={{ gap: 14 }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
        <div style={{ width: 40, height: 40, flex: 'none', borderRadius: '50%', background: 'rgba(255,255,255,0.08)', border: '1px solid rgba(255,255,255,0.12)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 13, fontWeight: 700 }}>{iniciales(fila.estudiante.nombre)}</div>
        <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: 2, minWidth: 0 }}>
          <span style={{ fontSize: 15, fontWeight: 600 }}>{fila.estudiante.nombre}</span>
          <span style={{ fontSize: 12, color: colors.textoTenue, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{entrega}</span>
        </div>
        <Chip estado={fila.estado} />
      </div>
      <div style={{ display: 'flex', gap: 10, alignItems: 'flex-start' }}>
        <label style={{ display: 'flex', flexDirection: 'column', gap: 6, fontSize: 11, fontWeight: 600, letterSpacing: '0.06em', color: colors.textoTenue, width: 84, flex: 'none' }}>NOTA
          <input value={nota} onChange={(e) => { setNota(e.target.value); setError(null); }} inputMode="decimal" placeholder="0.0" aria-invalid={!!error}
            style={{ height: 48, width: 84, padding: '0 10px', border: `1px solid ${error ? '#FF6B78' : colors.lineaFuerte}`, borderRadius: 10, background: colors.campo, fontSize: 19, fontWeight: 600, textAlign: 'center' }} />
        </label>
        <AreaTexto etiqueta="RETROALIMENTACIÓN" rows={2} value={retro} onChange={(e) => setRetro(e.target.value)} placeholder="Comentario para el estudiante" />
      </div>
      {error && <span role="alert" style={{ fontSize: 13, color: colors.error, fontWeight: 500, marginTop: -6 }}>{error}</span>}
      <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
        <Boton variante="contorno" style={{ height: 40, fontSize: 13 }} onClick={() => guardar(false)}>Guardar borrador</Boton>
        <Boton variante="claro" style={{ height: 40, fontSize: 13 }} onClick={() => guardar(true)}>Publicar</Boton>
      </div>
    </Panel>
  );
}

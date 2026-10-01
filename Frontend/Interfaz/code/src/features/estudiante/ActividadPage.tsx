import { useState, type ChangeEvent } from 'react';
import { useParams } from 'react-router-dom';
import { useEntrega } from '@/hooks';
import { fechaCorta, formatoNota } from '@/domain';
import { acento, colors } from '@/theme/tokens';
import { Cargando, Chip, ErrorEstado, FechaTile, Icono, icono, Panel, entrada } from '@/ui';
import { useToast } from '@/ui/Toast';
import { Volver } from '../cursos/compartido';

export function ActividadPage() {
  const { cursoId, actividadId } = useParams();
  const e = useEntrega(actividadId);
  const toast = useToast();
  const [error, setError] = useState<string | null>(null);

  if (e.isLoading) return <Cargando />;
  if (!e.actividad || !e.curso || !e.estado) return <ErrorEstado error={e.error ?? new Error('Actividad no encontrada.')} />;
  const { actividad: a, calificacion: c, curso, estado } = e;

  async function alElegir(ev: ChangeEvent<HTMLInputElement>) {
    const f = ev.target.files?.[0];
    ev.target.value = '';
    if (!f) return;
    const err = await e.entregar({ nombre: f.name, tamano: f.size, datos: f });
    setError(err);
    if (!err) toast('Entrega enviada');
  }

  return (
    <>
      <Volver a={`/e/cursos/${cursoId}`} etiqueta={curso.nombre} />
      <div style={{ position: 'relative', overflow: 'hidden', background: colors.superficie, border: '1px solid rgba(255,255,255,0.09)', borderRadius: 20, backdropFilter: 'blur(14px)', padding: 22, display: 'flex', alignItems: 'flex-start', gap: 16, ...entrada(0) }}>
        <div style={{ position: 'absolute', inset: 0, background: `radial-gradient(circle at 0% 0%, ${acento(curso.acento, 0.32)} 0%, transparent 55%)`, pointerEvents: 'none' }} />
        <div style={{ position: 'relative' }}><FechaTile fecha={a.vence} grande /></div>
        <div style={{ position: 'relative', display: 'flex', flexDirection: 'column', gap: 8, minWidth: 0 }}>
          <span style={{ fontSize: 13, color: colors.textoTenue }}>{curso.nombre} · Corte {a.corte} · {a.peso}% del corte</span>
          <h1 style={{ margin: 0, fontSize: 24, fontWeight: 600 }}>{a.titulo}</h1>
          <span style={{ alignSelf: 'flex-start' }}><Chip estado={estado} /></span>
        </div>
      </div>

      {estado === 'calificada' && c && (
        <Panel indice={1} style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 20 }}>
          <div style={{ display: 'flex', alignItems: 'baseline', gap: 8 }}>
            <span style={{ fontSize: 48, fontWeight: 600, lineHeight: 1 }}>{formatoNota(c.nota)}</span>
            <span style={{ fontSize: 14, color: colors.textoTenue }}>de 5.0</span>
          </div>
          <div style={{ flex: 1, minWidth: 220, display: 'flex', flexDirection: 'column', gap: 6 }}>
            <span style={{ fontSize: 11, fontWeight: 600, letterSpacing: '0.06em', color: colors.textoTenue }}>RETROALIMENTACIÓN</span>
            <p style={{ margin: 0, fontSize: 15, lineHeight: 1.55, color: colors.textoSuave }}>{c.retro || 'Sin comentarios del docente.'}</p>
          </div>
        </Panel>
      )}

      {c?.entregado && (
        <Panel padding={16} style={{ flexDirection: 'row', alignItems: 'center', gap: 14 }}>
          <div style={{ width: 44, height: 44, flex: 'none', borderRadius: 11, background: 'rgba(47,197,155,0.16)', color: colors.exito, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 11, fontWeight: 700 }}>{c.archivo?.split('.').pop()?.toUpperCase()}</div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 2, minWidth: 0 }}>
            <span style={{ fontSize: 15, fontWeight: 600, wordBreak: 'break-all' }}>{c.archivo}</span>
            <span style={{ fontSize: 13, color: colors.textoTenue }}>Enviada el {fechaCorta(c.entregado)}</span>
          </div>
        </Panel>
      )}

      {e.abierta && (
        <>
          <label style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 8, minHeight: 170, border: '1.5px dashed rgba(255,255,255,0.22)', borderRadius: 18, background: 'rgba(26,24,28,0.5)', cursor: 'pointer', padding: 24, textAlign: 'center', opacity: e.enviando ? 0.6 : 1 }}>
            <div style={{ width: 48, height: 48, borderRadius: '50%', background: 'rgba(200,16,46,0.18)', color: colors.marcaTexto, display: 'flex', alignItems: 'center', justifyContent: 'center' }}><Icono d={icono.subir} tam={22} /></div>
            <span style={{ fontSize: 16, fontWeight: 600 }}>{e.enviando ? 'Enviando…' : c?.entregado ? 'Reemplazar archivo' : 'Selecciona tu archivo'}</span>
            <span style={{ fontSize: 13, color: colors.textoTenue }}>PDF, DOCX o ZIP · máximo 10 MB</span>
            <input type="file" accept=".pdf,.docx,.zip" onChange={alElegir} disabled={e.enviando} style={{ display: 'none' }} />
          </label>
          {error && <span role="alert" style={{ fontSize: 13, color: colors.error, fontWeight: 500, marginTop: -12 }}>{error}</span>}
        </>
      )}

      {estado === 'vencida' && (
        <div style={{ background: 'rgba(224,41,58,0.14)', border: '1px solid rgba(224,41,58,0.3)', color: '#FFC2C7', borderRadius: 14, padding: '14px 16px', fontSize: 14 }}>
          El plazo cerró el {fechaCorta(a.vence)}. Ya no se reciben entregas.
        </div>
      )}
    </>
  );
}

import { useState, type FormEvent } from 'react';
import type { Corte, Curso } from '@/types';
import { useCalificaciones, useCrearActividad } from '@/hooks';
import { validarPesos } from '@/domain';
import { acento, colors } from '@/theme/tokens';
import { Barra, Boton, Campo, FechaTile, Fila, Panel, entrada } from '@/ui';
import { useToast } from '@/ui/Toast';

const CORTES: Corte[] = [1, 2, 3];
const VACIO = { titulo: '', corte: '2', peso: '', vence: '' };

export function ActividadesTab({ curso, onCalificar }: { curso: Curso; onCalificar: (actividadId: string) => void }) {
  const cal = useCalificaciones(curso.id, undefined);
  const { crear, creando } = useCrearActividad(curso.id);
  const toast = useToast();
  const [abierto, setAbierto] = useState(false);
  const [form, setForm] = useState(VACIO);
  const [errores, setErrores] = useState<Record<string, string>>({});
  const set = (k: keyof typeof VACIO) => (v: string) => setForm((f) => ({ ...f, [k]: v }));

  async function enviar(e: FormEvent) {
    e.preventDefault();
    const r = await crear(form);
    if (!r.ok) return setErrores(r.errores);
    setErrores({}); setForm(VACIO); setAbierto(false);
    toast(r.total === 100 ? 'Actividad creada' : `Actividad creada. El corte ${r.corte} suma ${r.total}%.`);
  }

  const N = cal.totalEstudiantes;
  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: -8 }}>
        <Boton onClick={() => setAbierto((a) => !a)}>{abierto ? 'Cancelar' : 'Nueva actividad'}</Boton>
      </div>
      {abierto && (
        <form onSubmit={enviar} noValidate style={{ background: colors.superficieFuerte, border: `1px solid ${colors.lineaFuerte}`, borderRadius: 18, backdropFilter: 'blur(14px)', padding: 18, display: 'grid', gridTemplateColumns: 'repeat(auto-fit,minmax(180px,1fr))', gap: 14, alignItems: 'start', ...entrada(0) }}>
          <Campo etiqueta="TÍTULO" value={form.titulo} onChange={(e) => set('titulo')(e.target.value)} error={errores.titulo} placeholder="Taller 3 · Simulación de inventarios" style={{ gridColumn: '1/-1' }} />
          <label style={{ display: 'flex', flexDirection: 'column', gap: 6, fontSize: 12, fontWeight: 600, letterSpacing: '0.04em', color: colors.textoMedio }}>CORTE
            <select value={form.corte} onChange={(e) => set('corte')(e.target.value)} style={{ height: 44, padding: '0 10px', border: `1px solid ${colors.lineaFuerte}`, borderRadius: 10, background: '#221F24', fontSize: 15, colorScheme: 'dark' }}>
              {CORTES.map((k) => <option key={k} value={k}>Corte {k}</option>)}
            </select>
          </label>
          <Campo etiqueta="PESO EN EL CORTE (%)" inputMode="numeric" value={form.peso} onChange={(e) => set('peso')(e.target.value)} error={errores.peso} placeholder="25" />
          <Campo etiqueta="FECHA LÍMITE" type="date" value={form.vence} onChange={(e) => set('vence')(e.target.value)} error={errores.vence} />
          <div style={{ gridColumn: '1/-1', display: 'flex', justifyContent: 'flex-end' }}>
            <Boton type="submit" variante="claro" disabled={creando}>{creando ? 'Creando…' : 'Crear actividad'}</Boton>
          </div>
        </form>
      )}

      {CORTES.map((k, i) => {
        const lista = (cal.actividades ?? []).filter((a) => a.corte === k);
        const suma = validarPesos(lista.map((a) => a.peso));
        return (
          <Panel key={k} indice={i}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
              <h2 style={{ margin: 0, fontSize: 17, fontWeight: 600 }}>Corte {k}</h2>
              <span style={{ fontSize: 13, color: colors.textoTenue }}>{curso.pesos[k - 1]}% de la nota</span>
            </div>
            {lista.length > 0 && !suma.ok && (
              <div style={{ fontSize: 13, color: colors.aviso, background: 'rgba(245,166,35,0.12)', borderRadius: 10, padding: '10px 12px' }}>Los pesos de este corte suman {suma.total}%. Ajústalos en Ponderado.</div>
            )}
            {lista.map((a) => {
              const c = cal.conteos.get(a.id);
              return (
                <Fila key={a.id} onClick={() => onCalificar(a.id)} style={{ flexWrap: 'wrap' }}>
                  <FechaTile fecha={a.vence} />
                  <div style={{ flex: 1, minWidth: 160, display: 'flex', flexDirection: 'column', gap: 3 }}>
                    <span style={{ fontSize: 15, fontWeight: 600 }}>{a.titulo}</span>
                    <span style={{ fontSize: 13, color: colors.textoTenue }}>{a.peso}% del corte</span>
                  </div>
                  {c && (
                    <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                      <div style={{ width: 90 }}><Barra valor={N ? (c.publicadas / N) * 100 : 0} color={acento(curso.acento)} alto={5} /></div>
                      <span style={{ fontSize: 13, color: colors.textoMedio, whiteSpace: 'nowrap' }}>{c.entregas}/{N} entregas · {c.publicadas} publicadas</span>
                    </div>
                  )}
                </Fila>
              );
            })}
          </Panel>
        );
      })}
    </>
  );
}

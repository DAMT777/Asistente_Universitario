import { usePonderado } from '@/hooks';
import { formatoNota, iniciales } from '@/domain';
import { acento, alphaCortes, colors, motion } from '@/theme/tokens';
import { Barra, Boton, CampoPorcentaje, Cargando, ErrorEstado, Panel } from '@/ui';
import { useToast } from '@/ui/Toast';

export function PonderadoTab({ cursoId }: { cursoId: string }) {
  const p = usePonderado(cursoId);
  const toast = useToast();
  if (p.error) return <ErrorEstado error={p.error} onReintentar={p.refetch} />;
  if (!p.curso || !p.borrador || !p.validacion) return <Cargando />;
  const { curso, borrador, validacion: v } = p;
  const okColor = (ok: boolean) => (ok ? colors.exito : colors.error);
  const escala = Math.max(v.cortes.total, 100);

  return (
    <>
      <Panel indice={0} style={{ gap: 16 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
          <h2 style={{ margin: 0, fontSize: 17, fontWeight: 600 }}>Peso de los cortes</h2>
          <span style={{ fontSize: 13, fontWeight: 600, color: okColor(v.cortes.ok) }}>Total {v.cortes.total}%</span>
        </div>
        <div style={{ display: 'flex', height: 12, borderRadius: 6, overflow: 'hidden', background: colors.linea, gap: 3 }} aria-hidden="true">
          {borrador.cortes.map((x, i) => <div key={i} style={{ height: '100%', width: `${((Number(x) || 0) / escala) * 100}%`, background: acento(curso.acento, alphaCortes[i]), transition: `width ${motion.base}ms ${motion.easing}` }} />)}
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,minmax(0,1fr))', gap: 10 }}>
          {borrador.cortes.map((x, i) => <CampoPorcentaje key={i} etiqueta={`CORTE ${i + 1}`} valor={x} onCambio={(val) => p.setPesoCorte(i as 0 | 1 | 2, val)} />)}
        </div>
      </Panel>

      {v.grupos.map((g) => (
        <Panel key={g.corte}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
            <h2 style={{ margin: 0, fontSize: 16, fontWeight: 600 }}>Actividades del corte {g.corte}</h2>
            <span style={{ fontSize: 13, fontWeight: 600, color: okColor(g.ok) }}>Total {g.total}%</span>
          </div>
          {g.actividades.map((a) => (
            <div key={a.id} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 12 }}>
              <span style={{ fontSize: 14, color: colors.textoSuave, minWidth: 0 }}>{a.titulo}</span>
              <CampoPorcentaje valor={borrador.actividades[a.id] ?? ''} onCambio={(val) => p.setPesoActividad(a.id, val)} ancho={96} etiqueta={`Peso de ${a.titulo}`} />
            </div>
          ))}
        </Panel>
      ))}

      <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'flex-end', gap: 12 }}>
        {!v.ok && <span style={{ fontSize: 13, color: colors.error, fontWeight: 500 }}>Los cortes deben sumar 100%. Las actividades de cada corte pueden sumar hasta 100%; cada peso debe ser mayor que 0.</span>}
        <Boton disabled={!v.ok || p.guardando} onClick={async () => { if (await p.guardar()) toast('Pesos actualizados'); }}>{p.guardando ? 'Guardando…' : 'Guardar pesos'}</Boton>
      </div>

      <Panel>
        <h2 style={{ margin: 0, fontSize: 17, fontWeight: 600 }}>Avance calculado por estudiante</h2>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill,minmax(280px,1fr))', gap: 10 }}>
          {(p.resumenEstudiantes ?? []).map(({ estudiante, resumen }) => (
            <div key={estudiante.id} style={{ background: colors.fila, border: `1px solid ${colors.linea}`, borderRadius: 12, padding: '12px 14px', display: 'flex', alignItems: 'center', gap: 12 }}>
              <div style={{ width: 36, height: 36, flex: 'none', borderRadius: '50%', background: colors.fondoProfundo, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 12, fontWeight: 700 }}>{iniciales(estudiante.nombre)}</div>
              <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: 6, minWidth: 0 }}>
                <span style={{ fontSize: 14, fontWeight: 600, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{estudiante.nombre}</span>
                <Barra valor={(resumen.acumulado / 5) * 100} color={acento(curso.acento)} alto={4} />
              </div>
              <span style={{ fontSize: 20, fontWeight: 600 }}>{formatoNota(resumen.acumulado)}</span>
            </div>
          ))}
        </div>
      </Panel>
    </>
  );
}

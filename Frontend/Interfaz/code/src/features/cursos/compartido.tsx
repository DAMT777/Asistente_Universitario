import { useNavigate, useOutletContext } from 'react-router-dom';
import type { Curso } from '@/types';
import { colors, font, web } from '@/theme/tokens';
import { BarraCortes, Icono, icono, Monograma, conOnda, entrada } from '@/ui';
import { acento } from '@/theme/tokens';

export const useLayout = () => useOutletContext<{ wide: boolean }>();

export function Volver({ a, etiqueta }: { a: string; etiqueta: string }) {
  const nav = useNavigate();
  return (
    <button onClick={() => nav(a)} onPointerDown={conOnda()} className="au-onda-host au-boton--contorno" style={{ alignSelf: 'flex-start', display: 'flex', alignItems: 'center', gap: 6, height: 44, padding: '0 12px 0 8px', border: `1px solid ${colors.lineaFuerte}`, borderRadius: 999, backgroundColor: colors.blanco, color: colors.textoMedio, fontWeight: 600, fontSize: 13, marginBottom: -8 }}>
      <Icono d={icono.atras} tam={16} />{etiqueta}
    </button>
  );
}

/** Encabezado del curso con resplandor del color del curso y barra de pesos por corte. */
export function BannerCurso({ curso, cifra, cifraEtiqueta, notasCortes }: { curso: Curso; cifra: string; cifraEtiqueta: string; notasCortes?: string[] }) {
  return (
    <div style={{ position: 'relative', overflow: 'hidden', background: colors.superficie, border: `1px solid ${colors.linea}`, borderRadius: 20, backdropFilter: 'blur(14px)', boxShadow: web.sombraSuave, padding: 18, display: 'flex', flexDirection: 'column', gap: 14, ...entrada(0) }}>
      <div style={{ position: 'absolute', inset: 0, background: `radial-gradient(circle at 0% 0%, ${acento(curso.acento, 0.12)} 0%, transparent 60%)`, pointerEvents: 'none' }} />
      <div style={{ position: 'relative', display: 'flex', flexWrap: 'wrap', justifyContent: 'space-between', alignItems: 'flex-start', gap: 16 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 16, minWidth: 0 }}>
          <Monograma texto={curso.monograma} acento={curso.acento} grande />
          <div style={{ display: 'flex', flexDirection: 'column', gap: 4, minWidth: 0 }}>
            <span style={{ fontSize: 13, color: colors.textoTenue }}>{codigoCurso(curso)} · {curso.docente}</span>
            <h1 style={{ margin: 0, fontSize: font.size.h2, fontWeight: 600 }}>{curso.nombre}</h1>
          </div>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end', gap: 2 }}>
          <span style={{ fontSize: 34, fontWeight: 600, lineHeight: 1 }}>{cifra}</span>
          <span style={{ fontSize: 12, color: colors.textoTenue }}>{cifraEtiqueta}</span>
        </div>
      </div>
      <div style={{ position: 'relative' }}>
        <BarraCortes pesos={curso.pesos} acento={curso.acento} etiquetas={curso.pesos.map((p, i) => ({ izquierda: `Corte ${i + 1} · ${p}%`, derecha: notasCortes?.[i] }))} />
      </div>
    </div>
  );
}

export const codigoCurso = (c: Curso) => `${c.codigo}-${c.grupo} · ${c.periodo}`;

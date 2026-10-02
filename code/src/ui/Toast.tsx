import { createContext, useCallback, useContext, useRef, useState, type ReactNode } from 'react';
import { colors, font, motion, web } from '@/theme/tokens';

const Ctx = createContext<(m: string) => void>(() => undefined);

export function ToastProvider({ children }: { children: ReactNode }) {
  const [msg, setMsg] = useState<{ id: number; texto: string } | null>(null);
  const t = useRef<ReturnType<typeof setTimeout>>();
  const mostrar = useCallback((texto: string) => {
    setMsg({ id: Date.now(), texto });
    clearTimeout(t.current);
    t.current = setTimeout(() => setMsg(null), 2600);
  }, []);
  return (
    <Ctx.Provider value={mostrar}>
      {children}
      {msg && (
        <div key={msg.id} role="status" style={{ position: 'fixed', left: '50%', bottom: 88, transform: 'translateX(-50%)', zIndex: 50, display: 'flex', alignItems: 'center', gap: 10, background: 'rgba(244,241,238,0.96)', color: colors.textoSobreClaro, fontSize: font.size.base, fontWeight: font.weight.semibold, padding: '12px 18px', borderRadius: 12, boxShadow: web.sombraToast, whiteSpace: 'nowrap', animation: `au-toast 260ms ${motion.easing} both` }}>
          <span style={{ width: 8, height: 8, borderRadius: '50%', background: '#2FC59B' }} />
          {msg.texto}
        </div>
      )}
    </Ctx.Provider>
  );
}

export const useToast = () => useContext(Ctx);

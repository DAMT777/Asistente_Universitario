import { createContext, useContext, useMemo, type ReactNode } from 'react';
import type { ApiClient } from '@/api';
import { NOTA_APROBATORIA, hoyISO } from '@/domain';

interface Entorno {
  api: ApiClient;
  /** Inyectable para demos y tests (el mock usa el 1 de octubre de 2026). */
  hoy: () => string;
  aprobatoria: number;
}

const Ctx = createContext<Entorno | null>(null);

export function ApiProvider({ api, hoy = () => hoyISO(), aprobatoria = NOTA_APROBATORIA, children }: Partial<Omit<Entorno, 'api'>> & { api: ApiClient; children: ReactNode }) {
  const value = useMemo(() => ({ api, hoy, aprobatoria }), [api, hoy, aprobatoria]);
  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}

export function useApi(): Entorno {
  const v = useContext(Ctx);
  if (!v) throw new Error('useApi debe usarse dentro de <ApiProvider>.');
  return v;
}

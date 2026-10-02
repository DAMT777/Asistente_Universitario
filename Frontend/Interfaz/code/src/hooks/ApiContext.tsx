import { createContext, useContext, useMemo, type ReactNode } from 'react';
import type { ApiClient } from '@/api';
import type { Rol } from '@/types';
import { NOTA_APROBATORIA, hoyISO } from '@/domain';

interface Entorno {
  api: ApiClient;
  /** Inyectable para demos y tests (el mock usa el 1 de octubre de 2026). */
  hoy: () => string;
  aprobatoria: number;
  /** Usuario que el login sugiere para cada rol (cambia entre la api simulada y el backend). */
  usuariosDemo: Partial<Record<Rol, string>>;
}

const Ctx = createContext<Entorno | null>(null);
const SIN_DEMO: Entorno['usuariosDemo'] = {};
const hoyISOActual = () => hoyISO();

export function ApiProvider({ api, hoy = hoyISOActual, aprobatoria = NOTA_APROBATORIA, usuariosDemo = SIN_DEMO, children }: Partial<Omit<Entorno, 'api'>> & { api: ApiClient; children: ReactNode }) {
  const value = useMemo(() => ({ api, hoy, aprobatoria, usuariosDemo }), [api, hoy, aprobatoria, usuariosDemo]);
  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}

export function useApi(): Entorno {
  const v = useContext(Ctx);
  if (!v) throw new Error('useApi debe usarse dentro de <ApiProvider>.');
  return v;
}

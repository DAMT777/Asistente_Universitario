import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import type { Sesion } from '@/types';
import { erroresPorCampo, loginSchema, sesionSchema, type LoginInput } from '@/schemas';
import { useApi } from './ApiContext';

/** Web: localStorage. React Native: AsyncStorage o SecureStore. */
export interface AlmacenSesion {
  leer(): Promise<string | null>;
  guardar(valor: string): Promise<void>;
  borrar(): Promise<void>;
}

interface EstadoSesion {
  sesion: Sesion | null;
  cargando: boolean;
  login(input: LoginInput): Promise<{ ok: true } | { ok: false; errores: Record<string, string> }>;
  logout(): Promise<void>;
}

const Ctx = createContext<EstadoSesion | null>(null);

export function SesionProvider({ almacen, children }: { almacen: AlmacenSesion; children: ReactNode }) {
  const { api } = useApi();
  const qc = useQueryClient();
  const [sesion, setSesion] = useState<Sesion | null>(null);
  const [cargando, setCargando] = useState(true);

  useEffect(() => {
    almacen.leer().then((raw) => {
      const r = raw ? sesionSchema.safeParse(JSON.parse(raw)) : null;
      if (r?.success) setSesion(r.data);
    }).finally(() => setCargando(false));
  }, [almacen]);

  const login = useCallback<EstadoSesion['login']>(async (input) => {
    const v = loginSchema.safeParse(input);
    if (!v.success) return { ok: false, errores: erroresPorCampo(v.error) };
    try {
      const s = await api.auth.login(v.data);
      setSesion(s);
      await almacen.guardar(JSON.stringify(s));
      return { ok: true };
    } catch (e) {
      return { ok: false, errores: { _: e instanceof Error ? e.message : 'No se pudo iniciar sesión.' } };
    }
  }, [api, almacen]);

  const logout = useCallback(async () => {
    await api.auth.logout().catch(() => undefined);
    await almacen.borrar();
    qc.clear();
    setSesion(null);
  }, [api, almacen, qc]);

  const value = useMemo(() => ({ sesion, cargando, login, logout }), [sesion, cargando, login, logout]);
  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}

export function useSesion(): EstadoSesion {
  const v = useContext(Ctx);
  if (!v) throw new Error('useSesion debe usarse dentro de <SesionProvider>.');
  return v;
}

export function useUsuario() {
  const { sesion } = useSesion();
  if (!sesion) throw new Error('No hay sesión activa.');
  return sesion.usuario;
}

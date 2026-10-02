import { useMemo, useState, type ReactNode } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createHttpApi, createMockApi } from '@/api';
import { ApiProvider, SesionProvider, type AlmacenSesion } from '@/hooks';
import { ToastProvider } from '@/ui/Toast';
import { Accesibilidad } from '@/ui/Accesibilidad';

const API_URL = import.meta.env.VITE_API_URL as string | undefined;
const HOY_DEMO = '2026-10-01T12:00:00-05:00';
const CLAVE = 'aula.sesion';

/** Almacén web. En React Native se reemplaza por AsyncStorage / SecureStore. */
const almacenLocal: AlmacenSesion = {
  leer: async () => localStorage.getItem(CLAVE),
  guardar: async (v) => localStorage.setItem(CLAVE, v),
  borrar: async () => localStorage.removeItem(CLAVE),
};

/** El mock guarda su estado en memoria; la sesión también, para que no queden desincronizados al recargar. */
function almacenMemoria(): AlmacenSesion {
  let v: string | null = null;
  return { leer: async () => v, guardar: async (x) => { v = x; }, borrar: async () => { v = null; } };
}

export function Providers({ children }: { children: ReactNode }) {
  const [qc] = useState(() => new QueryClient({ defaultOptions: { queries: { staleTime: 30_000, retry: 1, refetchOnWindowFocus: false } } }));
  const entorno = useMemo(() => {
    if (API_URL) {
      // Sesión vencida o token inválido (CU-01): se borra la sesión y se vuelve al acceso con un aviso.
      const onSesionVencida = () => {
        if (!localStorage.getItem(CLAVE)) return;
        localStorage.removeItem(CLAVE);
        window.location.assign('/login?sesion=vencida');
      };
      return { api: createHttpApi({ baseUrl: API_URL, getToken: () => JSON.parse(localStorage.getItem(CLAVE) ?? 'null')?.token ?? null, onSesionVencida }), hoy: undefined, almacen: almacenLocal };
    }
    const hoy = () => HOY_DEMO;
    return { api: createMockApi({ hoy }), hoy, almacen: almacenMemoria() };
  }, []);

  return (
    <QueryClientProvider client={qc}>
      <ApiProvider api={entorno.api} hoy={entorno.hoy}>
        <SesionProvider almacen={entorno.almacen}>
          <ToastProvider>{children}<Accesibilidad /></ToastProvider>
        </SesionProvider>
      </ApiProvider>
    </QueryClientProvider>
  );
}

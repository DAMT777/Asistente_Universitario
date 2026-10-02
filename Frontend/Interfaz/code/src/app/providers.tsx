import { useMemo, useState, type ReactNode } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createHttpApi, createMockApi } from '@/api';
import { ApiProvider, SesionProvider, type AlmacenSesion } from '@/hooks';
import { ToastProvider } from '@/ui/Toast';

/** Con VITE_API_URL (p. ej. "/api" con `npm run dev:backend`) se usa el backend real; sin ella, la api simulada. */
const API_URL = import.meta.env.VITE_API_URL as string | undefined;
const HOY_DEMO = '2026-10-01';
/** Usuarios semilla de cada entorno; la contraseña del backend de desarrollo es Demo1234!. */
const DEMO_MOCK = { estudiante: '160005017', docente: 'lrincon' };
const DEMO_BACKEND = { estudiante: 'E0001', docente: 'P0001' };
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
      return { api: createHttpApi({ baseUrl: API_URL, getToken: () => JSON.parse(localStorage.getItem(CLAVE) ?? 'null')?.token ?? null }), hoy: undefined, almacen: almacenLocal, demo: DEMO_BACKEND };
    }
    const hoy = () => HOY_DEMO;
    return { api: createMockApi({ hoy }), hoy, almacen: almacenMemoria(), demo: DEMO_MOCK };
  }, []);

  return (
    <QueryClientProvider client={qc}>
      <ApiProvider api={entorno.api} hoy={entorno.hoy} usuariosDemo={entorno.demo}>
        <SesionProvider almacen={entorno.almacen}>
          <ToastProvider>{children}</ToastProvider>
        </SesionProvider>
      </ApiProvider>
    </QueryClientProvider>
  );
}

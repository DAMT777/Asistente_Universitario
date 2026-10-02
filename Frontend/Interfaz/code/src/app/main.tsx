import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { RouterProvider } from 'react-router-dom';
import { Providers } from './providers';
import { router } from './router';
import { aplicarVidrioGuardado, instalarTema } from '@/ui/tema';
import { aplicarAccesibilidadGuardada } from '@/ui/Accesibilidad';
import '@/ui/global.css';

// Antes del primer render: así el modo nocturno o el vidrio guardados no parpadean al cargar.
instalarTema();
aplicarAccesibilidadGuardada();
aplicarVidrioGuardado();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Providers>
      <RouterProvider router={router} />
    </Providers>
  </StrictMode>,
);

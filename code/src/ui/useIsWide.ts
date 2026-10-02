import { useEffect, useState } from 'react';
import { breakpoints } from '@/theme/tokens';

/** Solo web. En React Native se reemplaza por useWindowDimensions(). */
export function useIsWide(min = breakpoints.ancho) {
  const q = `(min-width: ${min}px)`;
  const [wide, setWide] = useState(() => typeof window !== 'undefined' && window.matchMedia(q).matches);
  useEffect(() => {
    const m = window.matchMedia(q);
    const on = () => setWide(m.matches);
    m.addEventListener('change', on);
    return () => m.removeEventListener('change', on);
  }, [q]);
  return wide;
}

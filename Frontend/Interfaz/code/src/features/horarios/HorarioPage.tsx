import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import type { BloqueHorario } from '@/api/contratos';
import { useApi, useUsuario } from '@/hooks';
import { Cargando, ErrorEstado } from '@/ui';
import './horario.css';

const DIAS = ['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];
const HORAS = Array.from({ length: 13 }, (_, i) => `${String(i + 6).padStart(2, '0')}:00`);
const DURACION = 12 * 60;

function minutos(hora: string) {
  const [h, m] = hora.split(':').map(Number);
  return (h - 6) * 60 + m;
}

function distribuir(bloques: BloqueHorario[]) {
  const fines: number[] = [];
  const ubicados = [...bloques].sort((a, b) => a.inicio.localeCompare(b.inicio)).map(bloque => {
    const inicio = minutos(bloque.inicio);
    const fin = minutos(bloque.fin);
    const libre = fines.findIndex(hora => hora <= inicio);
    const carril = libre < 0 ? fines.length : libre;
    fines[carril] = fin;
    return { bloque, inicio, fin, carril };
  });
  return { ubicados, carriles: fines.length };
}

export function HorarioPage() {
  const { api } = useApi();
  const usuario = useUsuario();
  const prefijo = usuario.rol === 'docente' ? '/d' : '/e';
  const horario = useQuery({ queryKey: ['horario-semanal', usuario.id], queryFn: () => api.horarios.semana() });

  return (
    <section className="au-horario" aria-labelledby="horario-titulo">
      <h1 id="horario-titulo">Horario inscrito</h1>
      {horario.isLoading && <Cargando />}
      {horario.isError && <ErrorEstado error={horario.error} onReintentar={() => void horario.refetch()} />}
      {horario.data && (
        <>
          {horario.data.length === 0 && <p role="status">Tus cursos todavía no tienen clases programadas.</p>}
          <div className="au-horario-scroll" role="region" aria-label="Horario de lunes a sábado. Desplázate para ver todos los días." tabIndex={0}>
            <div className="au-horario-grilla">
              <div aria-hidden="true" />
              {DIAS.map(dia => <strong className="au-horario-dia" key={dia}>{dia}</strong>)}
              <div className="au-horario-horas" aria-hidden="true">
                {HORAS.map((hora, i) => <span key={hora} style={{ top: `${i / 12 * 100}%` }}>{hora}</span>)}
              </div>
              {DIAS.map((dia, i) => {
                const { ubicados, carriles } = distribuir(horario.data.filter(b => b.dia === i + 1));
                return (
                  <div className="au-horario-columna" key={dia} role="group" aria-label={dia}>
                    {ubicados.map(({ bloque, inicio, fin, carril }, posicion) => (
                      <Link key={`${bloque.cursoId}-${bloque.inicio}-${posicion}`}
                        className="au-horario-clase"
                        to={`${prefijo}/cursos/${bloque.cursoId}`}
                        aria-label={`${dia}, ${bloque.inicio} a ${bloque.fin}, ${bloque.nombre}, ${bloque.aula}`}
                        title={`${bloque.inicio}–${bloque.fin} · ${bloque.nombre} · ${bloque.aula}`}
                        style={{
                          top: `${inicio / DURACION * 100}%`,
                          height: `${(fin - inicio) / DURACION * 100}%`,
                          left: `${carril / carriles * 100}%`,
                          width: `${100 / carriles}%`,
                        }}>
                        <span>{bloque.inicio}–{bloque.fin}</span>
                        <strong>{bloque.nombre}</strong>
                        <span>{bloque.aula}</span>
                      </Link>
                    ))}
                  </div>
                );
              })}
            </div>
          </div>
        </>
      )}
    </section>
  );
}

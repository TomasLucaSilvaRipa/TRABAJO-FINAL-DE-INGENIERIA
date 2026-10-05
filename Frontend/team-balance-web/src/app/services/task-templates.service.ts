import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Tarea } from './tasks.service';

export interface PlantillaTarea {
  id: number;
  idAgencia: number;
  idSkillRequerido?: number | null;
  nombre: string;
  tituloSugerido?: string | null;
  descripcionBase?: string | null;
  horasEstimadas?: number | null;
  complejidad?: string | null;
  prioridadSugerida?: string | null;
  skillRequerido?: string | null;
  seniorityRecomendado?: string | null;
  estado: string;
  activo: boolean;
  fechaCreacion: string;
  fechaBaja?: string | null;
  motivoBaja?: string | null;
  checklistBaseJson?: string | null;
  archivosAdjuntosJson?: string | null;
}

@Injectable({ providedIn: 'root' })
export class TaskTemplatesService {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/plantillas-tareas';

  consultar(incluirInactivas = false): Observable<PlantillaTarea[]> {
    return this.http.get<PlantillaTarea[]>(this.url, { params: { incluirInactivas } });
  }

  consultarTareasBase(): Observable<Tarea[]> {
    return this.http.get<Tarea[]>(`${this.url}/tareas-base`);
  }

  crearDesdeTarea(tarea: Partial<Tarea>): Observable<PlantillaTarea> {
    return this.http.post<PlantillaTarea>(`${this.url}/desde-tarea`, tarea);
  }

  guardar(plantilla: Partial<PlantillaTarea>): Observable<PlantillaTarea> {
    return plantilla.id ? this.http.put<PlantillaTarea>(this.url, plantilla) : this.http.post<PlantillaTarea>(this.url, plantilla);
  }

  darBaja(plantilla: Partial<PlantillaTarea>): Observable<boolean> {
    return this.http.patch<boolean>(`${this.url}/baja`, plantilla);
  }
}

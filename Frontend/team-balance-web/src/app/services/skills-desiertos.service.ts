import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface FiltroCoberturaSkill {
  fechaDesde?: string | null;
  fechaHasta?: string | null;
  idProyecto?: number | null;
  idCliente?: number | null;
}

export interface TareaCoberturaSkill {
  idTarea: number;
  tituloTarea: string;
  horasEstimadas: number;
  deadline: string;
  idProyecto: number;
  nombreProyecto: string;
  nombreCliente: string;
}

export interface EmpleadoCoberturaSkill {
  idEmpleado: number;
  nombreEmpleado: string;
  seniority: string;
  nivelSkill: string;
  horasSemanales: number;
  cargaActual: number;
  horasDisponibles: number;
}

export interface AnalisisCoberturaSkill {
  idSkill: number;
  nombreSkill: string;
  categoriaSkill?: string;
  demandaHoras: number;
  capacidadDisponibleHoras: number;
  capacidadTotalHoras: number;
  porcentajeCobertura: number;
  cantidadTareasAfectadas: number;
  cantidadProyectosAfectados: number;
  tipoBrecha: string;
  criticidad: string;
  puntajeCriticidad: number;
  deadlineMasProximo?: string;
  accionSugerida: string;
  tareasAfectadas: TareaCoberturaSkill[];
  empleadosRelacionados: EmpleadoCoberturaSkill[];
}

export interface RecomendacionSkill {
  idSkill: number;
  tipoAccion: string;
  observacion: string;
}

export interface ConsultaSugerenciasSkill { idSkill: number; idProyecto: number; }
export interface SugerenciaEmpleadoSkill {
  idEmpleado: number;
  nombreEmpleado: string;
  seniority: string;
  horasSemanales: number;
  cargaActual: number;
  horasDisponibles: number;
  esDelProyecto: boolean;
  tieneSkillObjetivo: boolean;
  cantidadSkillsMismaArea: number;
  skillsRelacionadas: string;
  tipoSugerencia: string;
  puntajeAfinidad: number;
  motivo: string;
}

@Injectable({ providedIn: 'root' })
export class SkillsDesiertosService {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/skills-desiertos';

  resumen(): Observable<AnalisisCoberturaSkill[]> { return this.http.get<AnalisisCoberturaSkill[]>(`${this.url}/resumen`); }
  analizar(filtro: FiltroCoberturaSkill): Observable<AnalisisCoberturaSkill[]> { return this.http.post<AnalisisCoberturaSkill[]>(`${this.url}/analizar`, filtro); }
  detalle(idSkill: number, filtro: FiltroCoberturaSkill): Observable<AnalisisCoberturaSkill> { return this.http.post<AnalisisCoberturaSkill>(`${this.url}/detalle/${idSkill}`, filtro); }
  registrarRecomendacion(recomendacion: RecomendacionSkill): Observable<boolean> { return this.http.post<boolean>(`${this.url}/recomendaciones`, recomendacion); }
  sugerirEmpleados(consulta: ConsultaSugerenciasSkill): Observable<SugerenciaEmpleadoSkill[]> { return this.http.post<SugerenciaEmpleadoSkill[]>(`${this.url}/sugerencias`, consulta); }
}

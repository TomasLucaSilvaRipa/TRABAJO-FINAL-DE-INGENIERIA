import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface AnalisisOcupacionOperativa {
  porcentajeOcupacionGeneral: number;
  horasDisponiblesSemanales: number;
  horasEstimadasActivas: number;
  cantidadSubutilizados: number;
  cantidadBalanceados: number;
  cantidadSobrecargados: number;
  empleados: OcupacionEmpleado[];
  proyectos: OcupacionProyecto[];
  clientes: OcupacionCliente[];
}

export interface FiltroOcupacionOperativa {
  idCliente?: number | null;
  idProyecto?: number | null;
  idEmpleado?: number | null;
  estadoCarga?: string | null;
  desde?: string | null;
  hasta?: string | null;
}

export interface OcupacionEmpleado {
  idEmpleado: number;
  nombre: string;
  seniority: string;
  horasDisponibles: number;
  horasEstimadasActivas: number;
  horasRegistradas: number;
  porcentajeOcupacion: number;
  estadoCarga: string;
  causaPrincipal: string;
  accionSugerida: string;
  proyectos: string[];
}

export interface OcupacionProyecto {
  idProyecto: number;
  idCliente: number;
  idPMResponsable: number;
  nombreProyecto: string;
  nombreCliente: string;
  horasEstimadasActivas: number;
  porcentajeSobreCargaAgencia: number;
  empleados: OcupacionEmpleadoProyecto[];
}

export interface OcupacionCliente {
  idCliente: number;
  nombreCliente: string;
  horasEstimadasActivas: number;
  porcentajeSobreCargaAgencia: number;
  proyectos: OcupacionProyecto[];
}

export interface OcupacionEmpleadoProyecto {
  idEmpleado: number;
  nombreEmpleado: string;
  horasEstimadas: number;
  porcentajeSobreCargaProyecto: number;
}

export interface DetalleOcupacionEmpleado {
  empleado: OcupacionEmpleado;
  tareas: DetalleOcupacionTarea[];
}

export interface DetalleOcupacionTarea {
  idTarea: number;
  idProyecto: number;
  idPMResponsable: number;
  titulo: string;
  nombreProyecto: string;
  nombreCliente: string;
  horasEstimadas: number;
  incidenciaSobreCargaEmpleado: number;
}

export interface SolicitudRevisionOperativa {
  id: number;
  idAgencia: number;
  idEmpleado: number;
  idProyecto?: number | null;
  idTarea?: number | null;
  idPMResponsable: number;
  idUsuarioSolicitante: number;
  causaPrincipal: string;
  accionSugerida: string;
  estado: string;
  fechaAlta: string;
  nombreEmpleado: string;
  nombreProyecto: string;
  nombrePM: string;
}

export interface AnalisisDesvioHoras {
  horasEstimadas: number;
  horasReales: number;
  desvioHoras: number;
  porcentajeConsumo: number;
  proyectos: DesvioHorasProyecto[];
}

export interface DesvioHorasProyecto {
  idProyecto: number;
  nombreProyecto: string;
  nombreCliente: string;
  horasEstimadas: number;
  horasReales: number;
  desvioHoras: number;
  porcentajeConsumo: number;
  estado: string;
  tareas: DesvioHorasTarea[];
}

export interface DesvioHorasTarea {
  idTarea: number;
  idEmpleadoResponsable?: number | null;
  titulo: string;
  responsable: string;
  estadoTarea: string;
  horasEstimadas: number;
  horasReales: number;
  desvioHoras: number;
  porcentajeConsumo: number;
  estado: string;
}

export interface FiltroDesvioHoras {
  idProyecto?: number | null;
  idTarea?: number | null;
  idEmpleado?: number | null;
  estadoTarea?: string | null;
  desde?: string | null;
  hasta?: string | null;
  nivelDesvio?: string | null;
}

export interface RegistroHoraDesvio {
  id: number;
  idTarea: number;
  idEmpleado: number;
  fecha: string;
  cantidadHoras: number;
  descripcion?: string | null;
}

export interface DetalleDesvioHorasTarea {
  idProyecto: number;
  idTarea: number;
  nombreProyecto: string;
  titulo: string;
  responsable: string;
  estadoTarea: string;
  horasEstimadas: number;
  horasReales: number;
  desvioHoras: number;
  porcentajeConsumo: number;
  estadoDesvio: string;
  registrosHoras: RegistroHoraDesvio[];
}

export interface AccionAjusteDesvioHoras {
  idProyecto: number;
  idTarea?: number | null;
  observacion: string;
}

export interface AnalisisConvenienciaOperativa {
  costoPlanificado: number;
  costoReal: number;
  desvioCosto: number;
  proyectos: ConvenienciaOperativaProyecto[];
}

export interface ConvenienciaOperativaProyecto {
  idProyecto: number;
  nombreProyecto: string;
  nombreCliente: string;
  horasEstimadas: number;
  horasReales: number;
  costoPlanificado: number;
  costoReal: number;
  desvioCosto: number;
  estadoConveniencia: string;
  factorPrincipal: string;
}

export interface AnalisisSobrecalificacionPerfiles {
  horasAfectadas: number;
  impactoOperativoEstimado: number;
  cantidadTareasAfectadas: number;
  tareas: SobrecalificacionTarea[];
}

export interface SobrecalificacionTarea {
  idTarea: number;
  idProyecto: number;
  titulo: string;
  nombreProyecto: string;
  nombreCliente: string;
  seniorityRequerido: string;
  seniorityAsignado: string;
  empleadoAsignado: string;
  horasAfectadas: number;
  impactoOperativoEstimado: number;
  nivel: string;
}

@Injectable({ providedIn: 'root' })
export class AnaliticaOperativaService {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/analitica-operativa';

  consultarOcupacion(filtro: FiltroOcupacionOperativa = {}): Observable<AnalisisOcupacionOperativa> {
    return this.http.get<AnalisisOcupacionOperativa>(`${this.url}/ocupacion`, { params: this.crearParametros(filtro) });
  }

  consultarDetalleOcupacionEmpleado(filtro: FiltroOcupacionOperativa): Observable<DetalleOcupacionEmpleado> {
    return this.http.get<DetalleOcupacionEmpleado>(`${this.url}/ocupacion/detalle-empleado`, { params: this.crearParametros(filtro) });
  }

  solicitarRevisionOperativa(idEmpleado: number, idProyecto?: number | null, idTarea?: number | null): Observable<string> {
    return this.http.post(`${this.url}/ocupacion/solicitudes-revision`, { idEmpleado, idProyecto, idTarea }, { responseType: 'text' });
  }

  consultarSolicitudesRevisionPendientes(): Observable<SolicitudRevisionOperativa[]> {
    return this.http.get<SolicitudRevisionOperativa[]>(`${this.url}/ocupacion/solicitudes-revision/pendientes`);
  }

  consultarDesvioHoras(filtro: FiltroDesvioHoras = {}): Observable<AnalisisDesvioHoras> {
    return this.http.get<AnalisisDesvioHoras>(`${this.url}/desvio-horas`, { params: this.crearParametros(filtro) });
  }

  consultarDetalleDesvioHorasTarea(idProyecto: number, idTarea: number, desde?: string | null, hasta?: string | null): Observable<DetalleDesvioHorasTarea> {
    let params = new HttpParams().set('idProyecto', String(idProyecto)).set('idTarea', String(idTarea));
    if (desde) { params = params.set('desde', desde); }
    if (hasta) { params = params.set('hasta', hasta); }
    return this.http.get<DetalleDesvioHorasTarea>(`${this.url}/desvio-horas/detalle-tarea`, { params });
  }

  registrarAccionAjuste(accion: AccionAjusteDesvioHoras): Observable<string> {
    return this.http.post(`${this.url}/desvio-horas/acciones-ajuste`, accion, { responseType: 'text' });
  }

  comunicarDesvioADueno(idProyecto: number, filtro: FiltroDesvioHoras): Observable<string> {
    return this.http.post(`${this.url}/desvio-horas/comunicar-dueno`, { idProyecto, filtro }, { responseType: 'text' });
  }

  consultarConveniencia(): Observable<AnalisisConvenienciaOperativa> {
    return this.http.get<AnalisisConvenienciaOperativa>(`${this.url}/conveniencia`);
  }

  consultarSobrecalificacion(): Observable<AnalisisSobrecalificacionPerfiles> {
    return this.http.get<AnalisisSobrecalificacionPerfiles>(`${this.url}/sobrecalificacion`);
  }

  private crearParametros(filtro: object): HttpParams {
    let params = new HttpParams();
    Object.entries(filtro).forEach(([clave, valor]) => { if (valor !== null && valor !== undefined && valor !== '') { params = params.set(clave, String(valor)); } });
    return params;
  }
}

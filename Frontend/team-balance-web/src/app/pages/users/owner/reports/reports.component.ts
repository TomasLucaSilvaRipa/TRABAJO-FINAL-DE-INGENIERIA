import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Cliente, ProjectsService, Proyecto, UsuarioOpcion } from '../../../../services/projects.service';
import { ConfiguracionReporteEjecutivo, ReporteEjecutivo, ReporteEjecutivoService } from '../../../../services/reporte-ejecutivo.service';
import { DetalleRiesgoProyecto, FiltroRiesgoRetraso, NuevoSeguimientoRiesgoRetraso, RiesgoRetrasoService, TareaRiesgoRetraso } from '../../../../services/riesgo-retraso.service';
import { AccionAjusteDesvioHoras, AnalisisConvenienciaOperativa, AnalisisDesvioHoras, AnalisisOcupacionOperativa, AnalisisSobrecalificacionPerfiles, AnaliticaOperativaService, ConvenienciaOperativaCliente, ConvenienciaOperativaProyecto, DesvioHorasProyecto, DesvioHorasTarea, DetalleConvenienciaOperativa, DetalleDesvioHorasTarea, DetalleOcupacionEmpleado, DetalleOcupacionTarea, FiltroConvenienciaOperativa, FiltroDesvioHoras, FiltroOcupacionOperativa, NuevoSeguimientoConvenienciaOperativa, OcupacionEmpleado, OcupacionProyecto, RevisionOperativaPendiente, SobrecalificacionTarea } from '../../../../services/analitica-operativa.service';
import { AuthService } from '../../../../services/auth.service';
import { ImpactSimulationService, SimulacionImpacto } from '../../../../services/impact-simulation.service';
import { LocalizationService } from '../../../../services/localization.service';
import { AsignacionTarea, Tarea, TareaOpciones, TasksService } from '../../../../services/tasks.service';
import { HoursDrilldownChartComponent } from '../../../../shared/components/analytics/hours-drilldown-chart/hours-drilldown-chart.component';
import { OccupancyLandscapeComponent, SimulacionOcupacionVisual } from '../../../../shared/components/analytics/occupancy-landscape/occupancy-landscape.component';
import { OverqualificationMapComponent } from '../../../../shared/components/analytics/overqualification-map/overqualification-map.component';
import { RiesgoTareaSeleccionada, RiskObservatoryComponent } from '../../../../shared/components/analytics/risk-observatory/risk-observatory.component';
import { ConvenienceMapComponent } from '../../../../shared/components/analytics/convenience-map/convenience-map.component';

type VistaAnalisis = 'riesgo' | 'ocupacion' | 'desvio' | 'conveniencia' | 'sobrecalificacion' | 'revisiones';
type EstadoVisualOcupacion = 'subutilizada' | 'balanceada' | 'sobrecargada' | 'critica';

@Component({ selector: 'app-reports', imports: [DatePipe, DecimalPipe, FormsModule, HoursDrilldownChartComponent, OccupancyLandscapeComponent, OverqualificationMapComponent, RiskObservatoryComponent, ConvenienceMapComponent], templateUrl: './reports.component.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class Reports {
  private readonly projectsService = inject(ProjectsService);
  private readonly riesgoService = inject(RiesgoRetrasoService);
  private readonly reporteService = inject(ReporteEjecutivoService);
  private readonly analiticaService = inject(AnaliticaOperativaService);
  private readonly authService = inject(AuthService);
  private readonly simulationService = inject(ImpactSimulationService);
  private readonly tasksService = inject(TasksService);
  protected readonly localization = inject(LocalizationService);
  protected readonly proyectos = signal<Proyecto[]>([]);
  protected readonly clientes = signal<Cliente[]>([]);
  protected readonly responsables = signal<UsuarioOpcion[]>([]);
  protected readonly riesgos = signal<DetalleRiesgoProyecto[]>([]);
  protected readonly riesgoSeleccionado = signal<DetalleRiesgoProyecto | null>(null);
  protected readonly tareaRiesgoSeleccionada = signal<RiesgoTareaSeleccionada | null>(null);
  protected readonly cerrandoDetalleRiesgo = signal(false);
  protected readonly ocupacion = signal<AnalisisOcupacionOperativa | null>(null);
  protected readonly tareasSimulacion = signal<Tarea[]>([]);
  protected readonly empleadosSimulacion = signal<TareaOpciones['empleados']>([]);
  protected readonly escenariosConservados = signal<SimulacionImpacto[]>([]);
  protected readonly escenarioOcupacion = signal<SimulacionImpacto | null>(null);
  protected readonly asignacionesTareaSimulacion = signal<AsignacionTarea[]>([]);
  protected readonly idTareaSimulacion = signal(0);
  protected readonly idEmpleadoSimulacion = signal(0);
  protected readonly idEscenarioConservado = signal(0);
  protected readonly cargandoSimulacion = signal(false);
  protected readonly simulacionVisual = computed<SimulacionOcupacionVisual | null>(() => {
    const escenario = this.escenarioOcupacion();
    const tarea = this.tareasSimulacion().find(item => item.id === escenario?.idTarea);
    if (!escenario || !tarea) { return null; }
    const empleado = this.empleadosSimulacion().find(item => item.id === escenario.idEmpleadoCandidato);
    const asignaciones = this.asignacionesTareaSimulacion().filter(item => item.activa !== false && item.idEmpleado !== escenario.idEmpleadoCandidato);
    const totalAsignado = asignaciones.reduce((total, item) => total + Number(item.horasAsignadas || 0), 0);
    const horasImpacto = Number(escenario.horasTarea ?? 0);
    const datosOcupacion = this.ocupacion();
    const afectados = asignaciones.map(asignacion => {
      const actual = datosOcupacion?.empleados.find(item => item.idEmpleado === asignacion.idEmpleado);
      const proporcion = totalAsignado > 0 ? Number(asignacion.horasAsignadas || 0) / totalAsignado : 1 / Math.max(1, asignaciones.length);
      const reduccion = actual?.horasDisponibles ? horasImpacto * proporcion / actual.horasDisponibles * 100 : 0;
      return { idEmpleado: asignacion.idEmpleado, porcentajeProyectado: Math.max(0, Number(actual?.porcentajeOcupacion ?? 0) - reduccion) };
    });
    return { idProyecto: tarea.idProyecto, idEmpleado: escenario.idEmpleadoCandidato, nombreEmpleado: escenario.nombreEmpleado || (empleado ? `${empleado.nombre} ${empleado.apellido}` : 'Recurso simulado'), porcentajeProyectado: Number(escenario.porcentajeOcupacionProyectado ?? 0), horasImpacto, afectados };
  });
  protected readonly detalleOcupacion = signal<DetalleOcupacionEmpleado | null>(null);
  protected readonly cerrandoDetalleOcupacion = signal(false);
  protected readonly situacionARevisar = signal<DetalleOcupacionEmpleado | null>(null);
  protected readonly tareaARevisar = signal<DetalleOcupacionTarea | null>(null);
  protected readonly solicitudesRevision = signal<RevisionOperativaPendiente[]>([]);
  protected readonly desvioHoras = signal<AnalisisDesvioHoras | null>(null);
  protected readonly detalleDesvio = signal<DetalleDesvioHorasTarea | null>(null);
  protected readonly cerrandoDetalleDesvio = signal(false);
  protected readonly proyectoAComunicar = signal<DesvioHorasProyecto | null>(null);
  protected readonly proyectoDesvioSeleccionado = signal<DesvioHorasProyecto | null>(null);
  protected readonly conveniencia = signal<AnalisisConvenienciaOperativa | null>(null);
  protected readonly detalleConveniencia = signal<DetalleConvenienciaOperativa | null>(null);
  protected readonly cerrandoDetalleConveniencia = signal(false);
  protected readonly clienteConvenienciaARevisar = signal<ConvenienciaOperativaCliente | null>(null);
  protected readonly mensajeConveniencia = signal('');
  protected readonly hayFiltrosConvenienciaAplicados = signal(false);
  protected readonly sobrecalificacion = signal<AnalisisSobrecalificacionPerfiles | null>(null);
  protected readonly detalleSobrecalificacion = signal<SobrecalificacionTarea | null>(null);
  protected readonly sobrecalificacionARevisar = signal<SobrecalificacionTarea | null>(null);
  protected readonly observacionSobrecalificacion = signal('');
  protected readonly mensajeSobrecalificacion = signal('');
  protected readonly mensajeRiesgo = signal('');
  protected readonly vista = signal<VistaAnalisis>('riesgo');
  protected readonly cargando = signal(false);
  protected readonly error = signal('');
  protected readonly reporte = signal<ReporteEjecutivo | null>(null);
  protected readonly generandoReporte = signal(false);
  protected readonly procesandoDesvio = signal(false);
  protected readonly mensajeDesvio = signal('');
  protected readonly esDueno = this.authService.usuarioActual()?.roles.some(rol => rol.tipoUsuario === 'Dueno') ?? false;
  protected readonly esPM = this.authService.usuarioActual()?.roles.some(rol => rol.tipoUsuario === 'PM') ?? false;
  protected filtro: FiltroRiesgoRetraso = {};
  protected filtroOcupacion: FiltroOcupacionOperativa = {};
  protected filtroDesvio: FiltroDesvioHoras = {};
  protected filtroConveniencia: FiltroConvenienciaOperativa = {};
  protected accionAjuste: AccionAjusteDesvioHoras = { idProyecto: 0, idTarea: null, observacion: '' };
  protected seguimientoConveniencia: NuevoSeguimientoConvenienciaOperativa = { idCliente: 0, idProyecto: null, observacion: '', accionSugerida: '' };
  protected seguimientoRiesgo: NuevoSeguimientoRiesgoRetraso = { idProyecto: 0, idTarea: null, observacion: '', comunicarResponsable: true };
  protected configuracionReporte: ConfiguracionReporteEjecutivo = {
    titulo: 'Reporte ejecutivo de gestión', filtro: { alcanceGeneral: true }, incluirOcupacionOperativa: true,
    incluirRiesgoRetraso: true, incluirDesvioHoras: true, incluirEficienciaPerfiles: true,
    incluirConvenienciaOperativa: true, incluirTodasLasSecciones: true,
  };

  constructor() {
    this.projectsService.consultar().subscribe({ next: proyectos => this.proyectos.set(proyectos.filter(proyecto => proyecto.activo)), error: () => this.error.set('No se pudieron cargar los proyectos para aplicar filtros.') });
    this.projectsService.opciones().subscribe({ next: opciones => { this.clientes.set(opciones.clientes.filter(cliente => cliente.activo)); this.responsables.set(opciones.responsables); }, error: () => this.error.set('No se pudieron cargar los clientes para aplicar filtros.') });
    this.consultarRiesgos();
  }

  protected cambiarVista(vista: VistaAnalisis): void {
    if (vista !== 'ocupacion') { this.restaurarOcupacionReal(); }
    this.vista.set(vista);
    this.error.set('');
    if (vista === 'riesgo' && this.riesgos().length === 0) { this.consultarRiesgos(); }
    if (vista === 'ocupacion') { if (!this.ocupacion()) { this.consultarOcupacion(); } this.cargarOpcionesSimulacion(); }
    if (vista === 'desvio') { this.consultarDesvioHoras(); }
    if (vista === 'conveniencia' && !this.conveniencia()) { this.consultarConveniencia(); }
    if (vista === 'sobrecalificacion' && !this.sobrecalificacion()) { this.consultarSobrecalificacion(); }
    if (vista === 'revisiones') { this.consultarSolicitudesRevision(); }
  }

  protected actualizarVista(): void {
    if (this.vista() === 'riesgo') { this.consultarRiesgos(); return; }
    if (this.vista() === 'ocupacion') { this.consultarOcupacion(); return; }
    if (this.vista() === 'desvio') { this.consultarDesvioHoras(); return; }
    if (this.vista() === 'conveniencia') { this.consultarConveniencia(); return; }
    if (this.vista() === 'sobrecalificacion') { this.consultarSobrecalificacion(); return; }
    this.consultarSolicitudesRevision();
  }

  protected consultarRiesgos(): void {
    this.cargando.set(true); this.error.set('');
    this.riesgoService.consultar(this.filtro).subscribe({ next: resultado => { this.riesgos.set(resultado.proyectos); this.riesgoSeleccionado.set(null); this.tareaRiesgoSeleccionada.set(null); this.cerrandoDetalleRiesgo.set(false); this.cargando.set(false); }, error: error => { this.error.set(error?.error || 'No se pudo calcular el riesgo de retraso.'); this.cargando.set(false); } });
  }

  protected hayFiltrosRiesgoAplicados(): boolean {
    return this.filtro.idProyecto != null || this.filtro.idCliente != null || this.filtro.idPM != null || !!this.filtro.nivelRiesgo || this.filtro.diasHastaDeadlineMaximo != null;
  }

  protected limpiarFiltrosRiesgo(): void {
    this.filtro = {};
    this.consultarRiesgos();
  }

  protected consultarOcupacion(): void {
    this.restaurarOcupacionReal();
    this.cargando.set(true); this.error.set('');
    this.analiticaService.consultarOcupacion(this.filtroOcupacion).subscribe({ next: resultado => { this.ocupacion.set(resultado); this.detalleOcupacion.set(null); this.cerrandoDetalleOcupacion.set(false); this.cargando.set(false); }, error: error => { this.error.set(error?.error || 'No se pudo analizar la ocupación operativa.'); this.cargando.set(false); } });
  }

  protected simularEnOcupacion(): void {
    if (!this.idTareaSimulacion() || !this.idEmpleadoSimulacion()) { this.error.set('Seleccioná una tarea y un empleado para simular.'); return; }
    this.error.set(''); this.cargandoSimulacion.set(true);
    this.simulationService.calcular({ idTarea: this.idTareaSimulacion(), idEmpleadoCandidato: this.idEmpleadoSimulacion() }).subscribe({
      next: escenario => { this.aplicarEscenarioOcupacion(escenario); this.idEscenarioConservado.set(0); this.cargandoSimulacion.set(false); },
      error: respuesta => { this.error.set(typeof respuesta?.error === 'string' ? respuesta.error : 'No se pudo calcular la simulación.'); this.cargandoSimulacion.set(false); },
    });
  }

  protected conservarSimulacionOcupacion(): void {
    const escenario = this.escenarioOcupacion(); if (!escenario || escenario.id > 0) { return; }
    this.cargandoSimulacion.set(true);
    this.simulationService.conservar(escenario).subscribe({ next: guardado => { this.escenarioOcupacion.set(guardado); this.escenariosConservados.update(items => [guardado, ...items.filter(item => item.id !== guardado.id)]); this.idEscenarioConservado.set(guardado.id); this.cargandoSimulacion.set(false); }, error: respuesta => { this.error.set(typeof respuesta?.error === 'string' ? respuesta.error : 'No se pudo conservar la simulación.'); this.cargandoSimulacion.set(false); } });
  }

  protected revisarEscenarioConservado(id: number): void {
    this.error.set('');
    this.idEscenarioConservado.set(id);
    const escenario = this.escenariosConservados().find(item => item.id === id) ?? null;
    this.aplicarEscenarioOcupacion(escenario);
    this.idTareaSimulacion.set(escenario?.idTarea ?? 0);
    this.idEmpleadoSimulacion.set(escenario?.idEmpleadoCandidato ?? 0);
  }

  protected restaurarOcupacionReal(): void { this.escenarioOcupacion.set(null); this.asignacionesTareaSimulacion.set([]); this.idEscenarioConservado.set(0); }

  private aplicarEscenarioOcupacion(escenario: SimulacionImpacto | null): void {
    this.escenarioOcupacion.set(escenario);
    this.asignacionesTareaSimulacion.set([]);
    if (!escenario) { return; }
    const tarea = this.tareasSimulacion().find(item => item.id === escenario.idTarea);
    this.tasksService.consultarAsignaciones(escenario.idTarea).subscribe({
      next: asignaciones => this.asignacionesTareaSimulacion.set(asignaciones.length ? asignaciones : tarea?.idEmpleadoAsignado ? [{ idEmpleado: tarea.idEmpleadoAsignado, horasAsignadas: tarea.horasEstimadas, activa: true }] : []),
      error: () => this.asignacionesTareaSimulacion.set(tarea?.idEmpleadoAsignado ? [{ idEmpleado: tarea.idEmpleadoAsignado, horasAsignadas: tarea.horasEstimadas, activa: true }] : []),
    });
  }

  private cargarOpcionesSimulacion(): void {
    if (!this.esPM || this.esDueno || this.tareasSimulacion().length) { return; }
    this.simulationService.consultarTareasDisponibles().subscribe({ next: tareas => this.tareasSimulacion.set(tareas), error: () => this.error.set('No se pudieron cargar las tareas disponibles para simular.') });
    this.tasksService.opciones().subscribe({ next: opciones => this.empleadosSimulacion.set(opciones.empleados), error: () => this.error.set('No se pudieron cargar los empleados disponibles para simular.') });
    this.simulationService.consultarConservadas().subscribe({ next: escenarios => this.escenariosConservados.set(escenarios), error: () => this.error.set('No se pudieron cargar las simulaciones conservadas.') });
  }

  protected estiloEstadoOcupacion(estado: string): EstadoVisualOcupacion {
    if (estado === 'Crítico') { return 'critica'; }
    if (estado === 'Sobrecargado') { return 'sobrecargada'; }
    if (estado === 'Balanceado') { return 'balanceada'; }
    return 'subutilizada';
  }

  protected textoBalanceHoras(horasRestantes: number, horasExcedidas: number): string {
    if (horasRestantes > 0) { return `${horasRestantes} h restantes`; }
    if (horasExcedidas > 0) { return `${horasExcedidas} h excedidas`; }
    return 'Sin diferencia';
  }

  protected abrirDetalleOcupacion(empleado: OcupacionEmpleado, proyecto?: OcupacionProyecto): void {
    const filtroDetalle: FiltroOcupacionOperativa = {
      ...this.filtroOcupacion,
      idEmpleado: empleado.idEmpleado,
      idProyecto: proyecto?.idProyecto ?? this.filtroOcupacion.idProyecto,
    };
    this.error.set('');
    this.analiticaService.consultarDetalleOcupacionEmpleado(filtroDetalle).subscribe({ next: detalle => { this.cerrandoDetalleOcupacion.set(false); this.detalleOcupacion.set(detalle); }, error: error => this.error.set(error?.error || 'No se pudo consultar el detalle del empleado.') });
  }

  protected cerrarDetalleOcupacion(): void {
    if (!this.detalleOcupacion() || this.cerrandoDetalleOcupacion()) { return; }
    this.cerrandoDetalleOcupacion.set(true);
    setTimeout(() => { this.detalleOcupacion.set(null); this.cerrandoDetalleOcupacion.set(false); }, 180);
  }

  protected prepararSolicitudRevision(detalle: DetalleOcupacionEmpleado, tarea: DetalleOcupacionTarea): void {
    this.mensajeDesvio.set('');
    this.tareaARevisar.set(tarea);
    this.situacionARevisar.set(detalle);
  }

  protected enviarSolicitudRevision(): void {
    const detalle = this.situacionARevisar();
    const tarea = this.tareaARevisar();
    if (!detalle || !tarea) { return; }
    this.procesandoDesvio.set(true); this.error.set('');
    this.analiticaService.solicitarRevisionOperativa(detalle.empleado.idEmpleado, tarea.idProyecto, tarea.idTarea).subscribe({ next: mensaje => { this.mensajeDesvio.set(mensaje); this.situacionARevisar.set(null); this.tareaARevisar.set(null); this.cerrarDetalleOcupacion(); this.procesandoDesvio.set(false); }, error: error => { this.error.set(error?.error || 'No fue posible enviar la solicitud de revisión.'); this.procesandoDesvio.set(false); } });
  }

  protected consultarSolicitudesRevision(): void {
    this.cargando.set(true); this.error.set('');
    this.analiticaService.consultarSolicitudesRevisionPendientes().subscribe({ next: solicitudes => { this.solicitudesRevision.set(solicitudes); this.cargando.set(false); }, error: error => { this.error.set(error?.error || 'No se pudieron consultar las revisiones pendientes.'); this.cargando.set(false); } });
  }

  protected resolverSolicitudRevision(solicitud: RevisionOperativaPendiente): void {
    this.procesandoDesvio.set(true); this.error.set('');
    this.analiticaService.resolverSolicitudRevision(solicitud.id, solicitud.tipo).subscribe({
      next: mensaje => {
        this.mensajeDesvio.set(mensaje);
        this.solicitudesRevision.update(solicitudes => solicitudes.filter(item => item.id !== solicitud.id || item.tipo !== solicitud.tipo));
        this.procesandoDesvio.set(false);
      },
      error: error => { this.error.set(error?.error || 'No fue posible marcar la revisión como resuelta.'); this.procesandoDesvio.set(false); }
    });
  }

  protected etiquetaTipoRevision(tipo: RevisionOperativaPendiente['tipo']): string {
    if (tipo === 'ConvenienciaCliente') { return 'Conveniencia'; }
    if (tipo === 'ControlHoras') { return 'Control de horas'; }
    if (tipo === 'RiesgoRetraso') { return 'Riesgo de retraso'; }
    return 'Asignación';
  }

  protected consultarDesvioHoras(): void {
    this.cargando.set(true); this.error.set('');
    this.analiticaService.consultarDesvioHoras(this.filtroDesvio).subscribe({ next: resultado => { this.desvioHoras.set(resultado); this.proyectoDesvioSeleccionado.set(null); this.detalleDesvio.set(null); this.cerrandoDetalleDesvio.set(false); this.cargando.set(false); }, error: error => { this.error.set(error?.error || 'No se pudo analizar el desvío de horas.'); this.cargando.set(false); } });
  }

  protected hayFiltrosDesvioAplicados(): boolean {
    return Object.values(this.filtroDesvio).some(valor => valor !== null && valor !== undefined && valor !== '');
  }

  protected limpiarFiltrosDesvio(): void {
    this.filtroDesvio = {};
    this.consultarDesvioHoras();
  }

  protected seleccionarProyectoDesvio(proyecto: DesvioHorasProyecto | null): void { this.proyectoDesvioSeleccionado.set(proyecto); }

  protected abrirDetalleDesvio(proyecto: DesvioHorasProyecto, tarea: DesvioHorasTarea): void {
    this.error.set(''); this.mensajeDesvio.set('');
    this.analiticaService.consultarDetalleDesvioHorasTarea(proyecto.idProyecto, tarea.idTarea, this.filtroDesvio.desde, this.filtroDesvio.hasta).subscribe({ next: detalle => { this.cerrandoDetalleDesvio.set(false); this.detalleDesvio.set(detalle); }, error: error => this.error.set(error?.error || 'No se pudo consultar el detalle del desvío.') });
  }

  protected abrirAjusteDesdeDetalleDesvio(detalle: DetalleDesvioHorasTarea): void {
    const proyecto = this.desvioHoras()?.proyectos.find((item) => item.idProyecto === detalle.idProyecto);
    const tarea = proyecto?.tareas.find((item) => item.idTarea === detalle.idTarea);
    if (proyecto && tarea) this.abrirAccionAjuste(proyecto, tarea);
  }

  protected cerrarDetalleDesvio(): void {
    if (!this.detalleDesvio() || this.cerrandoDetalleDesvio()) { return; }
    this.cerrandoDetalleDesvio.set(true);
    setTimeout(() => { this.detalleDesvio.set(null); this.cerrandoDetalleDesvio.set(false); }, 180);
  }

  protected abrirAccionAjuste(proyecto: DesvioHorasProyecto, tarea?: DesvioHorasTarea): void {
    this.mensajeDesvio.set('');
    this.accionAjuste = { idProyecto: proyecto.idProyecto, idTarea: tarea?.idTarea ?? null, observacion: '' };
  }

  protected cerrarAccionAjuste(): void { this.accionAjuste = { idProyecto: 0, idTarea: null, observacion: '' }; }

  protected registrarAccionAjuste(): void {
    if (!this.accionAjuste.idProyecto || !this.accionAjuste.observacion.trim()) { this.error.set('Describí la acción de ajuste antes de confirmarla.'); return; }
    this.procesandoDesvio.set(true); this.error.set('');
    this.analiticaService.registrarAccionAjuste(this.accionAjuste).subscribe({ next: mensaje => { this.mensajeDesvio.set(mensaje); this.cerrarAccionAjuste(); this.procesandoDesvio.set(false); }, error: error => { this.error.set(error?.error || 'No se pudo registrar la acción de ajuste.'); this.procesandoDesvio.set(false); } });
  }

  protected prepararComunicacion(proyecto: DesvioHorasProyecto): void { this.proyectoAComunicar.set(proyecto); this.mensajeDesvio.set(''); }

  protected comunicarDesvioADueno(): void {
    const proyecto = this.proyectoAComunicar();
    if (!proyecto) { return; }
    this.procesandoDesvio.set(true); this.error.set('');
    this.analiticaService.comunicarDesvioADueno(proyecto.idProyecto, this.filtroDesvio).subscribe({ next: mensaje => { this.mensajeDesvio.set(mensaje); this.proyectoAComunicar.set(null); this.procesandoDesvio.set(false); }, error: error => { this.error.set(error?.error || 'No se pudo comunicar el desvío al Dueño.'); this.procesandoDesvio.set(false); } });
  }

  protected tareasDisponiblesDesvio(): DesvioHorasTarea[] { return this.desvioHoras()?.proyectos.flatMap(proyecto => proyecto.tareas) ?? []; }

  protected responsablesDisponiblesDesvio(): DesvioHorasTarea[] {
    const responsables: DesvioHorasTarea[] = [];
    const ids = new Set<number>();
    for (const tarea of this.tareasDisponiblesDesvio()) {
      const idEmpleado = tarea.idEmpleadoResponsable;
      if (idEmpleado === null || idEmpleado === undefined || ids.has(idEmpleado)) { continue; }
      ids.add(idEmpleado);
      responsables.push(tarea);
    }
    return responsables;
  }

  protected consultarConveniencia(): void {
    const filtroAplicado: FiltroConvenienciaOperativa = { ...this.filtroConveniencia };
    this.cargando.set(true); this.error.set('');
    this.analiticaService.consultarConveniencia(filtroAplicado).subscribe({ next: resultado => { this.conveniencia.set(resultado); this.detalleConveniencia.set(null); this.cerrandoDetalleConveniencia.set(false); this.hayFiltrosConvenienciaAplicados.set(this.tieneFiltrosConveniencia(filtroAplicado)); this.cargando.set(false); }, error: error => { this.error.set(error?.error || 'No se pudo analizar la conveniencia operativa.'); this.cargando.set(false); } });
  }

  protected limpiarFiltrosConveniencia(): void {
    this.filtroConveniencia = {};
    this.consultarConveniencia();
  }

  private tieneFiltrosConveniencia(filtro: FiltroConvenienciaOperativa): boolean {
    return Object.values(filtro).some(valor => valor !== null && valor !== undefined && valor !== '');
  }

  protected estadosProyectoConveniencia(): string[] { return [...new Set(this.proyectos().map(proyecto => proyecto.estado).filter(Boolean))].sort(); }

  protected abrirDetalleConveniencia(proyecto: ConvenienciaOperativaProyecto): void {
    this.error.set('');
    this.analiticaService.consultarDetalleConveniencia(proyecto.idProyecto, this.filtroConveniencia).subscribe({ next: detalle => { this.cerrandoDetalleConveniencia.set(false); this.detalleConveniencia.set(detalle); }, error: error => this.error.set(error?.error || 'No se pudo consultar la composición del costo operativo.') });
  }

  protected cerrarDetalleConveniencia(): void {
    if (!this.detalleConveniencia() || this.cerrandoDetalleConveniencia()) { return; }
    this.cerrandoDetalleConveniencia.set(true);
    setTimeout(() => { this.detalleConveniencia.set(null); this.cerrandoDetalleConveniencia.set(false); }, 180);
  }

  protected abrirSeguimientoConveniencia(proyecto: ConvenienciaOperativaProyecto): void {
    this.seguimientoConveniencia = { idCliente: proyecto.idCliente, idProyecto: proyecto.idProyecto, observacion: '', accionSugerida: '' };
  }

  protected cerrarSeguimientoConveniencia(): void { this.seguimientoConveniencia = { idCliente: 0, idProyecto: null, observacion: '', accionSugerida: '' }; }

  protected guardarSeguimientoConveniencia(): void {
    if (!this.seguimientoConveniencia.idCliente || !this.seguimientoConveniencia.observacion.trim() || !this.seguimientoConveniencia.accionSugerida.trim()) { this.error.set('Completá la observación y la acción sugerida.'); return; }
    this.procesandoDesvio.set(true); this.error.set('');
    this.analiticaService.registrarSeguimientoConveniencia(this.seguimientoConveniencia).subscribe({ next: mensaje => { this.mensajeConveniencia.set(mensaje); this.cerrarSeguimientoConveniencia(); this.procesandoDesvio.set(false); const detalle = this.detalleConveniencia(); if (detalle) { this.abrirDetalleConveniencia(detalle.proyecto); } }, error: error => { this.error.set(error?.error || 'No se pudo registrar el seguimiento.'); this.procesandoDesvio.set(false); } });
  }

  protected marcarClienteRevisionConveniencia(): void {
    const cliente = this.clienteConvenienciaARevisar();
    if (!cliente) { return; }
    this.procesandoDesvio.set(true); this.error.set('');
    this.analiticaService.marcarClienteRevisionConveniencia(cliente.idCliente).subscribe({ next: mensaje => { this.mensajeConveniencia.set(mensaje); this.clienteConvenienciaARevisar.set(null); this.procesandoDesvio.set(false); this.consultarConveniencia(); }, error: error => { this.error.set(error?.error || 'No se pudo marcar el cliente para revisión.'); this.procesandoDesvio.set(false); } });
  }

  protected verProyectosClienteConveniencia(idCliente: number): void {
    this.filtroConveniencia.idCliente = idCliente;
    this.filtroConveniencia.idProyecto = null;
    this.consultarConveniencia();
  }

  protected consultarSobrecalificacion(): void {
    this.cargando.set(true); this.error.set('');
    this.analiticaService.consultarSobrecalificacion({ idProyecto: this.filtro.idProyecto ?? null }).subscribe({ next: resultado => { this.sobrecalificacion.set(resultado); this.detalleSobrecalificacion.set(null); this.cargando.set(false); }, error: error => { this.error.set(error?.error || 'No se pudo analizar la sobrecalificación de perfiles.'); this.cargando.set(false); } });
  }

  protected abrirDetalleSobrecalificacion(tarea: SobrecalificacionTarea): void { this.detalleSobrecalificacion.set(tarea); }
  protected cerrarDetalleSobrecalificacion(): void { this.detalleSobrecalificacion.set(null); }

  protected empleadosSobrecalificados(tareas: SobrecalificacionTarea[]): number {
    return new Set(tareas.map(tarea => tarea.idEmpleado)).size;
  }

  protected prepararSolicitudSobrecalificacion(tarea: SobrecalificacionTarea): void {
    this.observacionSobrecalificacion.set('');
    this.mensajeSobrecalificacion.set('');
    this.sobrecalificacionARevisar.set(tarea);
  }

  protected enviarSolicitudSobrecalificacion(): void {
    const tarea = this.sobrecalificacionARevisar();
    const observacion = this.observacionSobrecalificacion().trim();
    if (!tarea || !observacion) { this.error.set('Describí el criterio de asignación que debe revisar el PM.'); return; }
    this.procesandoDesvio.set(true); this.error.set('');
    this.analiticaService.solicitarRevisionSobrecalificacion({ idEmpleado: tarea.idEmpleado, idProyecto: tarea.idProyecto, idTarea: tarea.idTarea, observacion }).subscribe({
      next: mensaje => { this.mensajeSobrecalificacion.set(mensaje); this.sobrecalificacionARevisar.set(null); this.procesandoDesvio.set(false); },
      error: error => { this.error.set(error?.error || 'No fue posible enviar la solicitud de revisión.'); this.procesandoDesvio.set(false); }
    });
  }

  protected verDetalleRiesgo(riesgo: DetalleRiesgoProyecto): void { this.cerrandoDetalleRiesgo.set(false); this.riesgoSeleccionado.set(riesgo); }
  protected abrirDetalleTareaRiesgo(seleccion: RiesgoTareaSeleccionada): void { this.tareaRiesgoSeleccionada.set(seleccion); }
  protected cerrarDetalleTareaRiesgo(): void { this.tareaRiesgoSeleccionada.set(null); }
  protected cerrarDetalleRiesgo(): void {
    if (!this.riesgoSeleccionado() || this.cerrandoDetalleRiesgo()) { return; }
    this.cerrandoDetalleRiesgo.set(true);
    setTimeout(() => { this.riesgoSeleccionado.set(null); this.cerrandoDetalleRiesgo.set(false); }, 180);
  }

  protected abrirSeguimientoRiesgo(riesgo: DetalleRiesgoProyecto, tarea?: TareaRiesgoRetraso): void {
    this.error.set(''); this.mensajeRiesgo.set('');
    this.seguimientoRiesgo = { idProyecto: riesgo.idProyecto, idTarea: tarea?.idTarea ?? null, observacion: '', comunicarResponsable: true };
  }

  protected cerrarSeguimientoRiesgo(): void { this.seguimientoRiesgo = { idProyecto: 0, idTarea: null, observacion: '', comunicarResponsable: true }; }

  protected guardarSeguimientoRiesgo(): void {
    if (!this.seguimientoRiesgo.idProyecto || !this.seguimientoRiesgo.observacion.trim()) { this.error.set('Describí la acción de seguimiento antes de confirmarla.'); return; }
    this.procesandoDesvio.set(true); this.error.set('');
    this.riesgoService.registrarSeguimiento({ ...this.seguimientoRiesgo, observacion: this.seguimientoRiesgo.observacion.trim() }).subscribe({
      next: mensaje => { this.mensajeRiesgo.set(mensaje); this.cerrarSeguimientoRiesgo(); this.procesandoDesvio.set(false); },
      error: error => { this.error.set(error?.error || 'No se pudo registrar el seguimiento de riesgo.'); this.procesandoDesvio.set(false); }
    });
  }

  protected generarReporte(): void {
    this.generandoReporte.set(true); this.error.set('');
    this.configuracionReporte.filtro.idProyecto = this.filtro.idProyecto;
    this.reporteService.previsualizar(this.configuracionReporte).subscribe({ next: reporte => { this.reporte.set(reporte); this.generandoReporte.set(false); }, error: error => { this.error.set(error?.error || 'No se pudo generar el reporte ejecutivo.'); this.generandoReporte.set(false); } });
  }

  protected imprimirReporte(): void { window.print(); }
}

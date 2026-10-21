import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Cliente, ProjectsService, Proyecto } from '../../../../services/projects.service';
import { ConfiguracionReporteEjecutivo, ReporteEjecutivo, ReporteEjecutivoService } from '../../../../services/reporte-ejecutivo.service';
import { DetalleRiesgoProyecto, FiltroRiesgoRetraso, RiesgoRetrasoService } from '../../../../services/riesgo-retraso.service';
import { AccionAjusteDesvioHoras, AnalisisConvenienciaOperativa, AnalisisDesvioHoras, AnalisisOcupacionOperativa, AnalisisSobrecalificacionPerfiles, AnaliticaOperativaService, DesvioHorasProyecto, DesvioHorasTarea, DetalleDesvioHorasTarea, DetalleOcupacionEmpleado, DetalleOcupacionTarea, FiltroDesvioHoras, FiltroOcupacionOperativa, OcupacionEmpleado, OcupacionProyecto, SolicitudRevisionOperativa } from '../../../../services/analitica-operativa.service';
import { AuthService } from '../../../../services/auth.service';

type VistaAnalisis = 'riesgo' | 'ocupacion' | 'desvio' | 'conveniencia' | 'sobrecalificacion' | 'revisiones';
type EstadoVisualOcupacion = 'subutilizada' | 'balanceada' | 'sobrecargada' | 'critica';

@Component({ selector: 'app-reports', imports: [DatePipe, DecimalPipe, FormsModule], templateUrl: './reports.component.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class Reports {
  private readonly projectsService = inject(ProjectsService);
  private readonly riesgoService = inject(RiesgoRetrasoService);
  private readonly reporteService = inject(ReporteEjecutivoService);
  private readonly analiticaService = inject(AnaliticaOperativaService);
  private readonly authService = inject(AuthService);
  protected readonly proyectos = signal<Proyecto[]>([]);
  protected readonly clientes = signal<Cliente[]>([]);
  protected readonly riesgos = signal<DetalleRiesgoProyecto[]>([]);
  protected readonly riesgoSeleccionado = signal<DetalleRiesgoProyecto | null>(null);
  protected readonly ocupacion = signal<AnalisisOcupacionOperativa | null>(null);
  protected readonly detalleOcupacion = signal<DetalleOcupacionEmpleado | null>(null);
  protected readonly cerrandoDetalleOcupacion = signal(false);
  protected readonly situacionARevisar = signal<DetalleOcupacionEmpleado | null>(null);
  protected readonly tareaARevisar = signal<DetalleOcupacionTarea | null>(null);
  protected readonly solicitudesRevision = signal<SolicitudRevisionOperativa[]>([]);
  protected readonly desvioHoras = signal<AnalisisDesvioHoras | null>(null);
  protected readonly detalleDesvio = signal<DetalleDesvioHorasTarea | null>(null);
  protected readonly proyectoAComunicar = signal<DesvioHorasProyecto | null>(null);
  protected readonly conveniencia = signal<AnalisisConvenienciaOperativa | null>(null);
  protected readonly sobrecalificacion = signal<AnalisisSobrecalificacionPerfiles | null>(null);
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
  protected accionAjuste: AccionAjusteDesvioHoras = { idProyecto: 0, idTarea: null, observacion: '' };
  protected configuracionReporte: ConfiguracionReporteEjecutivo = {
    titulo: 'Reporte ejecutivo de gestión', filtro: { alcanceGeneral: true }, incluirOcupacionOperativa: true,
    incluirRiesgoRetraso: true, incluirDesvioHoras: true, incluirEficienciaPerfiles: true,
    incluirConvenienciaOperativa: true, incluirTodasLasSecciones: true,
  };

  constructor() {
    this.projectsService.consultar().subscribe({ next: proyectos => this.proyectos.set(proyectos.filter(proyecto => proyecto.activo)), error: () => this.error.set('No se pudieron cargar los proyectos para aplicar filtros.') });
    this.projectsService.opciones().subscribe({ next: opciones => this.clientes.set(opciones.clientes.filter(cliente => cliente.activo)), error: () => this.error.set('No se pudieron cargar los clientes para aplicar filtros.') });
    this.consultarRiesgos();
  }

  protected cambiarVista(vista: VistaAnalisis): void {
    this.vista.set(vista);
    this.error.set('');
    if (vista === 'riesgo' && this.riesgos().length === 0) { this.consultarRiesgos(); }
    if (vista === 'ocupacion' && !this.ocupacion()) { this.consultarOcupacion(); }
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
    this.riesgoService.consultar(this.filtro).subscribe({ next: resultado => { this.riesgos.set(resultado.proyectos); this.riesgoSeleccionado.set(null); this.cargando.set(false); }, error: error => { this.error.set(error?.error || 'No se pudo calcular el riesgo de retraso.'); this.cargando.set(false); } });
  }

  protected consultarOcupacion(): void {
    this.cargando.set(true); this.error.set('');
    this.analiticaService.consultarOcupacion(this.filtroOcupacion).subscribe({ next: resultado => { this.ocupacion.set(resultado); this.detalleOcupacion.set(null); this.cerrandoDetalleOcupacion.set(false); this.cargando.set(false); }, error: error => { this.error.set(error?.error || 'No se pudo analizar la ocupación operativa.'); this.cargando.set(false); } });
  }

  protected estadoVisualOcupacion(porcentaje: number): EstadoVisualOcupacion {
    if (porcentaje > 100) { return 'critica'; }
    if (porcentaje >= 86) { return 'sobrecargada'; }
    if (porcentaje >= 50) { return 'balanceada'; }
    return 'subutilizada';
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

  protected consultarDesvioHoras(): void {
    this.cargando.set(true); this.error.set('');
    this.analiticaService.consultarDesvioHoras(this.filtroDesvio).subscribe({ next: resultado => { this.desvioHoras.set(resultado); this.detalleDesvio.set(null); this.cargando.set(false); }, error: error => { this.error.set(error?.error || 'No se pudo analizar el desvío de horas.'); this.cargando.set(false); } });
  }

  protected abrirDetalleDesvio(proyecto: DesvioHorasProyecto, tarea: DesvioHorasTarea): void {
    this.error.set(''); this.mensajeDesvio.set('');
    this.analiticaService.consultarDetalleDesvioHorasTarea(proyecto.idProyecto, tarea.idTarea, this.filtroDesvio.desde, this.filtroDesvio.hasta).subscribe({ next: detalle => this.detalleDesvio.set(detalle), error: error => this.error.set(error?.error || 'No se pudo consultar el detalle del desvío.') });
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
    this.cargando.set(true); this.error.set('');
    this.analiticaService.consultarConveniencia().subscribe({ next: resultado => { this.conveniencia.set(resultado); this.cargando.set(false); }, error: error => { this.error.set(error?.error || 'No se pudo analizar la conveniencia operativa.'); this.cargando.set(false); } });
  }

  protected consultarSobrecalificacion(): void {
    this.cargando.set(true); this.error.set('');
    this.analiticaService.consultarSobrecalificacion().subscribe({ next: resultado => { this.sobrecalificacion.set(resultado); this.cargando.set(false); }, error: error => { this.error.set(error?.error || 'No se pudo analizar la sobrecalificación de perfiles.'); this.cargando.set(false); } });
  }

  protected verDetalleRiesgo(riesgo: DetalleRiesgoProyecto): void { this.riesgoSeleccionado.set(riesgo); }
  protected cerrarDetalleRiesgo(): void { this.riesgoSeleccionado.set(null); }

  protected generarReporte(): void {
    this.generandoReporte.set(true); this.error.set('');
    this.configuracionReporte.filtro.idProyecto = this.filtro.idProyecto;
    this.reporteService.previsualizar(this.configuracionReporte).subscribe({ next: reporte => { this.reporte.set(reporte); this.generandoReporte.set(false); }, error: error => { this.error.set(error?.error || 'No se pudo generar el reporte ejecutivo.'); this.generandoReporte.set(false); } });
  }

  protected imprimirReporte(): void { window.print(); }
}

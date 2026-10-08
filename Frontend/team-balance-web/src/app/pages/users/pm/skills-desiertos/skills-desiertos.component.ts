import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { forkJoin } from 'rxjs';
import { Cliente, Proyecto, ProjectsService } from '../../../../services/projects.service';
import { AnalisisCoberturaSkill, FiltroCoberturaSkill, SkillsDesiertosService, SugerenciaEmpleadoSkill } from '../../../../services/skills-desiertos.service';

type OrdenRanking = 'criticidad' | 'demanda' | 'cobertura';

@Component({
  selector: 'app-skills-desiertos',
  imports: [DatePipe, FormsModule, RouterModule],
  templateUrl: './skills-desiertos.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkillsDesiertosComponent {
  private readonly skillsService = inject(SkillsDesiertosService);
  private readonly projectsService = inject(ProjectsService);
  private readonly router = inject(Router);
  private cierrePendiente?: ReturnType<typeof setTimeout>;

  readonly proyectos = signal<Proyecto[]>([]);
  readonly clientes = signal<Cliente[]>([]);
  readonly resultados = signal<AnalisisCoberturaSkill[]>([]);
  readonly detalle = signal<AnalisisCoberturaSkill | null>(null);
  readonly cargando = signal(false);
  readonly cargandoDetalle = signal(false);
  readonly guardandoRecomendacion = signal(false);
  readonly error = signal('');
  readonly mensaje = signal('');
  readonly panelDetalleVisible = signal(false);
  readonly panelDetalleAbierto = signal(false);
  readonly sugerencias = signal<SugerenciaEmpleadoSkill[]>([]);
  readonly cargandoSugerencias = signal(false);
  readonly cerrandoPagina = signal(false);
  readonly orden = signal<OrdenRanking>('criticidad');
  readonly filtro: FiltroCoberturaSkill = { fechaDesde: this.fechaIso(0), fechaHasta: this.fechaIso(90), idProyecto: null, idCliente: null };
  tipoAccion = 'Contratación';
  observacion = '';
  proyectoSugerenciasId: number | null = null;

  constructor() {
    forkJoin({ proyectos: this.projectsService.consultar(), opciones: this.projectsService.opciones() }).subscribe({
      next: ({ proyectos, opciones }) => {
        this.proyectos.set(proyectos.filter((proyecto) => proyecto.activo));
        this.clientes.set(opciones.clientes.filter((cliente) => cliente.activo));
      },
      error: () => this.error.set('No se pudieron cargar los filtros de proyectos y clientes.'),
    });
    this.analizar();
  }

  analizar(): void {
    this.limpiarMensajes();
    this.cargando.set(true);
    this.skillsService.analizar(this.filtro).subscribe({
      next: (resultados) => { this.resultados.set(resultados); this.cargando.set(false); },
      error: (respuesta) => { this.cargando.set(false); this.error.set(this.obtenerMensajeError(respuesta, 'No se pudo analizar la cobertura de skills.')); },
    });
  }

  resultadosOrdenados(): AnalisisCoberturaSkill[] {
    const resultados = [...this.resultados()];
    if (this.orden() === 'demanda') { return resultados.sort((a, b) => b.demandaHoras - a.demandaHoras); }
    if (this.orden() === 'cobertura') { return resultados.sort((a, b) => a.porcentajeCobertura - b.porcentajeCobertura); }
    return resultados.sort((a, b) => b.puntajeCriticidad - a.puntajeCriticidad || a.demandaHoras - b.demandaHoras);
  }

  abrirDetalle(resultado: AnalisisCoberturaSkill): void {
    this.limpiarMensajes();
    this.cargandoDetalle.set(true);
    this.skillsService.detalle(resultado.idSkill, this.filtro).subscribe({
      next: (detalle) => {
        this.detalle.set(detalle);
        this.tipoAccion = detalle.accionSugerida;
        this.observacion = '';
        this.proyectoSugerenciasId = detalle.tareasAfectadas[0]?.idProyecto ?? null;
        this.sugerencias.set([]);
        this.cargandoDetalle.set(false);
        this.panelDetalleVisible.set(true);
        requestAnimationFrame(() => this.panelDetalleAbierto.set(true));
      },
      error: (respuesta) => { this.cargandoDetalle.set(false); this.error.set(this.obtenerMensajeError(respuesta, 'No se pudo abrir el detalle de la skill.')); },
    });
  }

  cerrarDetalle(): void {
    this.panelDetalleAbierto.set(false);
    window.setTimeout(() => { this.panelDetalleVisible.set(false); this.detalle.set(null); }, 180);
  }

  proyectosAfectados(detalle: AnalisisCoberturaSkill): { id: number; nombre: string }[] {
    return [...new Map(detalle.tareasAfectadas.map(tarea => [tarea.idProyecto, tarea.nombreProyecto])).entries()].map(([id, nombre]) => ({ id, nombre }));
  }

  sugerirEmpleados(): void {
    const detalle = this.detalle();
    if (!detalle || !this.proyectoSugerenciasId) { this.error.set('Seleccioná el proyecto que necesita cobertura.'); return; }
    this.error.set(''); this.cargandoSugerencias.set(true);
    this.skillsService.sugerirEmpleados({ idSkill: detalle.idSkill, idProyecto: this.proyectoSugerenciasId }).subscribe({
      next: sugerencias => { this.sugerencias.set(sugerencias); this.cargandoSugerencias.set(false); },
      error: respuesta => { this.cargandoSugerencias.set(false); this.error.set(this.obtenerMensajeError(respuesta, 'No se pudieron generar sugerencias de empleados.')); }
    });
  }

  guardarRecomendacion(): void {
    const detalle = this.detalle();
    if (!detalle || !this.observacion.trim()) {
      this.error.set('Escribí una breve observación antes de guardar la recomendación.');
      return;
    }
    this.limpiarMensajes();
    this.guardandoRecomendacion.set(true);
    this.skillsService.registrarRecomendacion({ idSkill: detalle.idSkill, tipoAccion: this.tipoAccion, observacion: this.observacion.trim() }).subscribe({
      next: () => { this.guardandoRecomendacion.set(false); this.mensaje.set('Recomendación registrada correctamente. No se modificaron tareas ni cargas reales.'); this.observacion = ''; },
      error: (respuesta) => { this.guardandoRecomendacion.set(false); this.error.set(this.obtenerMensajeError(respuesta, 'No se pudo registrar la recomendación.')); },
    });
  }

  volverAlPanel(): void {
    this.cerrandoPagina.set(true);
    this.cierrePendiente = window.setTimeout(() => this.router.navigateByUrl('/dashboard'), 180);
  }

  colorCriticidad(resultado: AnalisisCoberturaSkill): string {
    return resultado.criticidad === 'Crítica' ? 'bg-red-500' : resultado.criticidad === 'Alta' ? 'bg-orange-500' : resultado.criticidad === 'Media' ? 'bg-amber-400' : 'bg-emerald-500';
  }

  claseCriticidad(resultado: AnalisisCoberturaSkill): string {
    return resultado.criticidad === 'Crítica' ? 'tb-skill-tag tb-skill-tag--critical' : resultado.criticidad === 'Alta' ? 'tb-skill-tag tb-skill-tag--high' : resultado.criticidad === 'Media' ? 'tb-skill-tag tb-skill-tag--medium' : 'tb-skill-tag tb-skill-tag--low';
  }

  anchoDemanda(resultado: AnalisisCoberturaSkill): number {
    const maximo = Math.max(...this.resultados().map((item) => item.demandaHoras), 1);
    return Math.max(4, Math.round(resultado.demandaHoras * 100 / maximo));
  }

  resumenCritico(): number { return this.resultados().filter((resultado) => resultado.criticidad === 'Crítica' || resultado.criticidad === 'Alta').length; }
  totalDemanda(): number { return this.resultados().reduce((total, resultado) => total + resultado.demandaHoras, 0); }
  porcentajeCapacidad(resultado: AnalisisCoberturaSkill): number { return Math.min(100, Math.max(0, resultado.porcentajeCobertura)); }

  private limpiarMensajes(): void { this.error.set(''); this.mensaje.set(''); }
  private fechaIso(dias: number): string { const fecha = new Date(); fecha.setDate(fecha.getDate() + dias); return fecha.toISOString().slice(0, 10); }
  private obtenerMensajeError(respuesta: any, predeterminado: string): string { return typeof respuesta?.error === 'string' ? respuesta.error : predeterminado; }
}

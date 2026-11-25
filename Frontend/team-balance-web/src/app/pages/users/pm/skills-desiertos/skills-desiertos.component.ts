import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { afterNextRender, ChangeDetectionStrategy, Component, DestroyRef, ElementRef, inject, Injector, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { LocalizationService } from '../../../../services/localization.service';
import { Cliente, Proyecto, ProjectsService } from '../../../../services/projects.service';
import { AnalisisCoberturaSkill, claveClasificacionCobertura, FiltroCoberturaSkill, SkillsDesiertosService, SugerenciaEmpleadoSkill } from '../../../../services/skills-desiertos.service';
import { SkillsCoverageMatrixComponent } from '../../../../shared/components/analytics/skills-coverage-matrix/skills-coverage-matrix.component';

type OrdenRanking = 'criticidad' | 'demanda' | 'cobertura' | 'proyectos' | 'tipo';

const accionesCobertura = [
  { valor: 'Capacitación', clave: 'capacitacion' },
  { valor: 'Contratación', clave: 'hiring' },
  { valor: 'Revisión de asignaciones', clave: 'revisionDeAsignaciones' },
];

@Component({
  selector: 'app-skills-desiertos',
  imports: [DatePipe, FormsModule, RouterModule, SkillsCoverageMatrixComponent],
  templateUrl: './skills-desiertos.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkillsDesiertosComponent {
  private readonly skillsService = inject(SkillsDesiertosService);
  private readonly projectsService = inject(ProjectsService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);
  private readonly panelDetalle = viewChild.required<ElementRef<HTMLDialogElement>>('panelDetalle');
  readonly localization = inject(LocalizationService);
  readonly acciones = accionesCobertura;
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
  readonly sugerencias = signal<SugerenciaEmpleadoSkill[]>([]);
  readonly cargandoSugerencias = signal(false);
  readonly cerrandoPagina = signal(false);
  readonly orden = signal<OrdenRanking>('criticidad');
  readonly areaSeleccionada = signal('');
  readonly filtro: FiltroCoberturaSkill = { fechaDesde: this.fechaIso(0), fechaHasta: this.fechaIso(90), idProyecto: null, idCliente: null };
  tipoAccion = '';
  observacion = '';
  proyectoSugerenciasId: number | null = null;

  constructor() {
    forkJoin({ proyectos: this.projectsService.consultar(), opciones: this.projectsService.opciones() }).subscribe({
      next: ({ proyectos, opciones }) => {
        this.proyectos.set(proyectos.filter((proyecto) => proyecto.activo));
        this.clientes.set(opciones.clientes.filter((cliente) => cliente.activo));
      },
      error: respuesta => this.error.set(this.obtenerMensajeError(respuesta, 'filtersError')),
    });
    this.analizar();
  }

  analizar(): void {
    if (this.cargando()) return;
    this.limpiarMensajes();
    this.cargando.set(true);
    this.skillsService.analizar(this.filtro)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.cargando.set(false)))
      .subscribe({
        next: (resultados) => this.resultados.set(resultados),
        error: respuesta => this.error.set(this.obtenerMensajeError(respuesta, 'analysisError')),
      });
  }

  resultadosOrdenados(): AnalisisCoberturaSkill[] {
    const resultados = this.resultados().filter(resultado => !this.areaSeleccionada() || (resultado.categoriaSkill || 'Sin categoría') === this.areaSeleccionada());
    if (this.orden() === 'demanda') { return resultados.sort((a, b) => b.demandaHoras - a.demandaHoras); }
    if (this.orden() === 'cobertura') { return resultados.sort((a, b) => a.porcentajeCobertura - b.porcentajeCobertura); }
    if (this.orden() === 'proyectos') { return resultados.sort((a, b) => b.cantidadProyectosAfectados - a.cantidadProyectosAfectados || b.puntajeCriticidad - a.puntajeCriticidad); }
    if (this.orden() === 'tipo') { return resultados.sort((a, b) => a.tipoBrecha.localeCompare(b.tipoBrecha) || b.puntajeCriticidad - a.puntajeCriticidad); }
    return resultados.sort((a, b) => b.puntajeCriticidad - a.puntajeCriticidad || a.demandaHoras - b.demandaHoras);
  }

  areasDisponibles(): string[] { return [...new Set(this.resultados().map(resultado => resultado.categoriaSkill || 'Sin categoría'))].sort(); }

  abrirDetalle(resultado: AnalisisCoberturaSkill): void {
    this.limpiarMensajes();
    this.cargandoDetalle.set(true);
    this.skillsService.detalle(resultado.idSkill, this.filtro).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (detalle) => {
        this.detalle.set(detalle);
        this.tipoAccion = this.acciones.find(accion => accion.valor === detalle.accionSugerida)?.valor ?? '';
        this.observacion = '';
        this.proyectoSugerenciasId = detalle.tareasAfectadas[0]?.idProyecto ?? null;
        this.sugerencias.set([]);
        this.cargandoDetalle.set(false);
        afterNextRender(() => {
          const panel = this.panelDetalle().nativeElement;
          if (this.detalle() && !panel.open) panel.showModal();
        }, { injector: this.injector });
      },
      error: respuesta => { this.cargandoDetalle.set(false); this.error.set(this.obtenerMensajeError(respuesta, 'detailError')); },
    });
  }

  cerrarDetalle(): void {
    this.panelDetalle().nativeElement.close();
    this.detalle.set(null);
    this.limpiarMensajes();
  }

  cerrarDesdeFondo(evento: MouseEvent): void {
    const panel = this.panelDetalle().nativeElement;
    if (evento.target !== panel) return;
    const { left, right, top, bottom } = panel.getBoundingClientRect();
    if (evento.clientX < left || evento.clientX >= right || evento.clientY < top || evento.clientY >= bottom) {
      this.cerrarDetalle();
    }
  }

  proyectosAfectados(detalle: AnalisisCoberturaSkill): { id: number; nombre: string }[] {
    return [...new Map(detalle.tareasAfectadas.map(tarea => [tarea.idProyecto, tarea.nombreProyecto])).entries()].map(([id, nombre]) => ({ id, nombre }));
  }

  destacarCapacitacion(candidato: SugerenciaEmpleadoSkill, cobertura: number): boolean {
    return cobertura < 100 && !candidato.tieneSkillObjetivo && candidato.cantidadSkillsMismaArea > 0
      && candidato.horasSemanales > 0 && candidato.cargaActual >= 0
      && candidato.cargaActual * 10 < candidato.horasSemanales * 3;
  }

  sugerirEmpleados(): void {
    const detalle = this.detalle();
    if (!detalle || !this.proyectoSugerenciasId) { this.error.set('private.skillsCoverage.projectRequired'); return; }
    this.limpiarMensajes(); this.cargandoSugerencias.set(true);
    this.skillsService.sugerirEmpleados({ idSkill: detalle.idSkill, idProyecto: this.proyectoSugerenciasId }).subscribe({
      next: sugerencias => { this.sugerencias.set(sugerencias); this.cargandoSugerencias.set(false); },
      error: respuesta => { this.cargandoSugerencias.set(false); this.error.set(this.obtenerMensajeError(respuesta, 'suggestError')); }
    });
  }

  guardarRecomendacion(): void {
    const detalle = this.detalle();
    this.limpiarMensajes();
    if (!detalle || !this.acciones.some(accion => accion.valor === this.tipoAccion)) {
      this.error.set('private.skillsCoverage.actionRequired');
      return;
    }
    if (!this.observacion.trim()) {
      this.error.set('private.skillsCoverage.noteRequired');
      return;
    }
    this.guardandoRecomendacion.set(true);
    this.skillsService.registrarRecomendacion({ idSkill: detalle.idSkill, tipoAccion: this.tipoAccion, observacion: this.observacion.trim() }).subscribe({
      next: () => { this.guardandoRecomendacion.set(false); this.mensaje.set('private.skillsCoverage.saved'); this.observacion = ''; },
      error: respuesta => { this.guardandoRecomendacion.set(false); this.error.set(this.obtenerMensajeError(respuesta, 'saveError')); },
    });
  }

  volverAlPanel(): void {
    this.cerrandoPagina.set(true);
    this.cierrePendiente = globalThis.setTimeout(() => this.router.navigateByUrl('/dashboard'), 180);
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

  resumenCritico(): number { return this.resultadosOrdenados().filter((resultado) => resultado.criticidad === 'Crítica' || resultado.criticidad === 'Alta').length; }
  totalDemanda(): number { return this.resultadosOrdenados().reduce((total, resultado) => total + resultado.demandaHoras, 0); }
  porcentajeCapacidad(resultado: AnalisisCoberturaSkill): number { return Math.min(100, Math.max(0, resultado.porcentajeCobertura)); }

  texto(clave: string): string { return this.localization.traducir(`private.skillsCoverage.${clave}`); }
  clasificacion(valor: string): string { return this.texto(claveClasificacionCobertura(valor)); }

  categoria(valor?: string): string {
    if (!valor || valor === 'Sin categoría') return this.texto('uncategorized');
    const claves: Record<string, string> = {
      'programación': 'programming', 'diseño': 'design', 'marketing y contenido': 'marketing',
      audiovisual: 'audiovisual', 'gestión de proyectos': 'projectManagement',
      'datos y analítica': 'dataAnalytics', operaciones: 'operations', 'recursos humanos': 'humanResources',
      general: 'general'
    };
    const clave = claves[valor.trim().normalize('NFC').toLowerCase()];
    return clave ? this.texto(clave) : valor;
  }

  nivel(valor: string): string {
    const claves: Record<string, string> = {
      'básico': 'levelBasic', basico: 'levelBasic', medio: 'levelIntermediate',
      intermedio: 'levelIntermediate', avanzado: 'levelAdvanced'
    };
    const clave = claves[valor?.trim().toLowerCase() ?? ''];
    return clave ? this.localization.traducir(`private.impactSimulation.${clave}`) : valor;
  }

  seniority(valor: string): string {
    return (valor || '').toLowerCase() === 'semisenior' ? this.localization.traducir('taskTemplates.seniorityMiddle') : valor;
  }

  accion(valor: string): string {
    return this.texto(this.acciones.find(accion => accion.valor === valor)?.clave ?? 'unclassified');
  }

  motivoCandidato(candidato: SugerenciaEmpleadoSkill): string {
    const afinidad = candidato.tieneSkillObjetivo ? this.texto('hasRequiredSkill')
      : this.texto('relatedSkills').replace('{count}', String(candidato.cantidadSkillsMismaArea))
          .replace('{skills}', () => candidato.skillsRelacionadas);
    const horas = this.localization.formatearNumero(Math.round(Math.max(0, candidato.horasDisponibles) * 10) / 10);
    return `${afinidad} · ${this.texto('availableHours').replace('{hours}', horas)}`;
  }

  private limpiarMensajes(): void { this.error.set(''); this.mensaje.set(''); }
  private fechaIso(dias: number): string { const fecha = new Date(); fecha.setDate(fecha.getDate() + dias); return fecha.toISOString().slice(0, 10); }
  private obtenerMensajeError(respuesta: HttpErrorResponse, predeterminado: string): string {
    const detalle = typeof respuesta.error === 'string' ? respuesta.error : respuesta.error?.detail ?? '';
    if (respuesta.status === 0) return 'projectForm.connection';
    if (respuesta.status === 401 && /sesi[oó]n/i.test(detalle)) return 'projectForm.invalidSession';
    if (respuesta.status === 401 || respuesta.status === 403) return 'projectForm.notAllowed';
    if (/fecha final no puede/i.test(detalle)) return 'private.skillsCoverage.invalidRange';
    if (/acci[oó]n seleccionada no es v[aá]lida/i.test(detalle)) return 'private.skillsCoverage.actionRequired';
    if (/no tiene demanda activa/i.test(detalle)) return 'private.skillsCoverage.noProjectDemand';
    if (/no pertenece al alcance/i.test(detalle)) return 'private.skillsCoverage.skillOutsideScope';
    return `private.skillsCoverage.${predeterminado}`;
  }
}

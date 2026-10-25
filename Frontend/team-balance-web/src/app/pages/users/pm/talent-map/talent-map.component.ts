import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, OnDestroy, ViewChild, computed, effect, inject, signal } from '@angular/core';
import * as echarts from 'echarts';
import { forkJoin } from 'rxjs';
import { AnaliticaOperativaService, OcupacionEmpleado } from '../../../../services/analitica-operativa.service';
import { Tarea, TareaOpciones, TasksService } from '../../../../services/tasks.service';

interface NodoTalento {
  id: number;
  nombre: string;
  seniority: string;
  skillPrincipal: string;
  skills: string[];
  proyectos: string[];
  relaciones: number;
}

interface EnlaceTalento { source: number; target: number; proyectos: string[]; }

@Component({
  selector: 'app-talent-map',
  standalone: true,
  templateUrl: './talent-map.component.html',
  styleUrl: './talent-map.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TalentMapComponent implements AfterViewInit, OnDestroy {
  private readonly tasksService = inject(TasksService);
  private readonly analytics = inject(AnaliticaOperativaService);
  @ViewChild('chart') private readonly chartElement?: ElementRef<HTMLDivElement>;

  readonly cargando = signal(true);
  readonly error = signal('');
  readonly tareas = signal<Tarea[]>([]);
  readonly opciones = signal<TareaOpciones>({ proyectos: [], estados: [], empleados: [], skills: [] });
  readonly ocupacion = signal<OcupacionEmpleado[]>([]);
  readonly skillFiltro = signal(0);
  readonly proyectoFiltro = signal(0);
  readonly seniorityFiltro = signal('');
  readonly empleadoSeleccionado = signal<number | null>(null);
  readonly proyectosVisibles = computed(() => {
    const ids = new Set(this.tareas().map(tarea => tarea.idProyecto));
    return this.opciones().proyectos.filter(proyecto => ids.has(proyecto.id));
  });
  readonly nodos = computed(() => this.construirRed().nodos);
  readonly enlaces = computed(() => this.construirRed().enlaces);
  readonly nodoSeleccionado = computed(() => this.nodos().find(nodo => nodo.id === this.empleadoSeleccionado()) ?? null);
  readonly totalColaboraciones = computed(() => this.enlaces().reduce((total, enlace) => total + enlace.proyectos.length, 0));

  private chart?: echarts.ECharts;
  private resizeObserver?: ResizeObserver;

  constructor() {
    effect(() => { this.skillFiltro(); this.proyectoFiltro(); this.seniorityFiltro(); this.empleadoSeleccionado(); this.tareas(); this.ocupacion(); queueMicrotask(() => this.dibujar()); });
    forkJoin({ tareas: this.tasksService.consultarPorPM(), opciones: this.tasksService.opciones(), ocupacion: this.analytics.consultarOcupacion() }).subscribe({
      next: ({ tareas, opciones, ocupacion }) => { this.tareas.set(tareas); this.opciones.set(opciones); this.ocupacion.set(ocupacion.empleados); this.cargando.set(false); },
      error: respuesta => { this.cargando.set(false); this.error.set(this.mensajeError(respuesta, 'No fue posible construir el mapa con tus proyectos autorizados.')); }
    });
  }

  ngAfterViewInit(): void {
    if (!this.chartElement) { return; }
    this.chart = echarts.init(this.chartElement.nativeElement, undefined, { renderer: 'canvas' });
    this.chart.on('click', evento => {
      const data = evento.data as { employeeId?: number };
      if (evento.dataType === 'node' && data.employeeId) { this.empleadoSeleccionado.set(this.empleadoSeleccionado() === data.employeeId ? null : data.employeeId); }
    });
    this.resizeObserver = new ResizeObserver(() => this.chart?.resize());
    this.resizeObserver.observe(this.chartElement.nativeElement);
    this.dibujar();
  }

  ngOnDestroy(): void { this.resizeObserver?.disconnect(); this.chart?.dispose(); }

  limpiarFiltros(): void { this.skillFiltro.set(0); this.proyectoFiltro.set(0); this.seniorityFiltro.set(''); this.empleadoSeleccionado.set(null); }

  private construirRed(): { nodos: NodoTalento[]; enlaces: EnlaceTalento[] } {
    const opciones = this.opciones();
    const tareas = this.tareas().filter(tarea => this.dentroDelTrimestre(tarea));
    const empleados = new Map(opciones.empleados.map(empleado => [empleado.id, empleado]));
    const ocupacion = new Map(this.ocupacion().map(empleado => [empleado.idEmpleado, empleado]));
    const skills = new Map(opciones.skills.map(skill => [skill.id, skill]));
    const proyectos = new Map(opciones.proyectos.map(proyecto => [proyecto.id, proyecto]));
    const empleadosPorProyecto = new Map<number, Set<number>>();
    const skillsPorEmpleado = new Map<number, Map<number, number>>();

    for (const tarea of tareas) {
      const ids = this.empleadosTarea(tarea);
      for (const id of ids) {
        if (!empleados.has(id) && !ocupacion.has(id)) { continue; }
        if (!empleadosPorProyecto.has(tarea.idProyecto)) { empleadosPorProyecto.set(tarea.idProyecto, new Set()); }
        empleadosPorProyecto.get(tarea.idProyecto)!.add(id);
        if (tarea.idSkillRequerido) {
          if (!skillsPorEmpleado.has(id)) { skillsPorEmpleado.set(id, new Map()); }
          const conteos = skillsPorEmpleado.get(id)!;
          conteos.set(tarea.idSkillRequerido, (conteos.get(tarea.idSkillRequerido) ?? 0) + 1);
        }
      }
    }

    const idsPermitidos = new Set<number>();
    for (const [idProyecto, ids] of empleadosPorProyecto) {
      if (this.proyectoFiltro() && idProyecto !== this.proyectoFiltro()) { continue; }
      for (const id of ids) {
        const seniority = ocupacion.get(id)?.seniority ?? '';
        const tieneSkill = !this.skillFiltro() || (skillsPorEmpleado.get(id)?.has(this.skillFiltro()) ?? false);
        if ((!this.seniorityFiltro() || seniority === this.seniorityFiltro()) && tieneSkill) { idsPermitidos.add(id); }
      }
    }

    const enlacesMap = new Map<string, EnlaceTalento>();
    for (const [idProyecto, idsProyecto] of empleadosPorProyecto) {
      if (this.proyectoFiltro() && idProyecto !== this.proyectoFiltro()) { continue; }
      const ids = [...idsProyecto].filter(id => idsPermitidos.has(id)).sort((a, b) => a - b);
      for (let i = 0; i < ids.length; i++) {
        for (let j = i + 1; j < ids.length; j++) {
          const clave = `${ids[i]}-${ids[j]}`;
          const proyecto = proyectos.get(idProyecto)?.nombre ?? `Proyecto #${idProyecto}`;
          const enlace = enlacesMap.get(clave) ?? { source: ids[i], target: ids[j], proyectos: [] };
          if (!enlace.proyectos.includes(proyecto)) { enlace.proyectos.push(proyecto); }
          enlacesMap.set(clave, enlace);
        }
      }
    }
    const enlaces = [...enlacesMap.values()];
    const nodos = [...idsPermitidos].map(id => {
      const usuario = empleados.get(id); const empleado = ocupacion.get(id);
      const conteos = [...(skillsPorEmpleado.get(id)?.entries() ?? [])].sort((a, b) => b[1] - a[1]);
      const nombresSkills = conteos.map(([idSkill]) => skills.get(idSkill)?.nombre ?? `Skill #${idSkill}`);
      const nombresProyectos = [...empleadosPorProyecto.entries()].filter(([, ids]) => ids.has(id)).map(([idProyecto]) => proyectos.get(idProyecto)?.nombre ?? `Proyecto #${idProyecto}`);
      return {
        id,
        nombre: empleado?.nombre || `${usuario?.nombre ?? ''} ${usuario?.apellido ?? ''}`.trim() || `Empleado #${id}`,
        seniority: empleado?.seniority || 'Sin definir',
        skillPrincipal: nombresSkills[0] ?? 'Sin skill observada',
        skills: nombresSkills,
        proyectos: nombresProyectos,
        relaciones: enlaces.filter(enlace => enlace.source === id || enlace.target === id).length,
      };
    });
    return { nodos, enlaces };
  }

  private dibujar(): void {
    if (!this.chart) { return; }
    const nodos = this.nodos(); const enlaces = this.enlaces(); const seleccionado = this.empleadoSeleccionado();
    const relacionados = new Set<number>();
    if (seleccionado) { relacionados.add(seleccionado); enlaces.filter(enlace => enlace.source === seleccionado || enlace.target === seleccionado).forEach(enlace => { relacionados.add(enlace.source); relacionados.add(enlace.target); }); }
    const paleta = ['#39d7b0', '#55b9ff', '#f4b844', '#a88cff', '#ff7088', '#31cfdf'];
    const categorias = [...new Set(nodos.map(nodo => nodo.skillPrincipal))];
    this.chart.setOption({
      animationDuration: 850, animationEasingUpdate: 'cubicInOut',
      tooltip: { backgroundColor: '#061a2c', borderColor: '#2c9fbd', textStyle: { color: '#effdff' }, formatter: (parametro: unknown) => {
        const dato = parametro as { dataType: string; data: { employeeId?: number; sharedProjects?: string[] } };
        if (dato.dataType === 'edge') { return `<strong>Colaboración observada</strong><br/>${dato.data.sharedProjects?.join('<br/>') || 'Proyecto compartido'}`; }
        const nodo = nodos.find(item => item.id === dato.data.employeeId); return nodo ? `<strong>${nodo.nombre}</strong><br/>${nodo.seniority}<br/>Skill destacada: ${nodo.skillPrincipal}<br/>${nodo.proyectos.length} proyecto(s) del alcance` : '';
      } },
      legend: { type: 'scroll', bottom: 6, textStyle: { color: '#a9ced6' }, data: categorias },
      series: [{
        type: 'graph', layout: 'circular', circular: { rotateLabel: false }, roam: true, draggable: true,
        emphasis: { focus: 'adjacency', lineStyle: { width: 6 } },
        label: { show: true, position: 'bottom', color: '#eafdff', fontWeight: 700, formatter: '{b}' },
        categories: categorias.map((nombre, indice) => ({ name: nombre, itemStyle: { color: paleta[indice % paleta.length] } })),
        data: nodos.map(nodo => ({
          id: String(nodo.id), employeeId: nodo.id, name: nodo.nombre, category: categorias.indexOf(nodo.skillPrincipal),
          symbolSize: 43 + Math.min(nodo.relaciones * 5, 22),
          itemStyle: { opacity: seleccionado && !relacionados.has(nodo.id) ? .18 : 1, borderColor: seleccionado === nodo.id ? '#ffffff' : '#aefbff', borderWidth: seleccionado === nodo.id ? 5 : 2, shadowBlur: seleccionado === nodo.id ? 28 : 14, shadowColor: paleta[Math.max(0, categorias.indexOf(nodo.skillPrincipal)) % paleta.length] },
          label: { opacity: seleccionado && !relacionados.has(nodo.id) ? .22 : 1 }
        })),
        links: enlaces.map(enlace => ({ source: String(enlace.source), target: String(enlace.target), sharedProjects: enlace.proyectos, lineStyle: { width: 1.5 + Math.min(enlace.proyectos.length * 1.7, 7), color: '#55c9d7', opacity: seleccionado && enlace.source !== seleccionado && enlace.target !== seleccionado ? .08 : .58, curveness: .08 } }))
      }]
    }, true);
  }

  private empleadosTarea(tarea: Tarea): number[] {
    const ids = new Set<number>();
    if (tarea.idEmpleadoAsignado) { ids.add(tarea.idEmpleadoAsignado); }
    tarea.asignaciones?.filter(asignacion => asignacion.activa !== false).forEach(asignacion => ids.add(asignacion.idEmpleado));
    return [...ids];
  }

  private dentroDelTrimestre(tarea: Tarea): boolean {
    const desde = new Date(); desde.setMonth(desde.getMonth() - 3); desde.setHours(0, 0, 0, 0);
    const inicio = tarea.fechaInicio ? new Date(tarea.fechaInicio) : null;
    const fin = tarea.fechaFinReal ? new Date(tarea.fechaFinReal) : tarea.deadline ? new Date(tarea.deadline) : null;
    return (!fin || fin >= desde) && (!inicio || inicio <= new Date());
  }

  private mensajeError(respuesta: unknown, predeterminado: string): string { const error = (respuesta as { error?: unknown })?.error; return typeof error === 'string' && error.trim() ? error : predeterminado; }
}

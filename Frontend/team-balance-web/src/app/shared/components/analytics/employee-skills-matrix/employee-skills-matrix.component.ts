import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, Input, OnChanges, OnDestroy, SimpleChanges, ViewChild, computed, inject, signal } from '@angular/core';
import { catchError, forkJoin, map, of } from 'rxjs';
import * as echarts from 'echarts';
import { FichaEmpleado, ResourcesService, Skill } from '../../../../services/resources.service';
import { UsuarioGestion } from '../../../../services/usuarios.service';

interface FilaMatriz {
  usuario: UsuarioGestion;
  ficha: FichaEmpleado;
}

@Component({
  selector: 'app-employee-skills-matrix',
  standalone: true,
  templateUrl: './employee-skills-matrix.component.html',
  styleUrl: './employee-skills-matrix.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmployeeSkillsMatrixComponent implements OnChanges, AfterViewInit, OnDestroy {
  private readonly resources = inject(ResourcesService);
  @Input({ required: true }) usuarios: UsuarioGestion[] = [];
  @Input({ required: true }) skills: Skill[] = [];
  @ViewChild('radar') private readonly radarElement?: ElementRef<HTMLDivElement>;

  readonly filas = signal<FilaMatriz[]>([]);
  readonly cargando = signal(false);
  readonly guardando = signal('');
  readonly error = signal('');
  readonly mensaje = signal('');
  readonly busqueda = signal('');
  readonly skillFiltro = signal(0);
  readonly seniorityFiltro = signal('');
  readonly empleadoSeleccionado = signal<number | null>(null);
  readonly niveles = ['', 'Básico', 'Medio', 'Avanzado'];
  readonly filasVisibles = computed(() => {
    const texto = this.busqueda().trim().toLocaleLowerCase();
    const skill = this.skillFiltro();
    const seniority = this.seniorityFiltro();
    return this.filas().filter(fila => {
      const nombre = `${fila.usuario.nombre} ${fila.usuario.apellido}`.toLocaleLowerCase();
      return (!texto || nombre.includes(texto))
        && (!seniority || fila.usuario.empleado?.seniority === seniority)
        && (!skill || fila.ficha.empleadoSkills.some(item => item.idSkill === skill && item.activo !== false));
    });
  });
  readonly filaSeleccionada = computed(() => this.filas().find(fila => fila.usuario.id === this.empleadoSeleccionado()) ?? null);

  private radar?: echarts.ECharts;
  private resizeObserver?: ResizeObserver;
  private firmaCarga = '';

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['usuarios'] && !changes['skills']) { return; }
    const empleados = this.usuarios.filter(usuario => !!usuario.empleado && usuario.activo);
    const firma = empleados.map(usuario => usuario.id).sort((a, b) => a - b).join(',');
    if (firma === this.firmaCarga || !empleados.length) { return; }
    this.firmaCarga = firma;
    this.cargarFichas(empleados);
  }

  ngAfterViewInit(): void {
    if (!this.radarElement) { return; }
    this.radar = echarts.init(this.radarElement.nativeElement, undefined, { renderer: 'canvas' });
    this.resizeObserver = new ResizeObserver(() => this.radar?.resize());
    this.resizeObserver.observe(this.radarElement.nativeElement);
    this.dibujarRadar();
  }

  ngOnDestroy(): void { this.resizeObserver?.disconnect(); this.radar?.dispose(); }

  nivel(fila: FilaMatriz, idSkill: number): string {
    const valor = fila.ficha.empleadoSkills.find(item => item.idSkill === idSkill && item.activo !== false)?.nivel;
    return this.normalizarNivel(valor, '');
  }

  claseNivel(nivel: string): string { return nivel ? `is-${nivel.toLocaleLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '')}` : 'is-empty'; }

  seleccionarEmpleado(idUsuario: number): void {
    this.empleadoSeleccionado.set(this.empleadoSeleccionado() === idUsuario ? null : idUsuario);
    queueMicrotask(() => this.dibujarRadar());
  }

  cambiarNivel(fila: FilaMatriz, idSkill: number, nivel: string): void {
    const clave = `${fila.usuario.id}-${idSkill}`;
    if (this.guardando()) { return; }
    const skills = fila.ficha.empleadoSkills.filter(item => item.idSkill !== idSkill);
    if (nivel) { skills.push({ idSkill, nivel, activo: true }); }
    if (!skills.length) { this.error.set('Cada empleado debe conservar al menos una skill registrada.'); return; }
    const actualizada: FichaEmpleado = { ...fila.ficha, id: fila.usuario.id, empleadoSkills: skills };
    this.guardando.set(clave); this.error.set(''); this.mensaje.set('');
    this.resources.guardarFichaEmpleado(actualizada).subscribe({
      next: () => {
        this.filas.update(filas => filas.map(item => item.usuario.id === fila.usuario.id ? { ...item, ficha: actualizada } : item));
        this.guardando.set(''); this.mensaje.set(`Se actualizó ${this.nombreSkill(idSkill)} para ${fila.usuario.nombre}.`);
        this.dibujarRadar();
      },
      error: respuesta => { this.guardando.set(''); this.error.set(this.mensajeError(respuesta, 'No se pudo actualizar el nivel de la skill.')); }
    });
  }

  nombreSkill(idSkill: number): string { return this.skills.find(skill => skill.id === idSkill)?.nombre ?? `Skill #${idSkill}`; }

  limpiarFiltros(): void { this.busqueda.set(''); this.skillFiltro.set(0); this.seniorityFiltro.set(''); }

  private cargarFichas(empleados: UsuarioGestion[]): void {
    this.cargando.set(true); this.error.set('');
    forkJoin(empleados.map(usuario => this.resources.consultarFichaEmpleado(usuario.id).pipe(
      map(ficha => ({ usuario, ficha } as FilaMatriz)),
      catchError(() => of(null))
    ))).subscribe(resultados => {
      const filas = resultados.filter((resultado): resultado is FilaMatriz => resultado !== null);
      this.filas.set(filas); this.cargando.set(false);
      const omitidos = empleados.length - filas.length;
      if (omitidos) { this.error.set(`No se pudieron cargar ${omitidos} ficha(s) de empleado.`); }
      this.normalizarNivelesHeredados(filas);
      if (!this.empleadoSeleccionado() && filas.length) { this.empleadoSeleccionado.set(filas[0].usuario.id); }
      queueMicrotask(() => this.dibujarRadar());
    });
  }

  private normalizarNivelesHeredados(filas: FilaMatriz[]): void {
    const heredadas = filas.filter(fila => fila.ficha.empleadoSkills.some(skill => !this.normalizarNivel(skill.nivel, '')));
    if (!heredadas.length) { return; }
    forkJoin(heredadas.map(fila => {
      const ficha: FichaEmpleado = {
        ...fila.ficha,
        id: fila.usuario.id,
        empleadoSkills: fila.ficha.empleadoSkills.map(skill => ({ ...skill, nivel: this.normalizarNivel(skill.nivel, 'Medio') }))
      };
      return this.resources.guardarFichaEmpleado(ficha).pipe(map(() => ({ id: fila.usuario.id, ficha })), catchError(() => of(null)));
    })).subscribe(resultados => {
      const actualizadas = resultados.filter((item): item is { id: number; ficha: FichaEmpleado } => item !== null);
      if (actualizadas.length) {
        this.filas.update(filasActuales => filasActuales.map(fila => actualizadas.find(item => item.id === fila.usuario.id)?.ficha ? { ...fila, ficha: actualizadas.find(item => item.id === fila.usuario.id)!.ficha } : fila));
        this.mensaje.set(`Se asignó nivel Medio a ${actualizadas.length} ficha(s) heredada(s) que tenían skills sin nivel.`);
        this.dibujarRadar();
      }
    });
  }

  private dibujarRadar(): void {
    if (!this.radar) { return; }
    const fila = this.filaSeleccionada();
    const principales = fila?.ficha.empleadoSkills.filter(item => item.activo !== false).slice(0, 8) ?? [];
    if (!fila || principales.length < 2) { this.radar.clear(); return; }
    const valorNivel = (nivel?: string) => ({ 'Básico': 1, 'Medio': 2, 'Avanzado': 3 }[this.normalizarNivel(nivel, 'Medio')] ?? 2);
    this.radar.setOption({
      animationDuration: 650,
      tooltip: { trigger: 'item', backgroundColor: '#061a2c', borderColor: '#29c9ce', textStyle: { color: '#effdff' } },
      radar: {
        radius: '66%', splitNumber: 3,
        indicator: principales.map(item => ({ name: this.nombreSkill(item.idSkill), max: 3 })),
        axisName: { color: '#b8e8ee', fontSize: 11 },
        axisLine: { lineStyle: { color: 'rgba(66,190,199,.28)' } },
        splitLine: { lineStyle: { color: 'rgba(66,190,199,.22)' } },
        splitArea: { areaStyle: { color: ['rgba(4,35,55,.55)', 'rgba(9,58,75,.42)'] } }
      },
      series: [{ type: 'radar', symbol: 'circle', symbolSize: 7, lineStyle: { width: 3, color: '#44e0c1' }, itemStyle: { color: '#f5bf4f' }, areaStyle: { color: 'rgba(68,224,193,.24)' }, data: [{ name: `${fila.usuario.nombre} ${fila.usuario.apellido}`, value: principales.map(item => valorNivel(item.nivel)) }] }]
    }, true);
  }

  private normalizarNivel(nivel?: string, predeterminado = 'Medio'): string {
    const valor = (nivel ?? '').trim().toLocaleLowerCase();
    if (!valor) { return predeterminado; }
    if (['básico', 'basico', 'bajo', 'junior'].includes(valor)) { return 'Básico'; }
    if (['avanzado', 'alto', 'experto'].includes(valor)) { return 'Avanzado'; }
    return 'Medio';
  }

  private mensajeError(respuesta: unknown, predeterminado: string): string {
    const http = respuesta as { error?: unknown };
    return typeof http.error === 'string' && http.error.trim() ? http.error : predeterminado;
  }
}

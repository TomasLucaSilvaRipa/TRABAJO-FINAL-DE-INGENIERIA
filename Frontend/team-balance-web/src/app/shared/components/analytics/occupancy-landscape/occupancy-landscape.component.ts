import { AfterViewInit, ChangeDetectorRef, Component, effect, ElementRef, EventEmitter, Input, NgZone, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild, inject } from '@angular/core';
import * as echarts from 'echarts/core';
import { LineChart, ScatterChart } from 'echarts/charts';
import { AriaComponent, DataZoomComponent, GridComponent, MarkAreaComponent, MarkLineComponent, TooltipComponent } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import { OcupacionEmpleado, OcupacionEmpleadoProyecto, OcupacionProyecto } from '../../../../services/analitica-operativa.service';
import { LocalizationService } from '../../../../services/localization.service';

echarts.use([LineChart, ScatterChart, AriaComponent, DataZoomComponent, GridComponent, MarkAreaComponent, MarkLineComponent, TooltipComponent, CanvasRenderer]);

type Escala = 'adaptada' | 'completa';
type FaseCamara = 'enfocando' | 'inmersion' | 'emergiendo' | 'retirando' | 'regresando' | null;

interface NodoPaisaje {
  id: number;
  nombre: string;
  cliente: string;
  valor: number | null;
  estado: string;
  horas: number;
  capacidad: number;
  participantes: number;
  empleado?: OcupacionEmpleado;
  asignacion?: OcupacionEmpleadoProyecto;
}

@Component({
  selector: 'app-occupancy-landscape',
  standalone: true,
  templateUrl: './occupancy-landscape.component.html',
  styleUrl: './occupancy-landscape.component.css',
})
export class OccupancyLandscapeComponent implements AfterViewInit, OnChanges, OnDestroy {
  private readonly zone = inject(NgZone);
  private readonly changeDetector = inject(ChangeDetectorRef);
  readonly localization = inject(LocalizationService);
  @Input({ required: true }) proyectos: OcupacionProyecto[] = [];
  @Input({ required: true }) empleados: OcupacionEmpleado[] = [];
  @Output() empleadoSeleccionado = new EventEmitter<{ empleado: OcupacionEmpleado; proyecto: OcupacionProyecto }>();
  @ViewChild('chart') private readonly chartElement?: ElementRef<HTMLDivElement>;

  private chart?: echarts.ECharts;
  private resizeObserver?: ResizeObserver;
  private temporizadorCamara?: ReturnType<typeof globalThis.setTimeout>;
  private temporizadorResaltado?: ReturnType<typeof globalThis.setTimeout>;
  protected proyecto: OcupacionProyecto | null = null;
  protected escala: Escala = 'adaptada';
  protected fase: FaseCamara = null;
  protected focoX = '50%';
  protected focoY = '50%';
  protected proyectoResaltadoId: number | null = null;

  constructor() { effect(() => { this.localization.language(); queueMicrotask(() => this.dibujar(false)); }); }

  ngAfterViewInit(): void { this.inicializar(); }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['proyectos'] || changes['empleados']) {
      this.proyecto = null;
      this.fase = null;
      this.proyectoResaltadoId = null;
      if (this.chart) this.dibujar();
    }
  }

  ngOnDestroy(): void {
    if (this.temporizadorCamara) globalThis.clearTimeout(this.temporizadorCamara);
    if (this.temporizadorResaltado) globalThis.clearTimeout(this.temporizadorResaltado);
    this.resizeObserver?.disconnect();
    this.chart?.dispose();
  }

  protected alternarEscala(): void {
    this.escala = this.escala === 'adaptada' ? 'completa' : 'adaptada';
    this.dibujar(false);
  }

  protected volverAlPanorama(): void {
    if (!this.proyecto || this.fase) return;
    const proyectoAnterior = this.proyecto;
    const duracion = this.duracionCamara();
    this.fase = 'retirando';
    this.temporizadorCamara = globalThis.setTimeout(() => {
      this.proyecto = null;
      this.proyectoResaltadoId = proyectoAnterior.idProyecto;
      this.fase = 'regresando';
      this.changeDetector.detectChanges();
      this.chart?.clear();
      this.chart?.resize();
      this.dibujar();
      requestAnimationFrame(() => requestAnimationFrame(() => {
        this.fase = null;
        this.changeDetector.detectChanges();
      }));
      this.temporizadorResaltado = globalThis.setTimeout(() => { this.proyectoResaltadoId = null; this.dibujar(false); }, 1750);
    }, duracion);
  }

  protected etiquetaEscala(): string { return this.escala === 'adaptada' ? 'Usar escala completa' : 'Usar escala adaptada'; }
  protected tituloNivel(): string { return this.proyecto ? this.localization.traducir('static.assignedEmployeesTitle') : this.localization.traducir('static.occupancyLandscapeTitle'); }
  protected descripcionNivel(): string {
    return this.proyecto
      ? `${this.proyecto.nombreProyecto} · seleccioná un empleado para abrir sus tareas, diagnóstico y revisión.`
      : 'Cada pico es el coeficiente ponderado de ocupación de los empleados asignados al proyecto. Ordenado por horas planificadas activas.';
  }
  protected leyendaEscala(): string { return this.escala === 'adaptada' ? 'Escala adaptada: amplía diferencias reales del alcance actual.' : 'Escala completa: referencia desde 0% y umbrales operativos.'; }
  protected hayNodos(): boolean { return this.nodosActuales().some(nodo => nodo.valor !== null); }

  private inicializar(): void {
    if (!this.chartElement) return;
    this.chart = echarts.init(this.chartElement.nativeElement, undefined, { renderer: 'canvas' });
    this.chart.on('click', (evento: { dataIndex?: number; seriesType?: string; event?: { offsetX?: number; offsetY?: number } }) => {
      if (evento.dataIndex === undefined || evento.seriesType !== 'scatter') return;
      this.fijarOrigenCamara(evento.event);
      this.zone.run(() => this.seleccionarNodo(evento.dataIndex!));
    });
    this.resizeObserver = new ResizeObserver(() => this.chart?.resize());
    this.resizeObserver.observe(this.chartElement.nativeElement);
    this.dibujar();
  }

  private seleccionarNodo(indice: number): void {
    if (this.fase) return;
    const nodo = this.nodosActuales()[indice];
    if (!nodo || nodo.valor === null) return;
    if (this.proyecto && nodo.empleado) {
      this.empleadoSeleccionado.emit({ empleado: nodo.empleado, proyecto: this.proyecto });
      return;
    }
    const proyecto = this.proyectos[indice];
    if (!proyecto) return;
    const duracion = this.duracionCamara();
    this.proyectoResaltadoId = proyecto.idProyecto;
    this.dibujar(false);
    this.fase = 'enfocando';
    this.temporizadorCamara = globalThis.setTimeout(() => {
      this.fase = 'inmersion';
      this.changeDetector.detectChanges();
      this.temporizadorCamara = globalThis.setTimeout(() => {
        this.proyecto = proyecto;
        this.proyectoResaltadoId = null;
        this.fase = 'emergiendo';
        this.changeDetector.detectChanges();
        this.chart?.clear();
        this.chart?.resize();
        this.dibujar();
        requestAnimationFrame(() => requestAnimationFrame(() => {
          this.fase = null;
          this.changeDetector.detectChanges();
        }));
      }, duracion);
    }, duracion);
  }

  private fijarOrigenCamara(evento?: { offsetX?: number; offsetY?: number }): void {
    const ancho = this.chartElement?.nativeElement.clientWidth ?? 1;
    const alto = this.chartElement?.nativeElement.clientHeight ?? 1;
    this.focoX = `${Math.max(8, Math.min(92, ((evento?.offsetX ?? ancho / 2) / ancho) * 100)).toFixed(1)}%`;
    this.focoY = `${Math.max(8, Math.min(92, ((evento?.offsetY ?? alto / 2) / alto) * 100)).toFixed(1)}%`;
  }

  private duracionCamara(): number { return globalThis.matchMedia?.('(prefers-reduced-motion: reduce)').matches ? 0 : 210; }

  private nodosActuales(): NodoPaisaje[] {
    if (!this.proyecto) return this.proyectos.map(proyecto => this.nodoProyecto(proyecto));
    return this.proyecto.empleados.map(asignacion => this.nodoEmpleado(asignacion));
  }

  private nodoProyecto(proyecto: OcupacionProyecto): NodoPaisaje {
    const contribuciones = proyecto.empleados.map(asignacion => ({ asignacion, empleado: this.empleados.find(empleado => empleado.idEmpleado === asignacion.idEmpleado) })).filter((item): item is { asignacion: OcupacionEmpleadoProyecto; empleado: OcupacionEmpleado } => !!item.empleado);
    const horasConOcupacion = contribuciones.reduce((total, item) => total + item.asignacion.horasEstimadas, 0);
    const coeficiente = horasConOcupacion > 0
      ? contribuciones.reduce((total, item) => total + item.empleado.porcentajeOcupacion * item.asignacion.horasEstimadas, 0) / horasConOcupacion
      : null;
    return { id: proyecto.idProyecto, nombre: proyecto.nombreProyecto, cliente: proyecto.nombreCliente, valor: coeficiente, estado: coeficiente === null ? 'Sin datos' : this.estadoPorcentaje(coeficiente), horas: proyecto.horasEstimadasActivas, capacidad: contribuciones.reduce((total, item) => total + item.empleado.horasDisponibles, 0), participantes: proyecto.empleados.length };
  }

  private nodoEmpleado(asignacion: OcupacionEmpleadoProyecto): NodoPaisaje {
    const empleado = this.empleados.find(item => item.idEmpleado === asignacion.idEmpleado);
    return { id: asignacion.idEmpleado, nombre: asignacion.nombreEmpleado, cliente: this.proyecto?.nombreCliente ?? '', valor: empleado?.porcentajeOcupacion ?? null, estado: empleado?.estadoCarga ?? 'Sin datos', horas: asignacion.horasEstimadas, capacidad: empleado?.horasDisponibles ?? 0, participantes: 1, empleado, asignacion };
  }

  private estadoPorcentaje(porcentaje: number): string {
    if (porcentaje < 50) return 'Subutilizado';
    if (porcentaje < 86) return 'Balanceado';
    if (porcentaje <= 100) return 'Sobrecargado';
    return 'Crítico';
  }

  private colorEstado(estado: string): string {
    const valor = estado.toLowerCase();
    if (valor.includes('crít')) return '#ff5b70';
    if (valor.includes('sobrec')) return '#ff9f1c';
    if (valor.includes('balance')) return '#35d39b';
    if (valor.includes('subutil')) return '#57a6ff';
    return '#829bb0';
  }

  private dominio(valores: number[]): { min: number; max: number } {
    if (!valores.length) return { min: 0, max: 120 };
    if (this.escala === 'completa') return { min: 0, max: Math.max(120, Math.ceil((Math.max(...valores) + 8) / 20) * 20) };
    const menor = Math.min(...valores); const mayor = Math.max(...valores); const margen = Math.max(5, (mayor - menor) * .26);
    return { min: Math.max(0, Math.floor((menor - margen) * 10) / 10), max: Math.ceil((mayor + margen) * 10) / 10 };
  }

  private escaparHtml(valor: string): string { return valor.replace(/[&<>"']/g, caracter => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[caracter] ?? caracter); }

  private tooltip(nodo: NodoPaisaje): string {
    const valor = nodo.valor === null ? this.localization.traducir('static.noData') : `${nodo.valor.toFixed(2)}%`;
    const lineaAlcance = this.proyecto ? `${this.localization.traducir('static.assignedHours')}: ${nodo.horas.toFixed(2)} h` : `${this.localization.traducir('static.activePlannedLoad')}: ${nodo.horas.toFixed(2)} h`;
    const contexto = this.proyecto ? `${this.localization.traducir('static.employeeCapacity')}: ${nodo.capacidad.toFixed(2)} h` : `${this.localization.traducir('static.employeesInvolved')}: ${nodo.participantes}`;
    const click = this.proyecto ? this.localization.traducir('static.clickTasksAction') : this.localization.traducir('static.clickProjectTeam');
    return `<div style="min-width:238px;line-height:1.55"><strong style="display:block;font-size:14px;margin-bottom:3px">${this.escaparHtml(nodo.nombre)}</strong><span style="display:block;color:#9fc5d2;margin-bottom:8px">${this.escaparHtml(nodo.cliente)}</span><b style="display:block;color:${this.colorEstado(nodo.estado)};margin-bottom:9px">${valor} · ${this.escaparHtml(this.localization.traducirTextoEstatico(nodo.estado))}</b><div style="border-top:1px solid rgba(159,197,210,.35);padding-top:8px;margin-top:4px">${lineaAlcance}<br/>${contexto}</div><small style="display:block;margin-top:10px;color:#6ce5ff">${click}</small></div>`;
  }

  private dibujar(animar = true): void {
    if (!this.chart) return;
    const nodos = this.nodosActuales();
    const valores = nodos.map(nodo => nodo.valor).filter((valor): valor is number => valor !== null);
    const dominio = this.dominio(valores);
    const alto = Math.max(460, nodos.length > 8 ? 520 : 460);
    this.chart.resize({ height: alto });
    const nombres = nodos.map(nodo => nodo.nombre);
    const ultimoIndice = Math.max(0, nodos.length - 1);
    const margenExtremo = nodos.length > 1 ? Math.max(.035, ultimoIndice * .025) : .035;
    const datosLinea: Array<[number, number | null]> = nodos.map((nodo, indice) => [indice, nodo.valor]);
    if (nodos.length) {
      datosLinea.unshift([-margenExtremo, nodos[0].valor]);
      datosLinea.push([ultimoIndice + margenExtremo, nodos[ultimoIndice].valor]);
    }
    const reducido = globalThis.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
    this.chart.setOption({
      animation: animar && !reducido, animationDuration: 1050, animationDurationUpdate: 760, animationEasing: 'cubicOut', animationEasingUpdate: 'cubicInOut',
      aria: { enabled: true, description: this.proyecto ? `Paisaje de ocupación de empleados del proyecto ${this.proyecto.nombreProyecto}.` : 'Paisaje de ocupación ponderada por proyecto.' },
      grid: { left: 48, right: 38, top: 64, bottom: 88, containLabel: true },
      tooltip: { trigger: 'item', backgroundColor: '#061a2c', borderColor: '#3d8ca9', borderWidth: 1, padding: 14, textStyle: { color: '#effdff' }, extraCssText: 'border-radius:14px;box-shadow:0 22px 52px rgba(0,0,0,.46);min-width:220px;', formatter: (evento: { dataIndex: number }) => nodos[evento.dataIndex] ? this.tooltip(nodos[evento.dataIndex]) : '' },
      xAxis: [
        { type: 'category', boundaryGap: false, data: nombres, axisTick: { show: false }, axisLine: { lineStyle: { color: '#4b7889', width: 2 } }, axisLabel: { color: '#d9f4fa', interval: 0, width: 132, overflow: 'truncate', rotate: nodos.length > 5 ? 18 : 0, fontWeight: 700, margin: 17 } },
        { type: 'value', min: -margenExtremo, max: ultimoIndice + margenExtremo, show: false, axisLine: { show: false }, axisTick: { show: false }, axisLabel: { show: false }, splitLine: { show: false } },
      ],
      yAxis: { type: 'value', min: dominio.min, max: dominio.max, name: this.proyecto ? this.localization.traducir('static.actualUtilization') : this.localization.traducir('static.weightedUtilization'), nameTextStyle: { color: '#a6cad6', fontWeight: 700, padding: [0, 0, 4, 4] }, axisLabel: { color: '#9fc6d1', formatter: '{value}%' }, axisLine: { lineStyle: { color: '#4b7889' } }, splitLine: { lineStyle: { color: 'rgba(132,195,214,.16)', type: 'dashed' } } },
      dataZoom: nodos.length > 7 ? [{ type: 'inside', xAxisIndex: [0, 1], zoomOnMouseWheel: true, moveOnMouseMove: true }, { type: 'slider', xAxisIndex: [0, 1], height: 13, bottom: 19, borderColor: 'transparent', fillerColor: 'rgba(24,193,225,.27)', backgroundColor: 'rgba(129,194,212,.12)', handleStyle: { color: '#48d7ef', borderColor: '#baf7ff' } }] : [],
      series: [
        { id: 'paisaje-ocupacion', type: 'line', xAxisIndex: 1, data: datosLinea, silent: true, tooltip: { show: false }, smooth: .36, smoothMonotone: 'x', connectNulls: false, showSymbol: false, z: 3, lineStyle: { width: 5, color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [{ offset: 0, color: '#57a6ff' }, { offset: .34, color: '#35d39b' }, { offset: .67, color: '#ffb11b' }, { offset: 1, color: '#ff5b70' }]), shadowBlur: 16, shadowColor: 'rgba(49,207,235,.38)' }, areaStyle: { color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [{ offset: 0, color: 'rgba(255,104,108,.34)' }, { offset: .35, color: 'rgba(255,178,29,.22)' }, { offset: .62, color: 'rgba(53,211,155,.18)' }, { offset: 1, color: 'rgba(69,157,255,.025)' }]) }, markArea: { silent: true, itemStyle: { opacity: .14 }, data: [[{ yAxis: 0, itemStyle: { color: '#317cc4' } }, { yAxis: 50 }], [{ yAxis: 50, itemStyle: { color: '#249d75' } }, { yAxis: 86 }], [{ yAxis: 86, itemStyle: { color: '#dc8d1d' } }, { yAxis: 100 }], [{ yAxis: 100, itemStyle: { color: '#d64e60' } }, { yAxis: dominio.max }]] }, markLine: { silent: true, symbol: 'none', label: { color: '#b8dce5', fontSize: 11, formatter: (parametro: { value: number }) => `${parametro.value}%` }, lineStyle: { type: 'dashed', color: 'rgba(199,235,242,.45)' }, data: [{ yAxis: 50 }, { yAxis: 86 }, { yAxis: 100 }] } },
        { id: 'nodos-ocupacion', type: 'scatter', xAxisIndex: 1, symbol: 'circle', z: 6, data: nodos.map((nodo, indice) => ({ value: nodo.valor === null ? [indice, dominio.min] : [indice, nodo.valor], itemStyle: { color: this.colorEstado(nodo.estado), borderColor: '#effdff', borderWidth: this.proyectoResaltadoId === nodo.id ? 4 : 2, shadowBlur: this.proyectoResaltadoId === nodo.id ? 30 : 18, shadowColor: `${this.colorEstado(nodo.estado)}c7`, opacity: nodo.valor === null ? .45 : 1 } })), symbolSize: 25, label: { show: true, position: 'top', distance: 11, formatter: (evento: { dataIndex: number }) => `{node|${nombres[evento.dataIndex]}}\n{value|${nodos[evento.dataIndex]?.valor?.toFixed(1) ?? '—'}%}`, rich: { node: { color: '#f0feff', fontWeight: 800, backgroundColor: '#10364a', borderColor: '#3e89a5', borderWidth: 1, borderRadius: 8, padding: [4, 7] }, value: { color: '#8fc9d7', fontSize: 10, lineHeight: 17 } } }, emphasis: { scale: 1.48, focus: 'series', itemStyle: { shadowBlur: 32 }, label: { show: true } } },
      ],
    }, { notMerge: true, lazyUpdate: false });
  }
}

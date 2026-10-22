import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, EventEmitter, inject, Input, NgZone, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild } from '@angular/core';
import * as echarts from 'echarts/core';
import { BarChart, LineChart, ScatterChart } from 'echarts/charts';
import { GridComponent, MarkAreaComponent, MarkLineComponent, MarkPointComponent, TooltipComponent } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import { DetalleRiesgoProyecto, TareaRiesgoRetraso } from '../../../../services/riesgo-retraso.service';

echarts.use([BarChart, LineChart, ScatterChart, GridComponent, MarkAreaComponent, MarkLineComponent, MarkPointComponent, TooltipComponent, CanvasRenderer]);

export interface RiesgoTareaSeleccionada { riesgo: DetalleRiesgoProyecto; tarea: TareaRiesgoRetraso; }

@Component({ selector: 'app-risk-observatory', standalone: true, templateUrl: './risk-observatory.component.html', styleUrl: './risk-observatory.component.css' })
export class RiskObservatoryComponent implements AfterViewInit, OnChanges, OnDestroy {
  private readonly zone = inject(NgZone);
  private readonly changeDetector = inject(ChangeDetectorRef);
  @Input({ required: true }) riesgos: DetalleRiesgoProyecto[] = [];
  @Output() tareaSeleccionada = new EventEmitter<RiesgoTareaSeleccionada>();
  @Output() detalleProyecto = new EventEmitter<DetalleRiesgoProyecto>();
  @ViewChild('overviewChart') private readonly overviewElement?: ElementRef<HTMLDivElement>;
  @ViewChild('timelineChart') private readonly timelineElement?: ElementRef<HTMLDivElement>;
  @ViewChild('capacityChart') private readonly capacityElement?: ElementRef<HTMLDivElement>;

  private overviewChart?: echarts.ECharts;
  private timelineChart?: echarts.ECharts;
  private capacityChart?: echarts.ECharts;
  private resizeObserver?: ResizeObserver;
  protected proyecto: DetalleRiesgoProyecto | null = null;

  ngAfterViewInit(): void {
    this.inicializarGraficos();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['riesgos']) {
      this.proyecto = null;
      if (this.overviewChart) this.dibujarPanorama();
    }
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
    this.overviewChart?.dispose();
    this.timelineChart?.dispose();
    this.capacityChart?.dispose();
  }

  protected abrirProyecto(riesgo: DetalleRiesgoProyecto): void {
    this.proyecto = riesgo;
    this.changeDetector.detectChanges();
    this.inicializarDetalle();
  }

  protected volverAlPanorama(): void {
    this.proyecto = null;
    this.changeDetector.detectChanges();
    this.dibujarPanorama();
  }

  protected abrirTarea(tarea: TareaRiesgoRetraso): void {
    if (this.proyecto) this.tareaSeleccionada.emit({ riesgo: this.proyecto, tarea });
  }

  protected verFichaProyecto(): void {
    if (this.proyecto) this.detalleProyecto.emit(this.proyecto);
  }

  protected colorRiesgo(nivel: string): string {
    const valor = nivel.toLowerCase();
    if (valor === 'alto') return '#fb5a70';
    if (valor === 'medio') return '#ffb11b';
    return '#38d39f';
  }

  protected tareasOrdenadas(): TareaRiesgoRetraso[] {
    return [...(this.proyecto?.tareasPendientes ?? [])].sort((a, b) => Number(b.bloqueada) - Number(a.bloqueada) || b.horasRestantes - a.horasRestantes);
  }

  private inicializarGraficos(): void {
    if (this.overviewElement) {
      this.overviewChart = echarts.init(this.overviewElement.nativeElement, undefined, { renderer: 'canvas' });
      this.overviewChart.on('click', (evento: { dataIndex?: number }) => {
        const riesgo = evento.dataIndex === undefined ? undefined : this.riesgos[evento.dataIndex];
        if (riesgo) this.zone.run(() => this.abrirProyecto(riesgo));
      });
    }
    this.resizeObserver = new ResizeObserver(() => {
      this.overviewChart?.resize(); this.timelineChart?.resize(); this.capacityChart?.resize();
    });
    if (this.overviewElement) this.resizeObserver.observe(this.overviewElement.nativeElement);
    this.dibujarPanorama();
  }

  private inicializarDetalle(): void {
    if (!this.proyecto || !this.timelineElement || !this.capacityElement) return;
    this.timelineChart?.dispose(); this.capacityChart?.dispose();
    this.timelineChart = echarts.init(this.timelineElement.nativeElement, undefined, { renderer: 'canvas' });
    this.capacityChart = echarts.init(this.capacityElement.nativeElement, undefined, { renderer: 'canvas' });
    this.resizeObserver?.observe(this.timelineElement.nativeElement);
    this.resizeObserver?.observe(this.capacityElement.nativeElement);
    this.dibujarTemporal();
    this.dibujarCapacidad();
  }

  private dibujarPanorama(): void {
    if (!this.overviewChart) return;
    const reduccionMovimiento = globalThis.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
    const proyectos = this.riesgos;
    const nombres = proyectos.map(proyecto => proyecto.nombreProyecto);
    const maximo = Math.max(7, ...proyectos.map(proyecto => proyecto.diasPosibleRetraso));
    this.overviewChart.setOption({
      animation: !reduccionMovimiento, animationDuration: 900, animationDurationUpdate: 480, animationEasing: 'cubicOut',
      grid: { left: 22, right: 38, top: 32, bottom: 28, containLabel: true },
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' }, backgroundColor: '#061a2c', borderColor: '#327d99', borderWidth: 1, padding: 14, textStyle: { color: '#ecfbff' }, extraCssText: 'border-radius:12px;box-shadow:0 18px 42px rgba(0,0,0,.38);', formatter: (parametros: Array<{ dataIndex: number }>) => this.tooltipPanorama(proyectos[parametros[0]?.dataIndex]) },
      xAxis: { type: 'value', max: maximo * 1.14, axisLabel: { color: '#8fb8c5', formatter: '{value} d' }, splitLine: { lineStyle: { color: 'rgba(122,184,203,.13)', type: 'dashed' } }, axisLine: { lineStyle: { color: '#376b7f' } } },
      yAxis: { type: 'category', inverse: true, data: nombres, axisLabel: { color: '#e0f6fb', width: 170, overflow: 'truncate', fontWeight: 700 }, axisTick: { show: false }, axisLine: { show: false } },
      series: [
        { type: 'bar', silent: true, data: proyectos.map(proyecto => ({ value: proyecto.diasPosibleRetraso, itemStyle: { color: `${this.colorRiesgo(proyecto.nivelRiesgo)}55`, borderRadius: 99 } })), barWidth: 7, emphasis: { disabled: true } },
        { type: 'scatter', data: proyectos.map(proyecto => ({ value: proyecto.diasPosibleRetraso, itemStyle: { color: this.colorRiesgo(proyecto.nivelRiesgo), shadowBlur: 22, shadowColor: `${this.colorRiesgo(proyecto.nivelRiesgo)}a8`, borderColor: '#e9fcff', borderWidth: 2 }, label: { show: true, position: 'right', color: '#e5f9fd', fontWeight: 800, formatter: proyecto.diasPosibleRetraso ? `${proyecto.diasPosibleRetraso} d` : 'Sin demora' } })), symbol: 'circle', symbolSize: 20, emphasis: { scale: 1.45, itemStyle: { shadowBlur: 34 }, focus: 'series' } },
      ],
    }, { notMerge: true });
  }

  private dibujarTemporal(): void {
    if (!this.timelineChart || !this.proyecto) return;
    const riesgo = this.proyecto;
    const hoy = this.inicioDelDia(new Date()).getTime();
    const deadline = this.fechaEnMilisegundos(riesgo.deadline);
    const estimada = this.fechaEnMilisegundos(riesgo.fechaEstimadaFinalizacion);
    if (!estimada) { this.timelineChart.clear(); return; }
    const finEje = Math.max(estimada, deadline ?? estimada, hoy) + 24 * 60 * 60 * 1000;
    const inicioEje = Math.min(hoy, deadline ?? hoy, estimada) - 24 * 60 * 60 * 1000;
    const color = this.colorRiesgo(riesgo.nivelRiesgo);
    const reduccionMovimiento = globalThis.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
    const capacidadEfectiva = riesgo.ritmoAvance > 0 ? Math.min(riesgo.disponibilidadEquipo, riesgo.ritmoAvance) : riesgo.disponibilidadEquipo;
    this.timelineChart.setOption({
      animation: !reduccionMovimiento, animationDuration: 1000, animationDurationUpdate: 520, animationEasing: 'cubicOut',
      grid: { left: 48, right: 38, top: 40, bottom: 48, containLabel: true },
      tooltip: { trigger: 'axis', axisPointer: { type: 'cross', label: { backgroundColor: '#12364a' } }, backgroundColor: '#061a2c', borderColor: '#327d99', borderWidth: 1, padding: 14, textStyle: { color: '#effcff' }, extraCssText: 'border-radius:12px;box-shadow:0 18px 42px rgba(0,0,0,.38);', formatter: () => `<strong>Proyección operativa</strong><br/>Pendiente actual: ${riesgo.horasRestantes} h<br/>Capacidad usada: ${capacidadEfectiva} h/sem<br/>Fecha estimada: ${this.formatearFecha(estimada)}<br/><span style="color:#8fc5d4">No se muestra histórico porque el análisis no lo entrega.</span>` },
      xAxis: { type: 'time', min: inicioEje, max: finEje, axisLabel: { color: '#9bc2ce', formatter: (valor: number) => this.formatearFecha(valor) }, axisLine: { lineStyle: { color: '#3b7083' } }, splitLine: { lineStyle: { color: 'rgba(122,184,203,.13)', type: 'dashed' } } },
      yAxis: { type: 'value', min: 0, name: 'Horas pendientes', nameTextStyle: { color: '#9bc2ce', padding: [0, 0, 0, 4] }, axisLabel: { color: '#9bc2ce', formatter: '{value} h' }, splitLine: { lineStyle: { color: 'rgba(122,184,203,.14)', type: 'dashed' } }, axisLine: { lineStyle: { color: '#3b7083' } } },
      series: [{ name: 'Proyección de trabajo pendiente', type: 'line', data: [[hoy, riesgo.horasRestantes], [estimada, 0]], smooth: 0.18, symbol: 'none', lineStyle: { width: 4, color, shadowBlur: 16, shadowColor: `${color}90` }, areaStyle: { color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [{ offset: 0, color: `${color}55` }, { offset: 1, color: `${color}05` }]) }, markPoint: { symbol: 'circle', symbolSize: 21, data: [{ coord: [hoy, riesgo.horasRestantes], value: 'Hoy', itemStyle: { color: '#21c4e4', borderColor: '#e8ffff', borderWidth: 2, shadowBlur: 18, shadowColor: '#21c4e4' }, label: { formatter: 'Hoy', color: '#dfffff', position: 'top' } }, { coord: [estimada, 0], value: 'Fin estimado', itemStyle: { color, borderColor: '#fff6e2', borderWidth: 2, shadowBlur: 19, shadowColor: color }, label: { formatter: 'Fin estimado', color: '#dfffff', position: 'top' } }] }, markLine: { symbol: 'none', label: { color: '#dcf9ff', backgroundColor: '#15384c', borderRadius: 5, padding: [4, 6] }, data: [{ xAxis: hoy, name: 'Hoy', lineStyle: { color: '#e8b22c', type: 'dashed', width: 2 } }, ...(deadline ? [{ xAxis: deadline, name: 'Deadline', lineStyle: { color: '#ff6576', type: 'dashed', width: 2 } }] : [])] }, markArea: deadline && estimada > deadline ? { itemStyle: { color: 'rgba(246,75,95,.13)' }, label: { color: '#ff9caa', fontWeight: 700, formatter: 'Zona posterior al deadline' }, data: [[{ xAxis: deadline }, { xAxis: estimada }]] } : undefined }],
    }, { notMerge: true });
  }

  private dibujarCapacidad(): void {
    if (!this.capacityChart || !this.proyecto) return;
    const riesgo = this.proyecto;
    const reduccionMovimiento = globalThis.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
    const capacidad = riesgo.disponibilidadEquipo;
    const ritmo = riesgo.ritmoAvance;
    this.capacityChart.setOption({
      animation: !reduccionMovimiento, animationDuration: 760, animationDurationUpdate: 420, animationEasing: 'cubicOut',
      grid: { left: 20, right: 16, top: 26, bottom: 32, containLabel: true },
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' }, backgroundColor: '#061a2c', borderColor: '#327d99', borderWidth: 1, padding: 12, textStyle: { color: '#effcff' }, extraCssText: 'border-radius:12px;', formatter: () => `<strong>Horas por semana</strong><br/>Capacidad disponible: ${capacidad} h<br/>Ritmo reciente: ${ritmo} h/sem` },
      xAxis: { type: 'category', data: ['Equipo'], axisLabel: { color: '#dff7fb', fontWeight: 700 }, axisTick: { show: false }, axisLine: { lineStyle: { color: '#3b7083' } } },
      yAxis: { type: 'value', axisLabel: { color: '#9bc2ce', formatter: '{value} h' }, splitLine: { lineStyle: { color: 'rgba(122,184,203,.14)', type: 'dashed' } } },
      series: [{ name: 'Capacidad disponible', type: 'bar', data: [capacidad], barMaxWidth: 42, itemStyle: { color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [{ offset: 0, color: '#3a9dff' }, { offset: 1, color: '#195095' }]), borderRadius: [8, 8, 2, 2] }, emphasis: { focus: 'series' } }, { name: 'Ritmo reciente', type: 'bar', data: [ritmo], barMaxWidth: 42, itemStyle: { color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [{ offset: 0, color: '#45d99a' }, { offset: 1, color: '#1c713f' }]), borderRadius: [8, 8, 2, 2] }, emphasis: { focus: 'series' } }],
    }, { notMerge: true });
  }

  private tooltipPanorama(riesgo?: DetalleRiesgoProyecto): string {
    if (!riesgo) return '';
    return `<strong>${riesgo.nombreProyecto}</strong><br/>Riesgo: ${riesgo.nivelRiesgo}<br/>Demora posible: ${riesgo.diasPosibleRetraso ? `${riesgo.diasPosibleRetraso} día(s)` : 'Sin demora'}<br/>Horas pendientes: ${riesgo.horasRestantes} h<br/>Fecha estimada: ${riesgo.fechaEstimadaFinalizacion ? this.formatearFecha(this.fechaEnMilisegundos(riesgo.fechaEstimadaFinalizacion)!) : 'Sin estimación'}<br/><span style="color:#6ce5ff">Hacé clic para profundizar</span>`;
  }

  private fechaEnMilisegundos(fecha?: string | null): number | null {
    if (!fecha) return null;
    const coincidencia = /^(\d{4})-(\d{2})-(\d{2})/.exec(fecha);
    if (coincidencia) return new Date(Number(coincidencia[1]), Number(coincidencia[2]) - 1, Number(coincidencia[3])).getTime();
    const valor = new Date(fecha).getTime();
    return Number.isNaN(valor) ? null : valor;
  }

  private inicioDelDia(fecha: Date): Date { return new Date(fecha.getFullYear(), fecha.getMonth(), fecha.getDate()); }
  private formatearFecha(valor: number): string { return new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: 'short' }).format(new Date(valor)); }
}

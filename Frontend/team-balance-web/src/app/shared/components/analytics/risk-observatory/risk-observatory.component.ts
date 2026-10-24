import { AfterViewInit, ChangeDetectorRef, Component, effect, ElementRef, EventEmitter, inject, Input, NgZone, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild } from '@angular/core';
import * as echarts from 'echarts/core';
import { BarChart, LineChart, ScatterChart } from 'echarts/charts';
import { GridComponent, MarkAreaComponent, MarkLineComponent, MarkPointComponent, TooltipComponent } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import { DetalleRiesgoProyecto, TareaRiesgoRetraso } from '../../../../services/riesgo-retraso.service';
import { LocalizationService } from '../../../../services/localization.service';

echarts.use([BarChart, LineChart, ScatterChart, GridComponent, MarkAreaComponent, MarkLineComponent, MarkPointComponent, TooltipComponent, CanvasRenderer]);

export interface RiesgoTareaSeleccionada { riesgo: DetalleRiesgoProyecto; tarea: TareaRiesgoRetraso; }

@Component({ selector: 'app-risk-observatory', standalone: true, templateUrl: './risk-observatory.component.html', styleUrl: './risk-observatory.component.css' })
export class RiskObservatoryComponent implements AfterViewInit, OnChanges, OnDestroy {
  private readonly zone = inject(NgZone);
  private readonly changeDetector = inject(ChangeDetectorRef);
  protected readonly localization = inject(LocalizationService);
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

  constructor() {
    effect(() => {
      this.localization.language();
      queueMicrotask(() => this.proyecto ? this.inicializarDetalle() : this.inicializarPanorama());
    });
  }

  ngAfterViewInit(): void {
    this.inicializarPanorama();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['riesgos']) {
      this.resizeObserver?.disconnect();
      this.timelineChart?.dispose();
      this.capacityChart?.dispose();
      this.timelineChart = undefined;
      this.capacityChart = undefined;
      this.proyecto = null;
      queueMicrotask(() => this.inicializarPanorama());
    }
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
    this.overviewChart?.dispose();
    this.timelineChart?.dispose();
    this.capacityChart?.dispose();
  }

  protected abrirProyecto(riesgo: DetalleRiesgoProyecto): void {
    this.resizeObserver?.disconnect();
    this.overviewChart?.dispose();
    this.overviewChart = undefined;
    this.proyecto = riesgo;
    this.changeDetector.detectChanges();
    queueMicrotask(() => this.inicializarDetalle());
  }

  protected volverAlPanorama(): void {
    this.resizeObserver?.disconnect();
    this.timelineChart?.dispose();
    this.capacityChart?.dispose();
    this.timelineChart = undefined;
    this.capacityChart = undefined;
    this.proyecto = null;
    this.changeDetector.detectChanges();
    queueMicrotask(() => this.inicializarPanorama());
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

  protected fechaLegible(fecha?: string | null): string {
    const valor = this.fechaEnMilisegundos(fecha);
    return valor ? this.formatearFechaCompleta(valor) : '—';
  }

  private inicializarPanorama(): void {
    if (!this.overviewElement) return;
    if (!this.overviewChart || this.overviewChart.getDom() !== this.overviewElement.nativeElement) {
      this.overviewChart?.dispose();
      this.overviewChart = echarts.init(this.overviewElement.nativeElement, undefined, { renderer: 'canvas' });
      this.overviewChart.on('click', (evento: { dataIndex?: number }) => {
        const riesgo = evento.dataIndex === undefined ? undefined : this.riesgos[evento.dataIndex];
        if (riesgo) this.zone.run(() => this.abrirProyecto(riesgo));
      });
    }
    this.configurarResizeObserver(this.overviewElement.nativeElement);
    this.dibujarPanorama();
  }

  private inicializarDetalle(): void {
    if (!this.proyecto || !this.timelineElement || !this.capacityElement) return;
    this.timelineChart?.dispose(); this.capacityChart?.dispose();
    this.timelineChart = echarts.init(this.timelineElement.nativeElement, undefined, { renderer: 'canvas' });
    this.capacityChart = echarts.init(this.capacityElement.nativeElement, undefined, { renderer: 'canvas' });
    this.configurarResizeObserver(this.timelineElement.nativeElement, this.capacityElement.nativeElement);
    this.dibujarTemporal();
    this.dibujarCapacidad();
  }

  private configurarResizeObserver(...elementos: HTMLElement[]): void {
    this.resizeObserver?.disconnect();
    this.resizeObserver = new ResizeObserver(() => {
      this.overviewChart?.resize(); this.timelineChart?.resize(); this.capacityChart?.resize();
    });
    elementos.forEach(elemento => this.resizeObserver?.observe(elemento));
  }

  private dibujarPanorama(): void {
    if (!this.overviewChart) return;
    const reduccionMovimiento = globalThis.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
    const proyectos = this.riesgos;
    const nombres = proyectos.map(proyecto => proyecto.nombreProyecto);
    const maximo = Math.max(7, ...proyectos.map(proyecto => proyecto.diasPosibleRetraso));
    const margenIzquierdo = Math.max(1, Math.ceil(maximo * 0.035));
    const limiteSuperior = Math.ceil(maximo * 1.16);
    this.overviewChart.setOption({
      animation: !reduccionMovimiento, animationDuration: 900, animationDurationUpdate: 480, animationEasing: 'cubicOut',
      grid: { left: 34, right: 58, top: 38, bottom: 42, containLabel: true },
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' }, backgroundColor: '#061a2c', borderColor: '#327d99', borderWidth: 1, padding: 14, textStyle: { color: '#ecfbff' }, extraCssText: 'border-radius:12px;box-shadow:0 18px 42px rgba(0,0,0,.38);', formatter: (parametros: Array<{ dataIndex: number }>) => this.tooltipPanorama(proyectos[parametros[0]?.dataIndex]) },
      xAxis: { type: 'value', min: -margenIzquierdo, max: limiteSuperior, splitNumber: 8, minInterval: 1, axisLabel: { color: '#8fb8c5', formatter: (valor: number) => valor < 0 ? '' : `${Math.round(valor)} d` }, splitLine: { lineStyle: { color: 'rgba(122,184,203,.13)', type: 'dashed' } }, minorTick: { show: true }, minorSplitLine: { show: true, lineStyle: { color: 'rgba(122,184,203,.055)' } }, axisLine: { lineStyle: { color: '#376b7f' } } },
      yAxis: { type: 'category', inverse: true, boundaryGap: true, data: nombres, axisLabel: { color: '#e0f6fb', margin: 18, width: 170, overflow: 'truncate', fontWeight: 700 }, axisTick: { show: false }, axisLine: { show: false } },
      series: [
        { type: 'bar', silent: true, data: proyectos.map(proyecto => ({ value: proyecto.diasPosibleRetraso, itemStyle: { color: `${this.colorRiesgo(proyecto.nivelRiesgo)}55`, borderRadius: 99 } })), barWidth: 7, emphasis: { disabled: true } },
        { type: 'scatter', data: proyectos.map(proyecto => ({ value: proyecto.diasPosibleRetraso, itemStyle: { color: this.colorRiesgo(proyecto.nivelRiesgo), shadowBlur: 22, shadowColor: `${this.colorRiesgo(proyecto.nivelRiesgo)}a8`, borderColor: '#e9fcff', borderWidth: 2 }, label: { show: true, position: 'right', color: '#e5f9fd', fontWeight: 800, formatter: proyecto.diasPosibleRetraso ? `${proyecto.diasPosibleRetraso} d` : this.t('static.noDelay') } })), symbol: 'circle', symbolSize: 20, emphasis: { scale: 1.45, itemStyle: { shadowBlur: 34 }, focus: 'series' } },
      ],
    }, { notMerge: true });
  }

  private dibujarTemporal(): void {
    if (!this.timelineChart || !this.proyecto) return;
    const riesgo = this.proyecto;
    const hoy = this.inicioDelDia(new Date()).getTime();
    const inicioProyecto = this.fechaEnMilisegundos(riesgo.fechaInicio);
    const deadline = this.fechaEnMilisegundos(riesgo.deadline);
    const estimada = this.fechaEnMilisegundos(riesgo.fechaEstimadaFinalizacion);
    if (!estimada) { this.timelineChart.clear(); return; }
    const dia = 24 * 60 * 60 * 1000;
    const tareasConDeadline = (riesgo.hitosTareas ?? riesgo.tareasPendientes).map(tarea => ({ tarea, deadline: this.fechaEnMilisegundos(tarea.deadline), inicio: this.fechaEnMilisegundos(tarea.fechaInicio) ?? inicioProyecto ?? hoy })).filter((hito): hito is typeof hito & { deadline: number } => hito.deadline !== null);
    const fechasTareas = tareasConDeadline.map(hito => hito.deadline);
    const fechasRelevantes = [inicioProyecto, hoy, deadline, estimada, ...fechasTareas].filter((fecha): fecha is number => fecha !== null);
    const alcance = Math.max(...fechasRelevantes) - Math.min(...fechasRelevantes);
    const margenTemporal = Math.max(5 * dia, Math.round(alcance * 0.07));
    const finEje = Math.max(...fechasRelevantes) + margenTemporal;
    const inicioEje = Math.min(...fechasRelevantes) - margenTemporal;
    const color = this.colorRiesgo(riesgo.nivelRiesgo);
    const reduccionMovimiento = globalThis.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
    const capacidadEfectiva = riesgo.ritmoAvance > 0 ? Math.min(riesgo.disponibilidadEquipo, riesgo.ritmoAvance) : riesgo.disponibilidadEquipo;
    const alturaTotal = Math.max(1, riesgo.horasRestantes);
    const alturaMinima = alturaTotal * 0.09;
    const hitosVisuales = tareasConDeadline.sort((hitoA, hitoB) => hitoA.deadline - hitoB.deadline).map((hito, indice) => {
      const evaluacion = this.evaluarHitoTarea({ porcentajeAvance: hito.tarea.porcentajeAvance ?? 0, fechaInicio: hito.inicio, deadline: hito.deadline, hoy });
      const alturaPorCarga = Math.max(alturaMinima, Math.min(alturaTotal * 0.88, hito.tarea.horasRestantes ?? 0));
      const separacion = (indice % 3) * alturaTotal * 0.018;
      const altura = Math.min(alturaTotal * 0.9, alturaPorCarga + separacion);
      return { ...hito, altura, finLinea: Math.max(0, altura - alturaTotal * 0.035), ...evaluacion };
    });
    const simboloHito = 'path://M0,-12 L3.2,-3.2 L12,0 L3.2,3.2 L0,12 L-3.2,3.2 L-12,0 L-3.2,-3.2 Z';
    const seriesHitos = hitosVisuales.map(hito => ({ name: hito.tarea.titulo, type: 'line' as const, silent: true, symbol: 'none', data: [[hito.deadline, 0], [hito.deadline, hito.finLinea]], lineStyle: { color: hito.color, type: 'dotted' as const, width: 2, opacity: 0.82 }, z: 4 }));
    const serieEstrellas = {
      name: 'Hitos de tareas', type: 'scatter' as const, symbol: simboloHito, symbolSize: 20, z: 10,
      data: hitosVisuales.map((hito, indice) => ({ value: [hito.deadline, hito.altura], itemStyle: { color: hito.color, borderColor: '#eaffff', borderWidth: 1.5, shadowBlur: 14, shadowColor: `${hito.color}99` }, label: { show: true, position: indice % 2 === 0 ? 'top' : 'right', distance: 8, color: '#dff8ff', fontSize: 10, fontWeight: 700, formatter: this.acortarTitulo(hito.tarea.titulo), backgroundColor: 'rgba(5, 28, 44, .82)', borderRadius: 4, padding: [3, 5] } })),
      labelLayout: { hideOverlap: true, moveOverlap: 'shiftY' },
      emphasis: { scale: 1.35, focus: 'self' as const },
      tooltip: { trigger: 'item' as const, formatter: (parametro: { dataIndex: number }) => this.tooltipHitoTarea(hitosVisuales[parametro.dataIndex]) },
    };
    const serieHoy = { name: this.t('static.today'), type: 'line' as const, silent: true, symbol: 'none', data: [[hoy, 0], [hoy, Math.max(0, riesgo.horasRestantes - alturaTotal * 0.055)]], lineStyle: { color: '#e8b22c', type: 'dashed' as const, width: 2 }, z: 7 };
    const serieHorizonte = {
      name: this.t('static.pendingWorkHorizon'), type: 'line' as const, clip: false, data: [[hoy, riesgo.horasRestantes], [estimada, 0]], smooth: 0.18, symbol: 'none', z: 6,
      lineStyle: { width: 4, color, shadowBlur: 16, shadowColor: `${color}90` },
      areaStyle: { color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [{ offset: 0, color: `${color}55` }, { offset: 1, color: `${color}05` }]) },
      markPoint: { symbol: 'circle', symbolSize: 21, z: 12, data: [{ coord: [hoy, riesgo.horasRestantes], value: this.t('static.today'), itemStyle: { color: '#21c4e4', borderColor: '#e8ffff', borderWidth: 3, shadowBlur: 18, shadowColor: '#21c4e4', opacity: 1 }, label: { show: false } }, { coord: [estimada, 0], value: this.t('static.estimatedEnd'), itemStyle: { color, borderColor: '#fff6e2', borderWidth: 2, shadowBlur: 19, shadowColor: color }, label: { formatter: `${this.t('static.estimatedEnd')}\n${this.formatearFechaCompleta(estimada)}`, color: '#dfffff', position: 'top', distance: 12, lineHeight: 16 } }] },
      markLine: { symbol: ['none', 'none'], silent: true, data: [...(inicioProyecto ? [{ xAxis: inicioProyecto, name: this.t('common.start'), label: { formatter: this.t('common.start'), position: 'insideEndTop', distance: 10, color: '#9ef0d1', backgroundColor: 'transparent', padding: 0, fontWeight: 700 }, lineStyle: { color: '#38d39f', type: 'solid', width: 2 } }] : []), ...(deadline ? [{ xAxis: deadline, name: this.etiquetaDeadlineProyecto(), label: { formatter: this.etiquetaDeadlineProyecto(), position: 'insideEndBottom', distance: 8, color: '#ffabb6', backgroundColor: 'rgba(38, 20, 35, .78)', borderRadius: 4, padding: [3, 6], fontWeight: 700 }, lineStyle: { color: '#ff6576', type: 'dashed', width: 2.5 } }] : [])] },
      markArea: deadline && estimada > deadline ? { itemStyle: { color: 'rgba(246,75,95,.13)' }, label: { color: '#ff9caa', fontWeight: 700, formatter: this.t('static.afterDeadline') }, data: [[{ xAxis: deadline }, { xAxis: estimada }]] } : undefined,
    };
    this.timelineChart.setOption({
      animation: !reduccionMovimiento, animationDuration: 1000, animationDurationUpdate: 520, animationEasing: 'cubicOut',
      grid: { left: 82, right: 72, top: 88, bottom: 78, containLabel: true },
      tooltip: { trigger: 'axis', axisPointer: { type: 'cross', snap: false, label: { backgroundColor: '#12364a', formatter: (parametro: { axisDimension?: string; value?: string | number }) => parametro.axisDimension === 'x' ? this.formatearFechaCompleta(Number(parametro.value)) : `${Math.round(Number(parametro.value))} h` } }, backgroundColor: '#061a2c', borderColor: '#327d99', borderWidth: 1, padding: 14, textStyle: { color: '#effcff' }, extraCssText: 'border-radius:12px;box-shadow:0 18px 42px rgba(0,0,0,.38);', formatter: () => `<strong>${this.t('static.operationalProjection')}</strong><br/>${this.t('static.currentPending')}: ${riesgo.horasRestantes} h<br/>${this.t('static.capacityUsed')}: ${capacidadEfectiva} h/sem<br/>${this.etiquetaDeadlineProyecto()}: ${deadline ? this.formatearFechaCompleta(deadline) : '—'}<br/>${this.t('static.estimatedDate')}: ${this.formatearFechaCompleta(estimada)}<br/><span style="color:#8fc5d4">${this.t('static.noHistory')}</span>` },
      xAxis: { type: 'time', min: inicioEje, max: finEje, splitNumber: 12, minInterval: dia, axisLabel: { color: '#9bc2ce', hideOverlap: true, margin: 15, formatter: (valor: number) => this.formatearFechaEje(valor, alcance) }, axisLine: { lineStyle: { color: '#3b7083' } }, axisTick: { show: true, length: 7, lineStyle: { color: '#4d8497' } }, splitLine: { show: true, lineStyle: { color: 'rgba(122,184,203,.13)', type: 'dashed' } }, minorTick: { show: true, splitNumber: 4, length: 3 }, minorSplitLine: { show: true, lineStyle: { color: 'rgba(122,184,203,.05)' } } },
      yAxis: { type: 'value', min: 0, splitNumber: 6, minInterval: 1, name: this.t('static.hoursPending'), nameGap: 18, nameTextStyle: { color: '#9bc2ce', padding: [0, 0, 0, 4] }, axisLabel: { color: '#9bc2ce', formatter: (valor: number) => `${Math.round(valor)} h` }, splitLine: { lineStyle: { color: 'rgba(122,184,203,.14)', type: 'dashed' } }, minorTick: { show: true }, minorSplitLine: { show: true, lineStyle: { color: 'rgba(122,184,203,.05)' } }, axisLine: { lineStyle: { color: '#3b7083' } } },
      series: [serieHorizonte, ...seriesHitos, serieHoy, serieEstrellas],
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
    return `<strong>${riesgo.nombreProyecto}</strong><br/>${this.t('static.risk')}: ${this.localization.traducirEstado(riesgo.nivelRiesgo)}<br/>${this.t('static.potentialDelay')}: ${riesgo.diasPosibleRetraso ? `${riesgo.diasPosibleRetraso} d` : this.t('static.noDelay')}<br/>${this.t('static.pendingHours')}: ${riesgo.horasRestantes} h<br/>${this.t('static.estimatedDate')}: ${riesgo.fechaEstimadaFinalizacion ? this.formatearFecha(this.fechaEnMilisegundos(riesgo.fechaEstimadaFinalizacion)!) : this.t('static.noEstimate')}<br/><span style="color:#6ce5ff">${this.t('static.clickDetail')}</span>`;
  }

  private fechaEnMilisegundos(fecha?: string | null): number | null {
    if (!fecha) return null;
    const coincidencia = /^(\d{4})-(\d{2})-(\d{2})/.exec(fecha);
    if (coincidencia) return new Date(Number(coincidencia[1]), Number(coincidencia[2]) - 1, Number(coincidencia[3])).getTime();
    const valor = new Date(fecha).getTime();
    return Number.isNaN(valor) ? null : valor;
  }

  private inicioDelDia(fecha: Date): Date { return new Date(fecha.getFullYear(), fecha.getMonth(), fecha.getDate()); }
  private t(key: string): string { return this.localization.traducir(key); }
  private etiquetaDeadlineProyecto(): string { return `${this.t('static.project')} · ${this.t('common.deadline')}`; }
  private acortarTitulo(titulo: string): string { return titulo.length > 26 ? `${titulo.slice(0, 25)}…` : titulo; }
  private evaluarHitoTarea({ porcentajeAvance, fechaInicio, deadline, hoy }: { porcentajeAvance: number; fechaInicio: number; deadline: number; hoy: number }): { color: string; nivel: string; avanceProyectado: number } {
    const avance = Math.max(0, Math.min(100, porcentajeAvance));
    if (avance >= 100) return { color: '#38d39f', nivel: this.t('static.low'), avanceProyectado: 100 };
    if (deadline < hoy) return { color: '#fb5a70', nivel: this.t('static.high'), avanceProyectado: avance };
    if (hoy <= fechaInicio) return { color: '#38d39f', nivel: this.t('static.low'), avanceProyectado: avance };
    const dia = 24 * 60 * 60 * 1000;
    const diasTranscurridos = Math.max(1, (hoy - fechaInicio) / dia);
    const diasDisponibles = Math.max(0, (deadline - hoy) / dia);
    const ritmoDiario = avance / diasTranscurridos;
    const avanceProyectado = Math.max(avance, Math.min(100, avance + ritmoDiario * diasDisponibles));
    if (avanceProyectado >= 100) return { color: '#38d39f', nivel: this.t('static.low'), avanceProyectado };
    if (avanceProyectado >= 80) return { color: '#ffb11b', nivel: this.t('static.medium'), avanceProyectado };
    return { color: '#fb5a70', nivel: this.t('static.high'), avanceProyectado };
  }
  private tooltipHitoTarea(hito: { tarea: { titulo: string; porcentajeAvance: number; horasRestantes: number }; deadline: number; color: string; nivel: string; avanceProyectado: number }): string {
    return `<strong>${hito.tarea.titulo}</strong><br/>${this.t('common.deadline')}: ${this.formatearFechaCompleta(hito.deadline)}<br/>${this.t('static.actual')}: ${Math.round(hito.tarea.porcentajeAvance)}%<br/>${this.t('static.estimated')}: ${Math.round(hito.avanceProyectado)}%<br/>${this.t('static.pendingHours')}: ${Math.round(hito.tarea.horasRestantes)} h<br/>${this.t('static.risk')}: <span style="color:${hito.color}">${hito.nivel}</span>`;
  }
  private formatearFecha(valor: number): string { return new Intl.DateTimeFormat(this.localization.culture(), { day: '2-digit', month: 'short' }).format(new Date(valor)); }
  private formatearFechaCompleta(valor: number): string { return new Intl.DateTimeFormat(this.localization.culture(), { day: '2-digit', month: 'short', year: 'numeric' }).format(new Date(valor)); }
  private formatearFechaEje(valor: number, alcance: number): string {
    return new Intl.DateTimeFormat(this.localization.culture(), alcance > 300 * 24 * 60 * 60 * 1000 ? { month: 'short', year: '2-digit' } : { day: '2-digit', month: 'short' }).format(new Date(valor));
  }
}

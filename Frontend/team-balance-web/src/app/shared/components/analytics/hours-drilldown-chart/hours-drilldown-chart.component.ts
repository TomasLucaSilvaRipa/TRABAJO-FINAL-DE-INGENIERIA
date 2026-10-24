import { AfterViewInit, Component, effect, ElementRef, EventEmitter, inject, Input, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild } from '@angular/core';
import * as echarts from 'echarts/core';
import { BarChart, ScatterChart } from 'echarts/charts';
import { GridComponent, LegendComponent, TooltipComponent } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import { DesvioHorasProyecto, DesvioHorasTarea } from '../../../../services/analitica-operativa.service';
import { LocalizationService } from '../../../../services/localization.service';

echarts.use([BarChart, ScatterChart, GridComponent, LegendComponent, TooltipComponent, CanvasRenderer]);

const simboloRelojArena = `image://data:image/svg+xml;charset=UTF-8,${encodeURIComponent(`
  <svg xmlns="http://www.w3.org/2000/svg" width="26" height="32" viewBox="0 0 26 32">
    <defs>
      <linearGradient id="cristal" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0" stop-color="#f4feff" stop-opacity=".98"/>
        <stop offset=".42" stop-color="#9feaff" stop-opacity=".88"/>
        <stop offset="1" stop-color="#40c9ec" stop-opacity=".72"/>
      </linearGradient>
      <linearGradient id="arena" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#effeff" stop-opacity=".82"/>
        <stop offset="1" stop-color="#65daf4" stop-opacity=".48"/>
      </linearGradient>
      <filter id="brillo" x="-70%" y="-50%" width="240%" height="200%">
        <feGaussianBlur stdDeviation="1.35" result="b"/>
        <feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge>
      </filter>
    </defs>
    <path d="M4 3h18M4 29h18M6 4c0 6.4 3.7 8.2 7 12-3.3 3.8-7 5.6-7 12M20 4c0 6.4-3.7 8.2-7 12 3.3 3.8 7 5.6 7 12" fill="none" stroke="url(#cristal)" stroke-width="2.15" stroke-linecap="round" stroke-linejoin="round" filter="url(#brillo)"/>
    <path d="M8.3 7.2h9.4c-.7 3.1-2.5 4.9-4.7 7.1-2.2-2.2-4-4-4.7-7.1Z" fill="url(#arena)" opacity=".58"/>
    <path d="m8.2 26.5 4.8-7.2 4.8 7.2Z" fill="url(#arena)" opacity=".82"/>
    <circle cx="13" cy="16" r="1.05" fill="#eaffff" filter="url(#brillo)"/>
  </svg>`)}`;

@Component({
  selector: 'app-hours-drilldown-chart',
  standalone: true,
  templateUrl: './hours-drilldown-chart.component.html',
  styleUrl: './hours-drilldown-chart.component.css',
})
export class HoursDrilldownChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  readonly localization = inject(LocalizationService);
  @Input({ required: true }) proyectos: DesvioHorasProyecto[] = [];
  @Output() proyectoCambiado = new EventEmitter<DesvioHorasProyecto | null>();
  @Output() tareaSeleccionada = new EventEmitter<{ proyecto: DesvioHorasProyecto; tarea: DesvioHorasTarea }>();
  @ViewChild('chart') private readonly chartElement?: ElementRef<HTMLDivElement>;
  private chart?: echarts.ECharts;
  private resizeObserver?: ResizeObserver;
  protected proyecto: DesvioHorasProyecto | null = null;

  constructor() { effect(() => { this.localization.language(); queueMicrotask(() => this.dibujar()); }); }

  ngAfterViewInit(): void { this.crearGrafico(); }
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['proyectos'] && this.proyecto) this.proyecto = this.proyectos.find((item) => item.idProyecto === this.proyecto?.idProyecto) ?? null;
    if (changes['proyectos'] && this.chart) this.dibujar();
  }
  ngOnDestroy(): void { this.resizeObserver?.disconnect(); this.chart?.dispose(); }

  protected volverAProyectos(): void { this.proyecto = null; this.proyectoCambiado.emit(null); this.dibujar(); }

  protected cantidadElementos(): number { return this.proyecto?.tareas.length ?? this.proyectos.length; }

  protected consumoVisible(): number {
    const estimadas = this.proyecto?.horasEstimadas ?? this.proyectos.reduce((total, proyecto) => total + proyecto.horasEstimadas, 0);
    const reales = this.proyecto?.horasReales ?? this.proyectos.reduce((total, proyecto) => total + proyecto.horasReales, 0);
    return estimadas > 0 ? Math.round(reales / estimadas * 10000) / 100 : 0;
  }

  protected estadoVisible(): string {
    const consumo = this.consumoVisible();
    if (consumo > 100) return this.t('static.exceeded');
    if (consumo >= 80) return this.t('static.inFollowUp');
    return this.t('static.withinExpected');
  }

  private crearGrafico(): void {
    if (!this.chartElement) return;
    this.chart = echarts.init(this.chartElement.nativeElement, undefined, { renderer: 'canvas' });
    this.chart.on('click', (evento: { dataIndex?: number }) => this.seleccionarBarra(evento.dataIndex));
    this.resizeObserver = new ResizeObserver(() => this.chart?.resize());
    this.resizeObserver.observe(this.chartElement.nativeElement);
    this.dibujar();
  }

  private seleccionarBarra(indice?: number): void {
    if (indice === undefined) return;
    if (!this.proyecto) {
      const proyecto = this.proyectos[indice];
      if (!proyecto) return;
      this.proyecto = proyecto; this.proyectoCambiado.emit(proyecto); this.dibujar();
      return;
    }
    const tarea = this.proyecto.tareas[indice];
    if (tarea) this.tareaSeleccionada.emit({ proyecto: this.proyecto, tarea });
  }

  private dibujar(): void {
    if (!this.chart) return;
    const nivelTareas = !!this.proyecto;
    const elementos = nivelTareas ? this.proyecto!.tareas : this.proyectos;
    const nombres = elementos.map((elemento) => nivelTareas ? (elemento as DesvioHorasTarea).titulo : (elemento as DesvioHorasProyecto).nombreProyecto);
    const estimadas = elementos.map((elemento) => elemento.horasEstimadas);
    const reales = elementos.map((elemento) => elemento.horasReales);
    const porcentajes = elementos.map((elemento) => elemento.porcentajeConsumo);
    const maximo = Math.max(1, ...estimadas, ...reales);
    const limiteSuperior = Math.max(5, Math.ceil(maximo * 1.24));
    const alto = Math.max(430, elementos.length * 78 + 150);
    this.chart.resize({ height: alto });
    this.chart.setOption({
      animationDuration: 760, animationDurationUpdate: 480, animationEasing: 'cubicOut',
      backgroundColor: 'transparent',
      grid: { left: 34, right: 70, top: 44, bottom: elementos.length > 8 ? 62 : 42, containLabel: true },
      legend: { show: false },
      tooltip: {
        trigger: 'axis', axisPointer: { type: 'line', lineStyle: { color: 'rgba(114, 218, 239, .42)', type: 'dashed' } }, backgroundColor: '#061c2e', borderColor: '#2b7692', textStyle: { color: '#effcff' }, extraCssText: 'border-radius:12px;padding:14px 16px;line-height:1.6;min-width:210px;box-shadow:0 18px 38px rgba(0,0,0,.32);',
        formatter: (parametros: Array<{ seriesName: string; value: number; dataIndex: number }>) => {
          const indice = parametros[0]?.dataIndex ?? 0; const item = elementos[indice];
          const diferencia = reales[indice] - estimadas[indice]; const porcentaje = estimadas[indice] ? (reales[indice] / estimadas[indice]) * 100 : 0;
          const detalle = nivelTareas ? item as DesvioHorasTarea : item as DesvioHorasProyecto;
          const responsable = nivelTareas ? `<div><b>${this.t('static.responsible')}:</b> ${(detalle as DesvioHorasTarea).responsable || this.t('static.unassigned')}</div>` : `<div><b>${this.t('static.client')}:</b> ${(detalle as DesvioHorasProyecto).nombreCliente}</div>`;
          return `<div><strong style="display:block;margin-bottom:8px;font-size:14px">${nombres[indice]}</strong><div style="display:grid;grid-template-columns:1fr auto;gap:3px 18px;margin:7px 0 9px;color:#c8e7ee"><span>${this.t('static.estimated')}</span><b>${estimadas[indice]} h</b><span>${this.t('static.actual')}</span><b>${reales[indice]} h</b><span>${this.t('static.consumption')}</span><b style="color:${this.colorConsumo(porcentaje)}">${porcentaje.toFixed(2)}%</b><span>${this.t('static.deviation')}</span><b>${diferencia > 0 ? '+' : ''}${diferencia} h</b></div>${responsable}<small style="display:block;margin-top:9px;color:#70e5ff">${this.t('static.clickDrillDown')}</small></div>`;
        },
      },
      xAxis: { type: 'value', min: 0, max: limiteSuperior, splitNumber: 10, minInterval: maximo <= 20 ? 0.25 : 1, name: this.t('static.hours'), nameLocation: 'end', nameGap: 12, nameTextStyle: { color: '#91b9c8', padding: [0, 0, 0, 6] }, axisLabel: { color: '#9fc3cf', formatter: (valor: number) => maximo <= 20 ? Number(valor.toFixed(2)) : Math.round(valor) }, axisTick: { show: true, length: 6, lineStyle: { color: '#4d8497' } }, splitLine: { lineStyle: { color: 'rgba(111, 169, 188, .16)', type: 'dashed' } }, minorTick: { show: true, splitNumber: 4, length: 3 }, minorSplitLine: { show: true, lineStyle: { color: 'rgba(111, 169, 188, .055)' } }, axisLine: { lineStyle: { color: '#34667a' } } },
      yAxis: { type: 'category', inverse: true, boundaryGap: true, data: nombres, axisLabel: { color: '#d8f3f7', width: 205, overflow: 'truncate', margin: 16, fontWeight: 700 }, axisTick: { show: false }, axisLine: { show: false }, splitLine: { show: true, lineStyle: { color: 'rgba(111, 169, 188, .075)' } } },
      dataZoom: elementos.length > 8 ? [{ type: 'inside', yAxisIndex: 0 }, { type: 'slider', yAxisIndex: 0, right: 4, width: 12, borderColor: 'transparent', fillerColor: 'rgba(17, 178, 217, .28)', handleStyle: { color: '#11b2d9' } }] : [],
      series: [
        { name: this.t('static.estimated'), type: 'bar', data: estimadas, barWidth: 22, silent: false, itemStyle: { color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [{ offset: 0, color: 'rgba(26, 112, 139, .38)' }, { offset: 1, color: 'rgba(65, 155, 184, .62)' }]), borderColor: 'rgba(103, 197, 222, .28)', borderWidth: 1, borderRadius: [0, 8, 8, 0] }, emphasis: { focus: 'series', itemStyle: { borderColor: '#70dff6', borderWidth: 1.5 } }, z: 2 },
        { name: this.t('static.actual'), type: 'bar', data: reales.map((valor, indice) => ({ value: valor, itemStyle: { color: this.colorConsumo(porcentajes[indice]), shadowColor: `${this.colorConsumo(porcentajes[indice])}80` } })), barWidth: 11, barGap: '-75%', itemStyle: { borderRadius: [0, 7, 7, 0], shadowBlur: 10 }, label: { show: true, position: 'right', distance: 9, color: '#e9fbff', fontSize: 10, fontWeight: 700, formatter: (item: { dataIndex: number }) => `${reales[item.dataIndex]} / ${estimadas[item.dataIndex]} h · ${porcentajes[item.dataIndex].toFixed(2)}%` }, labelLayout: { hideOverlap: true, moveOverlap: 'shiftY' }, emphasis: { focus: 'series' }, z: 4 },
        { name: this.t('static.estimated'), type: 'scatter', data: estimadas.map((valor, indice) => [valor, indice]), symbol: simboloRelojArena, symbolSize: [22, 28], symbolOffset: [0, 0], emphasis: { scale: 1.3, focus: 'self' }, z: 7 },
      ],
    }, { notMerge: true, lazyUpdate: false });
  }
  protected colorConsumo(porcentaje: number): string {
    if (porcentaje > 100) return '#fb5a70';
    if (porcentaje >= 80) return '#ffb11b';
    return '#2ed7a1';
  }
  private t(key: string): string { return this.localization.traducir(key); }
}

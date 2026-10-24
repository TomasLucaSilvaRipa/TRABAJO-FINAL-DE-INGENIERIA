import { AfterViewInit, Component, ElementRef, Input, OnChanges, OnDestroy, SimpleChanges, ViewChild } from '@angular/core';
import { LineChart, ScatterChart } from 'echarts/charts';
import { AriaComponent, DataZoomComponent, GridComponent, MarkAreaComponent, MarkLineComponent, TooltipComponent } from 'echarts/components';
import * as echarts from 'echarts/core';
import { CanvasRenderer } from 'echarts/renderers';

echarts.use([LineChart, ScatterChart, AriaComponent, DataZoomComponent, GridComponent, MarkAreaComponent, MarkLineComponent, TooltipComponent, CanvasRenderer]);

export interface EscenarioVisualSimulacion {
  idEmpleado: number;
  nombreEmpleado: string;
  nombreTarea: string;
  nombreProyecto: string;
  ocupacionActual: number;
  ocupacionProyectada: number;
  cargaActual: number;
  cargaProyectada: number;
  disponibilidadRestante: number;
  impacto: string;
  conservado: boolean;
}

@Component({
  selector: 'app-simulation-impact-chart',
  standalone: true,
  templateUrl: './simulation-impact-chart.component.html',
  styleUrl: './simulation-impact-chart.component.css',
})
export class SimulationImpactChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @Input({ required: true }) escenarios: EscenarioVisualSimulacion[] = [];
  @ViewChild('chart') private readonly chartElement?: ElementRef<HTMLDivElement>;

  private chart?: echarts.ECharts;
  private resizeObserver?: ResizeObserver;

  ngAfterViewInit(): void { this.inicializar(); }
  ngOnChanges(changes: SimpleChanges): void { if (changes['escenarios'] && this.chart) { this.dibujar(); } }
  ngOnDestroy(): void { this.resizeObserver?.disconnect(); this.chart?.dispose(); }

  private inicializar(): void {
    if (!this.chartElement) { return; }
    this.chart = echarts.init(this.chartElement.nativeElement, undefined, { renderer: 'canvas' });
    this.resizeObserver = new ResizeObserver(() => this.chart?.resize());
    this.resizeObserver.observe(this.chartElement.nativeElement);
    this.dibujar();
  }

  private dibujar(): void {
    if (!this.chart) { return; }
    const nombres = this.escenarios.map(escenario => escenario.nombreEmpleado);
    const maximo = Math.max(120, ...this.escenarios.map(escenario => Math.max(escenario.ocupacionActual, escenario.ocupacionProyectada)));
    const limite = Math.ceil((maximo + Math.max(8, maximo * .08)) / 20) * 20;
    const reducido = globalThis.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
    const conexiones = this.escenarios.map((escenario, indice) => ({
      id: `transicion-${escenario.idEmpleado}`,
      type: 'line' as const,
      data: [[indice, escenario.ocupacionActual], [indice, escenario.ocupacionProyectada]],
      showSymbol: false,
      silent: true,
      lineStyle: { width: 5, color: this.colorPorcentaje(escenario.ocupacionProyectada), shadowBlur: 16, shadowColor: `${this.colorPorcentaje(escenario.ocupacionProyectada)}9a`, opacity: .9 },
      z: 4,
    }));

    this.chart.setOption({
      animation: !reducido,
      animationDuration: 1150,
      animationDurationUpdate: 760,
      animationEasing: 'cubicOut',
      animationEasingUpdate: 'cubicInOut',
      aria: { enabled: true, description: 'Comparación de ocupación actual y proyectada de los candidatos simulados.' },
      grid: { left: 55, right: 42, top: 62, bottom: nombres.length > 6 ? 98 : 78, containLabel: true },
      tooltip: { trigger: 'item', backgroundColor: '#061a2c', borderColor: '#3d8ca9', borderWidth: 1, padding: 14, textStyle: { color: '#effdff' }, extraCssText: 'border-radius:14px;box-shadow:0 22px 52px rgba(0,0,0,.46);min-width:250px;', formatter: (evento: { dataIndex: number; seriesId?: string }) => this.tooltip(evento.dataIndex, evento.seriesId ?? '') },
      xAxis: {
        type: 'category',
        data: nombres,
        boundaryGap: true,
        axisTick: { show: false },
        axisLine: { lineStyle: { color: '#4b7889', width: 2 } },
        axisLabel: { color: '#d9f4fa', interval: 0, width: 132, overflow: 'truncate', rotate: nombres.length > 5 ? 18 : 0, fontWeight: 700, margin: 17 },
      },
      yAxis: {
        type: 'value',
        min: 0,
        max: limite,
        name: 'Ocupación semanal (%)',
        nameTextStyle: { color: '#a6cad6', fontWeight: 700, padding: [0, 0, 6, 8] },
        axisLabel: { color: '#9fc6d1', formatter: '{value}%' },
        axisLine: { lineStyle: { color: '#4b7889' } },
        splitLine: { lineStyle: { color: 'rgba(132,195,214,.16)', type: 'dashed' } },
      },
      dataZoom: nombres.length > 7 ? [{ type: 'inside', xAxisIndex: 0, zoomOnMouseWheel: true, moveOnMouseMove: true }, { type: 'slider', xAxisIndex: 0, height: 13, bottom: 18, borderColor: 'transparent', fillerColor: 'rgba(24,193,225,.27)', backgroundColor: 'rgba(129,194,212,.12)', handleStyle: { color: '#48d7ef', borderColor: '#baf7ff' } }] : [],
      series: [
        {
          id: 'zonas-simulacion', type: 'line', data: [], silent: true, symbol: 'none', lineStyle: { opacity: 0 },
          markArea: { silent: true, itemStyle: { opacity: .14 }, data: [[{ yAxis: 0, itemStyle: { color: '#317cc4' } }, { yAxis: 50 }], [{ yAxis: 50, itemStyle: { color: '#249d75' } }, { yAxis: 86 }], [{ yAxis: 86, itemStyle: { color: '#dc8d1d' } }, { yAxis: 100 }], [{ yAxis: 100, itemStyle: { color: '#d64e60' } }, { yAxis: limite }]] },
          markLine: { silent: true, symbol: 'none', label: { color: '#b8dce5', fontSize: 11, formatter: (parametro: { value: number }) => `${parametro.value}%` }, lineStyle: { type: 'dashed', color: 'rgba(199,235,242,.45)' }, data: [{ yAxis: 50 }, { yAxis: 86 }, { yAxis: 100 }] },
        },
        ...conexiones,
        {
          id: 'ocupacion-actual', type: 'scatter', symbol: 'circle', symbolSize: 19, z: 7,
          data: this.escenarios.map((escenario, indice) => ({ value: [indice, escenario.ocupacionActual], itemStyle: { color: '#6ed8ee', borderColor: '#e9fdff', borderWidth: 2, shadowBlur: 14, shadowColor: 'rgba(74,211,235,.65)' } })),
          label: { show: true, position: 'left', distance: 10, formatter: (evento: { dataIndex: number }) => `{actual|Actual}\n{value|${this.escenarios[evento.dataIndex]?.ocupacionActual.toFixed(1)}%}`, rich: { actual: { color: '#bcebf2', fontSize: 10, fontWeight: 700 }, value: { color: '#ecfeff', fontSize: 11, fontWeight: 800, lineHeight: 17 } } },
          emphasis: { scale: 1.35 },
        },
        {
          id: 'ocupacion-proyectada', type: 'scatter', symbol: 'circle', symbolSize: 29, z: 8,
          data: this.escenarios.map((escenario, indice) => ({ value: [indice, escenario.ocupacionProyectada], itemStyle: { color: 'rgba(7,31,48,.76)', borderColor: this.colorPorcentaje(escenario.ocupacionProyectada), borderWidth: 5, shadowBlur: 28, shadowColor: `${this.colorPorcentaje(escenario.ocupacionProyectada)}cc` } })),
          label: { show: true, position: 'top', distance: 12, formatter: (evento: { dataIndex: number }) => `{projected|Proyectada ${this.escenarios[evento.dataIndex]?.ocupacionProyectada.toFixed(1)}%}${this.escenarios[evento.dataIndex]?.conservado ? '\n{saved|ESCENARIO CONSERVADO}' : ''}`, rich: { projected: { color: '#f0feff', fontWeight: 850, backgroundColor: '#10364a', borderColor: '#3e89a5', borderWidth: 1, borderRadius: 8, padding: [5, 8] }, saved: { color: '#62f0bd', fontSize: 9, fontWeight: 800, lineHeight: 22 } } },
          emphasis: { scale: 1.42, itemStyle: { shadowBlur: 38 } },
        },
      ],
    }, { notMerge: true, lazyUpdate: false });
  }

  private colorPorcentaje(porcentaje: number): string {
    if (porcentaje < 50) { return '#57a6ff'; }
    if (porcentaje < 86) { return '#35d39b'; }
    if (porcentaje <= 100) { return '#ff9f1c'; }
    return '#ff5b70';
  }

  private tooltip(indice: number, serie: string): string {
    const escenario = this.escenarios[indice];
    if (!escenario) { return ''; }
    const escapar = (valor: string) => valor.replace(/[&<>"']/g, caracter => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[caracter] ?? caracter);
    const foco = serie === 'ocupacion-actual' ? 'Carga actual' : 'Carga proyectada';
    return `<div style="line-height:1.55"><strong style="display:block;font-size:15px;margin-bottom:3px">${escapar(escenario.nombreEmpleado)}</strong><span style="display:block;color:#9fc5d2;margin-bottom:8px">${escapar(escenario.nombreProyecto)} · ${escapar(escenario.nombreTarea)}</span><b style="display:block;color:${this.colorPorcentaje(escenario.ocupacionProyectada)};margin-bottom:9px">${escapar(foco)} · ${escapar(escenario.impacto)}</b><div style="border-top:1px solid rgba(159,197,210,.35);padding-top:8px">Actual: <b>${escenario.ocupacionActual.toFixed(2)}%</b> (${escenario.cargaActual.toFixed(2)} h)<br/>Proyectada: <b>${escenario.ocupacionProyectada.toFixed(2)}%</b> (${escenario.cargaProyectada.toFixed(2)} h)<br/>Disponibilidad restante: <b>${escenario.disponibilidadRestante.toFixed(2)} h</b></div></div>`;
  }
}

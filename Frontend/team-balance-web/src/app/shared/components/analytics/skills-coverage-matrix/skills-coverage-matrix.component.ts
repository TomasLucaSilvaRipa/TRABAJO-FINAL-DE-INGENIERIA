import { AfterViewInit, Component, ElementRef, EventEmitter, Input, NgZone, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild, inject } from '@angular/core';
import { HeatmapChart } from 'echarts/charts';
import { AriaComponent, DataZoomComponent, GridComponent, TooltipComponent, VisualMapComponent } from 'echarts/components';
import * as echarts from 'echarts/core';
import { LegacyGridContainLabel } from 'echarts/features';
import { CanvasRenderer } from 'echarts/renderers';
import { AnalisisCoberturaSkill } from '../../../../services/skills-desiertos.service';

echarts.use([HeatmapChart, AriaComponent, DataZoomComponent, GridComponent, TooltipComponent, VisualMapComponent, LegacyGridContainLabel, CanvasRenderer]);

interface CeldaCobertura {
  value: [number, number, number];
  itemStyle: { color: string; borderColor: string; borderWidth: number; borderRadius: number; shadowBlur: number; shadowColor: string };
  etiqueta: string;
}

@Component({
  selector: 'app-skills-coverage-matrix',
  standalone: true,
  templateUrl: './skills-coverage-matrix.component.html',
  styleUrl: './skills-coverage-matrix.component.css',
})
export class SkillsCoverageMatrixComponent implements AfterViewInit, OnChanges, OnDestroy {
  @Input({ required: true }) resultados: AnalisisCoberturaSkill[] = [];
  @Input() cargando = false;
  @Output() skillSeleccionado = new EventEmitter<AnalisisCoberturaSkill>();
  @ViewChild('chart') private readonly chartElement?: ElementRef<HTMLDivElement>;

  private readonly zone = inject(NgZone);
  private chart?: echarts.ECharts;
  private resizeObserver?: ResizeObserver;

  ngAfterViewInit(): void { this.inicializar(); }

  ngOnChanges(changes: SimpleChanges): void {
    if ((changes['resultados'] || changes['cargando']) && this.chart) { this.dibujar(); }
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
    this.chart?.dispose();
  }

  private inicializar(): void {
    if (!this.chartElement) { return; }
    this.chart = echarts.init(this.chartElement.nativeElement, undefined, { renderer: 'canvas' });
    this.chart.on('click', (evento: unknown) => {
      const datos = (evento as { data?: CeldaCobertura | null }).data;
      const indice = datos?.value?.[0];
      if (indice === undefined || !this.resultados[indice]) { return; }
      this.zone.run(() => this.skillSeleccionado.emit(this.resultados[indice]));
    });
    this.resizeObserver = new ResizeObserver(() => this.chart?.resize());
    this.resizeObserver.observe(this.chartElement.nativeElement);
    this.dibujar();
  }

  private dibujar(): void {
    if (!this.chart) { return; }
    const reducido = globalThis.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
    const nombres = this.resultados.map(resultado => resultado.nombreSkill);
    const maximoDemanda = Math.max(...this.resultados.map(resultado => resultado.demandaHoras), 1);
    const maximoCapacidad = Math.max(...this.resultados.map(resultado => resultado.capacidadDisponibleHoras), 1);
    const celdas: CeldaCobertura[] = [];

    this.resultados.forEach((resultado, indice) => {
      const intensidadDemanda = Math.max(.18, Math.min(1, resultado.demandaHoras / maximoDemanda));
      const intensidadCapacidad = Math.max(.16, Math.min(1, resultado.capacidadDisponibleHoras / maximoCapacidad));
      const brecha = Math.max(0, 100 - resultado.porcentajeCobertura);
      celdas.push(this.crearCelda(indice, 0, resultado.demandaHoras, `${resultado.demandaHoras.toFixed(1)} h`, `rgba(40, 190, 235, ${(.18 + intensidadDemanda * .72).toFixed(2)})`, '#35d8ff'));
      celdas.push(this.crearCelda(indice, 1, resultado.capacidadDisponibleHoras, `${resultado.capacidadDisponibleHoras.toFixed(1)} h`, `rgba(53, 211, 155, ${(.14 + intensidadCapacidad * .72).toFixed(2)})`, '#55e9b5'));
      celdas.push(this.crearCelda(indice, 2, brecha, `${resultado.porcentajeCobertura.toFixed(0)}%`, this.colorCriticidad(resultado, .82), this.colorCriticidad(resultado, 1)));
    });

    this.chart.setOption({
      animation: !reducido,
      animationDuration: 900,
      animationDurationUpdate: 650,
      animationEasing: 'cubicOut',
      animationEasingUpdate: 'cubicInOut',
      aria: { enabled: true, description: 'Matriz de demanda, capacidad disponible y cobertura de skills del alcance seleccionado.' },
      visualMap: { show: false, min: 0, max: Math.max(maximoDemanda, maximoCapacidad, 100), dimension: 2, inRange: { color: ['#153f55', '#1bbecf', '#f3bf4c', '#ff5b70'] } },
      grid: { left: 108, right: 26, top: 24, bottom: this.resultados.length > 7 ? 94 : 70, containLabel: false },
      tooltip: {
        trigger: 'item',
        backgroundColor: '#061a2c',
        borderColor: '#3d8ca9',
        borderWidth: 1,
        padding: 14,
        textStyle: { color: '#effdff' },
        extraCssText: 'border-radius:14px;box-shadow:0 22px 52px rgba(0,0,0,.46);min-width:250px;',
        formatter: (evento: { data?: CeldaCobertura }) => this.tooltip(evento.data),
      },
      xAxis: {
        type: 'category',
        data: nombres,
        splitArea: { show: true, areaStyle: { color: ['rgba(8,37,55,.28)', 'rgba(8,50,66,.22)'] } },
        axisTick: { show: false },
        axisLine: { lineStyle: { color: '#477688' } },
        axisLabel: { color: '#d9f4fa', interval: 0, rotate: nombres.length > 6 ? 24 : 0, width: 116, overflow: 'truncate', fontWeight: 700, margin: 14 },
      },
      yAxis: {
        type: 'category',
        data: ['Demanda', 'Capacidad disponible', 'Cobertura'],
        axisTick: { show: false },
        axisLine: { lineStyle: { color: '#477688' } },
        axisLabel: { color: '#bfe4ec', fontWeight: 700, margin: 18 },
        splitArea: { show: true, areaStyle: { color: ['rgba(4,30,46,.45)', 'rgba(7,42,59,.45)'] } },
      },
      dataZoom: nombres.length > 8 ? [{ type: 'inside', xAxisIndex: 0, zoomOnMouseWheel: true, moveOnMouseMove: true }, { type: 'slider', xAxisIndex: 0, height: 13, bottom: 18, borderColor: 'transparent', fillerColor: 'rgba(24,193,225,.27)', backgroundColor: 'rgba(129,194,212,.12)', handleStyle: { color: '#48d7ef', borderColor: '#baf7ff' } }] : [],
      series: [{
        type: 'heatmap',
        data: celdas,
        progressive: 0,
        label: { show: true, color: '#f2feff', fontWeight: 800, formatter: (evento: { data?: CeldaCobertura }) => evento.data?.etiqueta ?? '' },
        emphasis: { itemStyle: { borderColor: '#effdff', borderWidth: 3, shadowBlur: 28, shadowColor: 'rgba(95,225,255,.66)' } },
      }],
    }, { notMerge: true, lazyUpdate: false });
  }

  private crearCelda(indiceSkill: number, indiceFila: number, valor: number, etiqueta: string, color: string, borde: string): CeldaCobertura {
    return { value: [indiceSkill, indiceFila, valor], etiqueta, itemStyle: { color, borderColor: borde, borderWidth: 1.5, borderRadius: 9, shadowBlur: 12, shadowColor: `${borde}55` } };
  }

  private colorCriticidad(resultado: AnalisisCoberturaSkill, opacidad: number): string {
    if (resultado.criticidad === 'Crítica') { return `rgba(255, 91, 112, ${opacidad})`; }
    if (resultado.criticidad === 'Alta') { return `rgba(255, 145, 61, ${opacidad})`; }
    if (resultado.criticidad === 'Media') { return `rgba(246, 190, 61, ${opacidad})`; }
    return `rgba(53, 211, 155, ${opacidad})`;
  }

  private tooltip(celda?: CeldaCobertura): string {
    const indice = celda?.value?.[0];
    const resultado = indice === undefined ? undefined : this.resultados[indice];
    if (!resultado) { return '';
    }
    const escapar = (valor: string) => valor.replace(/[&<>"']/g, caracter => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[caracter] ?? caracter);
    return `<div style="line-height:1.55"><strong style="display:block;font-size:15px;margin-bottom:7px">${escapar(resultado.nombreSkill)}</strong><b style="display:block;color:${this.colorCriticidad(resultado, 1)};margin-bottom:9px">${escapar(resultado.criticidad)} · ${escapar(resultado.tipoBrecha)}</b><div style="border-top:1px solid rgba(159,197,210,.32);padding-top:8px">Demanda: <b>${resultado.demandaHoras.toFixed(1)} h</b><br/>Capacidad disponible: <b>${resultado.capacidadDisponibleHoras.toFixed(1)} h</b><br/>Cobertura: <b>${resultado.porcentajeCobertura.toFixed(1)}%</b><br/>Afecta ${resultado.cantidadTareasAfectadas} tarea(s) en ${resultado.cantidadProyectosAfectados} proyecto(s).</div><small style="display:block;margin-top:10px;color:#6ce5ff">Seleccioná para abrir el detalle operativo</small></div>`;
  }
}

import { AfterViewInit, Component, ElementRef, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild } from '@angular/core';
import * as echarts from 'echarts/core';
import { BarChart } from 'echarts/charts';
import { GridComponent, LegendComponent, TooltipComponent } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import { DesvioHorasProyecto, DesvioHorasTarea } from '../../../../services/analitica-operativa.service';

echarts.use([BarChart, GridComponent, LegendComponent, TooltipComponent, CanvasRenderer]);

@Component({
  selector: 'app-hours-drilldown-chart',
  standalone: true,
  templateUrl: './hours-drilldown-chart.component.html',
  styleUrl: './hours-drilldown-chart.component.css',
})
export class HoursDrilldownChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @Input({ required: true }) proyectos: DesvioHorasProyecto[] = [];
  @Output() proyectoCambiado = new EventEmitter<DesvioHorasProyecto | null>();
  @Output() tareaSeleccionada = new EventEmitter<{ proyecto: DesvioHorasProyecto; tarea: DesvioHorasTarea }>();
  @ViewChild('chart') private readonly chartElement?: ElementRef<HTMLDivElement>;
  private chart?: echarts.ECharts;
  private resizeObserver?: ResizeObserver;
  protected proyecto: DesvioHorasProyecto | null = null;

  ngAfterViewInit(): void { this.crearGrafico(); }
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['proyectos'] && this.proyecto) this.proyecto = this.proyectos.find((item) => item.idProyecto === this.proyecto?.idProyecto) ?? null;
    if (changes['proyectos'] && this.chart) this.dibujar();
  }
  ngOnDestroy(): void { this.resizeObserver?.disconnect(); this.chart?.dispose(); }

  protected volverAProyectos(): void { this.proyecto = null; this.proyectoCambiado.emit(null); this.dibujar(); }

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
    const exceso = elementos.map((elemento) => Math.max(0, elemento.horasReales - elemento.horasEstimadas));
    const alto = Math.max(390, elementos.length * 68 + 125);
    this.chart.resize({ height: alto });
    this.chart.setOption({
      animationDuration: 520, animationDurationUpdate: 420, animationEasing: 'cubicOut',
      backgroundColor: 'transparent',
      grid: { left: 12, right: 36, top: 54, bottom: elementos.length > 8 ? 54 : 18, containLabel: true },
      legend: { top: 10, textStyle: { color: '#b8dbe2' }, data: ['Estimadas', 'Reales'] },
      tooltip: {
        trigger: 'axis', axisPointer: { type: 'shadow' }, backgroundColor: '#061c2e', borderColor: '#2b7692', textStyle: { color: '#effcff' },
        formatter: (parametros: Array<{ seriesName: string; value: number; dataIndex: number }>) => {
          const indice = parametros[0]?.dataIndex ?? 0; const item = elementos[indice];
          const diferencia = reales[indice] - estimadas[indice]; const porcentaje = estimadas[indice] ? (reales[indice] / estimadas[indice]) * 100 : 0;
          const detalle = nivelTareas ? item as DesvioHorasTarea : item as DesvioHorasProyecto;
          const responsable = nivelTareas ? `<br/>Responsable: ${(detalle as DesvioHorasTarea).responsable || 'Sin asignar'}` : `<br/>Cliente: ${(detalle as DesvioHorasProyecto).nombreCliente}`;
          return `<strong>${nombres[indice]}</strong><br/>Estimadas: ${estimadas[indice]} h<br/>Reales: ${reales[indice]} h<br/>Desvío: ${diferencia} h (${porcentaje.toFixed(1)}%)${responsable}<br/><span style="color:#70e5ff">Hacé clic para profundizar</span>`;
        },
      },
      xAxis: { type: 'value', name: 'Horas', nameTextStyle: { color: '#91b9c8', padding: [0, 0, 0, 6] }, axisLabel: { color: '#9fc3cf' }, splitLine: { lineStyle: { color: 'rgba(111, 169, 188, .18)' } }, axisLine: { lineStyle: { color: '#34667a' } } },
      yAxis: { type: 'category', inverse: true, data: nombres, axisLabel: { color: '#d8f3f7', width: 190, overflow: 'truncate' }, axisTick: { show: false }, axisLine: { show: false } },
      dataZoom: elementos.length > 8 ? [{ type: 'inside', yAxisIndex: 0 }, { type: 'slider', yAxisIndex: 0, right: 4, width: 12, borderColor: 'transparent', fillerColor: 'rgba(17, 178, 217, .28)', handleStyle: { color: '#11b2d9' } }] : [],
      series: [
        { name: 'Estimadas', type: 'bar', data: estimadas, barWidth: 31, silent: false, itemStyle: { color: '#1e6078', borderRadius: [0, 7, 7, 0], shadowColor: 'rgba(0,0,0,.25)', shadowBlur: 7 }, emphasis: { focus: 'series' } },
        { name: 'Reales', type: 'bar', data: reales, barWidth: 16, barGap: '-76%', itemStyle: { color: (item: { dataIndex: number }) => exceso[item.dataIndex] > 0 ? '#f59e0b' : '#21d6a1', borderRadius: [0, 7, 7, 0], shadowColor: 'rgba(0,0,0,.34)', shadowBlur: 9 }, emphasis: { focus: 'series' } },
      ],
    }, { notMerge: true, lazyUpdate: false });
  }
}

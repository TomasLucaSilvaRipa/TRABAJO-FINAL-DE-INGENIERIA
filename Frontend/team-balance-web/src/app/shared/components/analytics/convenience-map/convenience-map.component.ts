import { AfterViewInit, Component, effect, ElementRef, EventEmitter, Input, NgZone, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild, inject } from '@angular/core';
import * as echarts from 'echarts/core';
import { BarChart, ScatterChart } from 'echarts/charts';
import { GridComponent, TooltipComponent } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import { ConvenienciaOperativaCliente, ConvenienciaOperativaProyecto } from '../../../../services/analitica-operativa.service';
import { LocalizationService } from '../../../../services/localization.service';

echarts.use([BarChart, ScatterChart, GridComponent, TooltipComponent, CanvasRenderer]);

type ElementoConveniencia = ConvenienciaOperativaCliente | ConvenienciaOperativaProyecto;
type TransicionMapa = 'entrando' | 'preparando-entrada' | 'saliendo' | 'preparando-salida' | null;

@Component({
  selector: 'app-convenience-map',
  standalone: true,
  templateUrl: './convenience-map.component.html',
  styleUrl: './convenience-map.component.css',
})
export class ConvenienceMapComponent implements AfterViewInit, OnChanges, OnDestroy {
  private readonly zone = inject(NgZone);
  protected readonly localization = inject(LocalizationService);
  @Input({ required: true }) clientes: ConvenienciaOperativaCliente[] = [];
  @Input({ required: true }) proyectos: ConvenienciaOperativaProyecto[] = [];
  @Output() proyectoSeleccionado = new EventEmitter<ConvenienciaOperativaProyecto>();
  @Output() clienteRevisionSolicitada = new EventEmitter<ConvenienciaOperativaCliente>();
  @ViewChild('chart') private readonly chartElement?: ElementRef<HTMLDivElement>;

  private chart?: echarts.ECharts;
  private resizeObserver?: ResizeObserver;
  private temporizadorTransicion?: ReturnType<typeof globalThis.setTimeout>;
  protected clienteSeleccionado: ConvenienciaOperativaCliente | null = null;
  protected transicion: TransicionMapa = null;

  constructor() { effect(() => { this.localization.language(); queueMicrotask(() => this.dibujar()); }); }

  ngAfterViewInit(): void { this.inicializar(); }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['clientes'] || changes['proyectos']) {
      this.clienteSeleccionado = null;
      this.transicion = null;
      if (this.chart) this.dibujar();
    }
  }

  ngOnDestroy(): void {
    if (this.temporizadorTransicion) globalThis.clearTimeout(this.temporizadorTransicion);
    this.resizeObserver?.disconnect();
    this.chart?.dispose();
  }

  protected volverAClientes(): void {
    if (!this.clienteSeleccionado || this.transicion) return;
    this.transicion = 'saliendo';
    this.temporizadorTransicion = globalThis.setTimeout(() => {
      this.clienteSeleccionado = null;
      this.dibujar();
      this.transicion = 'preparando-salida';
      requestAnimationFrame(() => requestAnimationFrame(() => this.transicion = null));
    }, 185);
  }

  protected derivarCliente(): void {
    if (this.clienteSeleccionado && !this.clienteSeleccionado.enRevisionOperativa) {
      this.clienteRevisionSolicitada.emit(this.clienteSeleccionado);
    }
  }

  private inicializar(): void {
    if (!this.chartElement) return;
    this.chart = echarts.init(this.chartElement.nativeElement, undefined, { renderer: 'canvas' });
    this.chart.on('click', (evento: { dataIndex?: number; seriesType?: string }) => {
      if (evento.dataIndex === undefined || (evento.seriesType !== 'bar' && evento.seriesType !== 'scatter')) return;
      this.zone.run(() => this.seleccionarElemento(evento.dataIndex!));
    });
    this.resizeObserver = new ResizeObserver(() => this.chart?.resize());
    this.resizeObserver.observe(this.chartElement.nativeElement);
    this.dibujar();
  }

  private seleccionarElemento(indice: number): void {
    if (this.transicion) return;
    if (this.clienteSeleccionado) {
      const proyecto = this.proyectosDelCliente()[indice];
      if (proyecto) this.proyectoSeleccionado.emit(proyecto);
      return;
    }
    const cliente = this.clientes[indice];
    if (!cliente) return;
    this.transicion = 'entrando';
    this.temporizadorTransicion = globalThis.setTimeout(() => {
      this.clienteSeleccionado = cliente;
      this.dibujar();
      this.transicion = 'preparando-entrada';
      requestAnimationFrame(() => requestAnimationFrame(() => this.transicion = null));
    }, 185);
  }

  private proyectosDelCliente(): ConvenienciaOperativaProyecto[] {
    if (!this.clienteSeleccionado) return [];
    const porCliente = this.proyectos.filter(proyecto => proyecto.idCliente === this.clienteSeleccionado!.idCliente);
    return porCliente.length ? porCliente : this.clienteSeleccionado.proyectos;
  }

  protected elementosActuales(): ElementoConveniencia[] {
    return this.clienteSeleccionado ? this.proyectosDelCliente() : this.clientes;
  }

  private indiceSaldo(elemento: ElementoConveniencia): number {
    if (elemento.costoPlanificado <= 0) return 0;
    return Number((((elemento.costoPlanificado - elemento.costoReal) / elemento.costoPlanificado) * 100).toFixed(1));
  }

  private colorEstado(estado: string): string {
    const valor = estado.toLocaleLowerCase();
    if (valor.includes('exced')) return '#ff5b72';
    if (valor.includes('incomplet')) return '#ffb34b';
    if (valor.includes('sin datos')) return '#e8d767';
    return '#35d39b';
  }

  private etiqueta(elemento: ElementoConveniencia): string {
    return 'nombreProyecto' in elemento ? elemento.nombreProyecto : elemento.nombreCliente;
  }

  private escaparHtml(valor: string): string {
    return valor.replace(/[&<>"']/g, caracter => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[caracter] ?? caracter);
  }

  private tooltip(elemento: ElementoConveniencia): string {
    const esCliente = 'proyectos' in elemento;
    const exceso = elemento.sobrecosto > 0
      ? `<span style="display:block;color:#ff91a0;margin-top:7px">${this.localization.traducir('static.costOverrun')}: US$ ${elemento.sobrecosto.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>`
      : `<span style="display:block;color:#7de2ba;margin-top:7px">${this.localization.traducir('static.availableBudget')}: US$ ${elemento.presupuestoNoConsumido.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>`;
    const alcance = esCliente
      ? `${this.localization.traducir('static.projectsIncludedLabel')}: ${(elemento as ConvenienciaOperativaCliente).proyectos.length}`
      : `${this.localization.traducir('static.actualEstimatedHours')}: ${elemento.horasReales} / ${elemento.horasEstimadas} h`;
    const accion = esCliente ? this.localization.traducir('static.clickExploreProjects') : this.localization.traducir('static.clickCostComposition');
    return `<div style="min-width:242px;line-height:1.55"><strong style="display:block;font-size:14px;margin-bottom:4px">${this.escaparHtml(this.etiqueta(elemento))}</strong><span style="display:block;color:#9fc5d2;margin-bottom:9px">${this.escaparHtml(this.localization.traducirTextoEstatico(elemento.estadoConveniencia))}</span><div style="border-top:1px solid rgba(159,197,210,.35);padding-top:8px">${alcance}<br/>${this.localization.traducir('static.costPlanned')}: US$ ${elemento.costoPlanificado.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}<br/>${this.localization.traducir('static.costActual')}: US$ ${elemento.costoReal.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</div>${exceso}<small style="display:block;margin-top:10px;color:#67ddfa">${accion}</small></div>`;
  }

  private dibujar(): void {
    if (!this.chart) return;
    const elementos = this.elementosActuales();
    const nombres = elementos.map(elemento => this.etiqueta(elemento));
    const valores = elementos.map(elemento => this.indiceSaldo(elemento));
    const extremo = Math.max(25, ...valores.map(valor => Math.abs(valor) + 12));
    const alto = Math.max(395, 315 + (nombres.length > 7 ? 50 : 0));
    this.chart.resize({ height: alto });
    this.chart.setOption({
      animation: true,
      animationDuration: 720,
      animationDurationUpdate: 540,
      animationEasing: 'cubicOut',
      animationEasingUpdate: 'cubicInOut',
      grid: { left: 42, right: 36, top: 48, bottom: 82, containLabel: true },
      tooltip: {
        trigger: 'item',
        backgroundColor: '#061b2e', borderColor: '#3285a3', borderWidth: 1, padding: 14,
        textStyle: { color: '#ecfbff' },
        extraCssText: 'border-radius:14px;box-shadow:0 18px 42px rgba(0,0,0,.38);min-width:220px;',
        formatter: (evento: { dataIndex: number }) => elementos[evento.dataIndex] ? this.tooltip(elementos[evento.dataIndex]) : '',
      },
      xAxis: {
        type: 'category', data: nombres,
        axisLabel: { color: '#cfeef5', interval: 0, width: 118, overflow: 'truncate', rotate: nombres.length > 5 ? 20 : 0, fontWeight: 700 },
        axisTick: { show: false }, axisLine: { lineStyle: { color: '#3a7184' } },
      },
      yAxis: {
        type: 'value', min: -extremo, max: extremo,
        name: this.localization.traducir('static.relativePlannedCost'), nameTextStyle: { color: '#9fc3cf', padding: [0, 0, 0, 12] },
        axisLabel: { color: '#9fc3cf', formatter: '{value}%' }, axisLine: { lineStyle: { color: '#3a7184' } },
        splitLine: { lineStyle: { color: 'rgba(118,181,201,.16)', type: 'dashed' } },
      },
      series: [
        {
          type: 'bar', barWidth: 9, z: 1,
          data: elementos.map((elemento, indice) => ({ value: valores[indice], itemStyle: { color: `${this.colorEstado(elemento.estadoConveniencia)}82`, borderRadius: 99 } })),
          emphasis: { itemStyle: { opacity: 1 }, focus: 'series' },
        },
        {
          type: 'scatter', symbol: 'circle', z: 3,
          data: elementos.map((elemento, indice) => ({ value: [indice, valores[indice]], itemStyle: { color: this.colorEstado(elemento.estadoConveniencia), borderColor: '#e8fcff', borderWidth: 2, shadowBlur: 20, shadowColor: `${this.colorEstado(elemento.estadoConveniencia)}9f` } })),
          symbolSize: 26,
          label: { show: true, position: 'top', distance: 9, formatter: (evento: { dataIndex: number }) => `{node|${nombres[evento.dataIndex]}}`, rich: { node: { color: '#effdff', backgroundColor: '#12384a', borderColor: '#3d829a', borderWidth: 1, borderRadius: 8, padding: [4, 7], fontWeight: 700 } } },
          emphasis: { scale: 1.35, focus: 'series' },
        },
      ],
    }, { notMerge: true });
  }
}

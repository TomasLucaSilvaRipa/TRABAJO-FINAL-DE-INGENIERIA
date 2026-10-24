import { AfterViewInit, ChangeDetectorRef, Component, effect, ElementRef, EventEmitter, Input, NgZone, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild, inject } from '@angular/core';
import * as echarts from 'echarts/core';
import { BarChart, ScatterChart } from 'echarts/charts';
import { GridComponent, MarkAreaComponent, MarkLineComponent, TooltipComponent } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import { ConvenienciaOperativaCliente, ConvenienciaOperativaProyecto } from '../../../../services/analitica-operativa.service';
import { LocalizationService } from '../../../../services/localization.service';

echarts.use([BarChart, ScatterChart, GridComponent, MarkAreaComponent, MarkLineComponent, TooltipComponent, CanvasRenderer]);

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
  private readonly changeDetector = inject(ChangeDetectorRef);
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
      this.transicion = 'preparando-salida';
      this.changeDetector.detectChanges();
      this.chart?.clear();
      this.chart?.resize();
      this.dibujar();
      requestAnimationFrame(() => requestAnimationFrame(() => {
        this.transicion = null;
        this.changeDetector.detectChanges();
      }));
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
      this.transicion = 'preparando-entrada';
      this.changeDetector.detectChanges();
      this.chart?.clear();
      this.chart?.resize();
      this.dibujar();
      requestAnimationFrame(() => requestAnimationFrame(() => {
        this.transicion = null;
        this.changeDetector.detectChanges();
      }));
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
    const valorAbsolutoMayor = Math.max(25, ...valores.map(valor => Math.abs(valor)));
    const intervaloVertical = valorAbsolutoMayor <= 100 ? 25 : valorAbsolutoMayor <= 250 ? 50 : Math.ceil(valorAbsolutoMayor / 5 / 25) * 25;
    const extremo = Math.max(50, Math.ceil((valorAbsolutoMayor * 1.12) / intervaloVertical) * intervaloVertical);
    const alto = Math.max(440, 350 + (nombres.length > 7 ? 55 : 0));
    this.chart.resize({ height: alto });
    this.chart.setOption({
      animation: true,
      animationDuration: 720,
      animationDurationUpdate: 540,
      animationEasing: 'cubicOut',
      animationEasingUpdate: 'cubicInOut',
      grid: { left: 58, right: 46, top: 62, bottom: 94, containLabel: true },
      tooltip: {
        trigger: 'item',
        backgroundColor: '#061b2e', borderColor: '#3285a3', borderWidth: 1, padding: 14,
        textStyle: { color: '#ecfbff' },
        extraCssText: 'border-radius:14px;box-shadow:0 18px 42px rgba(0,0,0,.38);min-width:220px;',
        formatter: (evento: { dataIndex: number }) => elementos[evento.dataIndex] ? this.tooltip(elementos[evento.dataIndex]) : '',
      },
      xAxis: {
        type: 'category', data: nombres,
        axisLabel: { color: '#d9f5fa', interval: 0, width: 132, overflow: 'truncate', rotate: nombres.length > 5 ? 18 : 0, fontWeight: 700, margin: 18 },
        axisTick: { show: false }, axisLine: { lineStyle: { color: '#4c8293', width: 2 } },
      },
      yAxis: {
        type: 'value', min: -extremo, max: extremo, interval: intervaloVertical,
        name: this.localization.traducir('static.relativePlannedCost'), nameGap: 20, nameTextStyle: { color: '#b5d5de', fontWeight: 700, padding: [0, 0, 0, 20] },
        axisLabel: { color: '#a9ced7', margin: 13, formatter: (valor: number) => `${valor > 0 ? '+' : ''}${valor}%` },
        axisTick: { show: false }, axisLine: { show: true, lineStyle: { color: '#4c8293' } },
        splitLine: { lineStyle: { color: 'rgba(118,181,201,.2)', type: 'dashed' } },
        splitArea: { show: true, areaStyle: { color: ['rgba(64,151,177,.025)', 'rgba(64,151,177,.055)'] } },
      },
      series: [
        {
          type: 'bar', barWidth: 12, z: 2,
          data: elementos.map((elemento, indice) => {
            const color = this.colorEstado(elemento.estadoConveniencia);
            const positivo = valores[indice] >= 0;
            return { value: valores[indice], itemStyle: { color: new echarts.graphic.LinearGradient(0, positivo ? 1 : 0, 0, positivo ? 0 : 1, [{ offset: 0, color: `${color}45` }, { offset: 1, color }]), borderColor: `${color}a8`, borderWidth: 1, borderRadius: positivo ? [8, 8, 2, 2] : [2, 2, 8, 8], shadowBlur: 12, shadowColor: `${color}42` } };
          }),
          emphasis: { itemStyle: { opacity: 1 }, focus: 'series' },
          markArea: { silent: true, data: [[{ yAxis: 0, itemStyle: { color: 'rgba(53,211,155,.055)' } }, { yAxis: extremo }], [{ yAxis: -extremo, itemStyle: { color: 'rgba(255,91,114,.05)' } }, { yAxis: 0 }]] },
          markLine: { silent: true, symbol: 'none', label: { show: true, position: 'insideEndTop', color: '#aee2ec', formatter: '0%' }, lineStyle: { color: 'rgba(128,220,232,.72)', width: 2 }, data: [{ yAxis: 0 }] },
        },
        {
          type: 'scatter', symbol: 'circle', z: 3,
          data: elementos.map((elemento, indice) => ({ value: [indice, valores[indice]], label: { position: valores[indice] >= 0 ? 'top' : 'bottom' }, itemStyle: { color: this.colorEstado(elemento.estadoConveniencia), borderColor: '#e8fcff', borderWidth: 2, shadowBlur: 22, shadowColor: `${this.colorEstado(elemento.estadoConveniencia)}b8` } })),
          symbolSize: 28,
          label: { show: true, distance: 11, formatter: (evento: { dataIndex: number }) => `{node|${nombres[evento.dataIndex]}}\n{value|${valores[evento.dataIndex] > 0 ? '+' : ''}${valores[evento.dataIndex].toFixed(1)}%}`, rich: { node: { color: '#effdff', backgroundColor: '#12384a', borderColor: '#3d829a', borderWidth: 1, borderRadius: 8, padding: [4, 7], fontWeight: 700 }, value: { color: '#aad4de', fontSize: 11, fontWeight: 700, lineHeight: 19 } } },
          emphasis: { scale: 1.4, focus: 'series', itemStyle: { shadowBlur: 32 } },
        },
      ],
    }, { notMerge: true });
  }
}

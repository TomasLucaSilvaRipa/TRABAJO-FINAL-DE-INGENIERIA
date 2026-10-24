import { AfterViewInit, ChangeDetectorRef, Component, effect, ElementRef, EventEmitter, inject, Input, NgZone, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild } from '@angular/core';
import * as echarts from 'echarts/core';
import { LineChart, ScatterChart } from 'echarts/charts';
import { GridComponent, MarkAreaComponent, TooltipComponent } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import { SobrecalificacionTarea } from '../../../../services/analitica-operativa.service';
import { LocalizationService } from '../../../../services/localization.service';

echarts.use([LineChart, ScatterChart, GridComponent, MarkAreaComponent, TooltipComponent, CanvasRenderer]);

interface NodoEmpleado { nombre: string; tareas: SobrecalificacionTarea[]; nivel: number; horas: number; costo: number; criticidad: string; }

@Component({ selector: 'app-overqualification-map', standalone: true, templateUrl: './overqualification-map.component.html', styleUrl: './overqualification-map.component.css' })
export class OverqualificationMapComponent implements AfterViewInit, OnChanges, OnDestroy {
  private readonly changeDetector = inject(ChangeDetectorRef);
  private readonly zone = inject(NgZone);
  readonly localization = inject(LocalizationService);
  @Input({ required: true }) tareas: SobrecalificacionTarea[] = [];
  @Output() empleadoCambiado = new EventEmitter<NodoEmpleado | null>();
  @Output() tareaSeleccionada = new EventEmitter<SobrecalificacionTarea>();
  @ViewChild('chart') private readonly chartElement?: ElementRef<HTMLDivElement>;
  private chart?: echarts.ECharts;
  private resizeObserver?: ResizeObserver;
  protected empleado: NodoEmpleado | null = null;

  constructor() { effect(() => { this.localization.language(); queueMicrotask(() => this.dibujar()); }); }

  ngAfterViewInit(): void { this.inicializar(); }
  ngOnChanges(changes: SimpleChanges): void { if (changes['tareas']) { this.empleado = null; this.empleadoCambiado.emit(null); if (this.chart) this.dibujar(); } }
  ngOnDestroy(): void { this.resizeObserver?.disconnect(); this.chart?.dispose(); }
  protected volver(): void {
    this.empleado = null;
    this.empleadoCambiado.emit(null);
    this.changeDetector.detectChanges();
    this.chart?.clear();
    this.chart?.resize();
    this.dibujar();
  }

  private inicializar(): void {
    if (!this.chartElement) return;
    this.chart = echarts.init(this.chartElement.nativeElement, undefined, { renderer: 'canvas' });
    this.chart.on('click', (evento: { dataIndex?: number; seriesType?: string }) => {
      if (evento.seriesType !== 'scatter') return;
      this.zone.run(() => this.seleccionar(evento.dataIndex));
    });
    this.resizeObserver = new ResizeObserver(() => this.chart?.resize()); this.resizeObserver.observe(this.chartElement.nativeElement);
    this.dibujar();
  }

  private seleccionar(indice?: number): void {
    if (indice === undefined) return;
    if (!this.empleado) {
      const nodo = this.nodos()[indice];
      if (!nodo) return;
      this.empleado = nodo;
      this.empleadoCambiado.emit(nodo);
      this.changeDetector.detectChanges();
      this.chart?.clear();
      this.chart?.resize();
      this.dibujar();
      return;
    }
    const tarea = this.empleado.tareas[indice]; if (tarea) this.tareaSeleccionada.emit(tarea);
  }

  private nodos(): NodoEmpleado[] {
    const porEmpleado = new Map<string, SobrecalificacionTarea[]>();
    this.tareas.forEach((tarea) => porEmpleado.set(tarea.empleadoAsignado, [...(porEmpleado.get(tarea.empleadoAsignado) ?? []), tarea]));
    return [...porEmpleado.entries()].map(([nombre, tareas]) => {
      const horas = tareas.reduce((total, tarea) => total + tarea.horasAfectadas, 0) || 1;
      const nivel = tareas.reduce((total, tarea) => total + this.deltaSeniority(tarea) * tarea.horasAfectadas, 0) / horas;
      return { nombre, tareas, nivel: Number(nivel.toFixed(2)), horas, costo: tareas.reduce((total, tarea) => total + tarea.impactoOperativoEstimado, 0), criticidad: tareas.map((tarea) => tarea.nivel).sort((a, b) => this.valorCriticidad(b) - this.valorCriticidad(a))[0] ?? 'Leve' };
    }).sort((a, b) => a.nombre.localeCompare(b.nombre));
  }

  private deltaSeniority(tarea: SobrecalificacionTarea): number { return this.valorSeniority(tarea.seniorityAsignado) - this.valorSeniority(tarea.seniorityRequerido); }
  private valorSeniority(valor: string): number { const normalizado = valor.toLowerCase(); return normalizado.includes('senior') && !normalizado.includes('semi') ? 3 : normalizado.includes('semi') ? 2 : 1; }
  private valorCriticidad(valor: string): number { const texto = valor.toLowerCase(); return texto.includes('crít') ? 3 : texto.includes('moder') || texto.includes('alto') ? 2 : 1; }
  private color(nivel: number): string { return nivel >= 2 ? '#ff5b6e' : nivel >= 1 ? '#ffae42' : nivel > 0 ? '#f4df55' : '#32d9a1'; }

  private dibujar(): void {
    if (!this.chart) return;
    const nodos = this.empleado ? this.empleado.tareas.map((tarea) => ({ nombre: tarea.titulo, nivel: this.deltaSeniority(tarea), horas: tarea.horasAfectadas, costo: tarea.impactoOperativoEstimado, criticidad: tarea.nivel, tareas: [tarea] })) : this.nodos();
    const nombres = nodos.map((nodo) => nodo.nombre);
    const valores = nodos.map((nodo) => nodo.nivel);
    const intervaloVertical = .25;
    const valorMayor = Math.max(1, ...valores);
    const maximo = Math.max(1.25, Math.ceil((valorMayor + intervaloVertical * .6) / intervaloVertical) * intervaloVertical);
    const ultimoIndice = Math.max(0, nodos.length - 1);
    const margenHorizontal = nodos.length > 1 ? Math.max(.16, ultimoIndice * .035) : .18;
    const datosLinea: Array<[number, number]> = nodos.length
      ? [[-margenHorizontal, 0], ...valores.map((valor, indice): [number, number] => [indice, valor]), [ultimoIndice + margenHorizontal, valores[ultimoIndice]]]
      : [];
    const alto = Math.max(470, nodos.length * 70 + 175); this.chart.resize({ height: alto });
    this.chart.setOption({
      animationDuration: 850, animationDurationUpdate: 560, animationEasing: 'cubicOut', backgroundColor: 'transparent',
      grid: { left: 58, right: 48, top: 72, bottom: 88, containLabel: true },
      tooltip: { trigger: 'item', backgroundColor: '#061b2e', borderColor: '#3185a4', borderWidth: 1, padding: 14, textStyle: { color: '#edfaff' }, extraCssText: 'box-shadow:0 18px 42px rgba(0,0,0,.36);border-radius:12px;min-width:205px;line-height:1.65;', formatter: (evento: { dataIndex: number }) => { const nodo = nodos[evento.dataIndex]; const tarea = nodo.tareas[0]; const detalle = this.empleado ? `<div><b>${this.t('static.project')}:</b> ${tarea.nombreProyecto}</div><div><b>${this.t('static.required')}:</b> ${tarea.seniorityRequerido}</div><div><b>${this.t('static.assigned')}:</b> ${tarea.seniorityAsignado}</div>` : `<div><b>${this.t('static.affectedTasks')}:</b> ${nodo.tareas.length}</div>`; return `<strong style="display:block;margin-bottom:7px">${nodo.nombre}</strong><div style="display:grid;grid-template-columns:1fr auto;gap:3px 14px"><span>${this.t('static.mismatchLevel')}</span><b>${nodo.nivel.toFixed(2)}</b><span>${this.t('static.affectedHours')}</span><b>${nodo.horas} h</b><span>${this.t('static.impact')}</span><b>US$ ${nodo.costo.toFixed(2)}</b><span>${this.t('static.criticality')}</span><b>${this.localization.traducirTextoEstatico(nodo.criticidad)}</b></div><div style="margin-top:8px;color:#bad8df">${detalle}</div><small style="display:block;margin-top:9px;color:#6ce5ff">${this.t('static.clickDrillDown')}</small>`; } },
      xAxis: [
        { type: 'category', boundaryGap: false, data: nombres, axisLabel: { color: '#d8f3f7', interval: 0, rotate: nodos.length > 6 ? 24 : 0, width: 125, overflow: 'truncate', margin: 18, fontWeight: 700 }, axisLine: { lineStyle: { color: '#4b8295', width: 2 } }, axisTick: { show: false } },
        { type: 'value', min: -margenHorizontal, max: ultimoIndice + margenHorizontal, show: false, axisLine: { show: false }, axisTick: { show: false }, axisLabel: { show: false }, splitLine: { show: false } },
      ],
      yAxis: { type: 'value', min: 0, max: maximo, interval: intervaloVertical, name: this.t('static.fitLevel'), nameGap: 22, nameTextStyle: { color: '#b2d6df', fontWeight: 700, padding: [0, 0, 0, 18] }, axisLabel: { color: '#acd0d9', margin: 13, formatter: (valor: number) => valor.toFixed(2) }, axisTick: { show: false }, axisLine: { show: true, lineStyle: { color: '#4b8295' } }, splitLine: { lineStyle: { color: 'rgba(119,180,201,.2)', type: 'dashed' } }, splitArea: { show: true, areaStyle: { color: ['rgba(60,151,177,.028)', 'rgba(60,151,177,.058)'] } } },
      series: [
        { type: 'line', xAxisIndex: 1, data: datosLinea, silent: true, tooltip: { show: false }, smooth: 0.34, smoothMonotone: 'x', symbol: 'none', lineStyle: { width: 4, color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [{ offset: 0, color: '#22d3ee' }, { offset: .46, color: '#eab308' }, { offset: 1, color: '#fb5a70' }]), shadowBlur: 16, shadowColor: 'rgba(31,196,227,.42)' }, areaStyle: { color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [{ offset: 0, color: 'rgba(255,91,110,.17)' }, { offset: .55, color: 'rgba(234,179,8,.09)' }, { offset: 1, color: 'rgba(34,211,238,.015)' }]) }, markArea: { silent: true, data: [[{ yAxis: 0, itemStyle: { color: 'rgba(50,217,161,.045)' } }, { yAxis: .5 }], [{ yAxis: .5, itemStyle: { color: 'rgba(244,223,85,.045)' } }, { yAxis: 1 }], [{ yAxis: 1, itemStyle: { color: 'rgba(255,174,66,.05)' } }, { yAxis: 1.5 }], [{ yAxis: 1.5, itemStyle: { color: 'rgba(255,91,110,.055)' } }, { yAxis: maximo }]] }, z: 2 },
        { type: 'scatter', xAxisIndex: 1, silent: true, tooltip: { show: false }, data: nodos.map((nodo, indice) => ({ value: [indice, nodo.nivel], symbolSize: 32 + Math.min(18, nodo.horas / 9), itemStyle: { color: 'transparent', borderColor: `${this.color(nodo.nivel)}8f`, borderWidth: nodo.nivel >= 1.5 ? 3 : 2, shadowBlur: 18, shadowColor: `${this.color(nodo.nivel)}8f` } })), symbol: 'circle', z: 3 },
        { type: 'scatter', xAxisIndex: 1, silent: true, tooltip: { show: false }, data: nodos.map((nodo, indice) => ({ value: [indice, nodo.nivel], symbolSize: nodo.nivel >= 1.5 ? 42 + Math.min(18, nodo.horas / 9) : 0, itemStyle: { color: 'transparent', borderColor: `${this.color(nodo.nivel)}52`, borderWidth: 1 } })), symbol: 'circle', z: 3 },
        { type: 'scatter', xAxisIndex: 1, data: nodos.map((nodo, indice) => { const color = this.color(nodo.nivel); return { value: [indice, nodo.nivel], itemStyle: { color: new echarts.graphic.RadialGradient(.38, .32, .9, [{ offset: 0, color: '#f7ffff' }, { offset: .16, color }, { offset: 1, color: `${color}d9` }]), borderColor: '#eaffff', borderWidth: 2, shadowBlur: nodo.nivel >= 2 ? 28 : 18, shadowColor: color }, symbolSize: 22 + Math.min(18, nodo.horas / 9) }; }), symbol: 'circle', label: { show: true, formatter: (evento: { dataIndex: number }) => `{badge|${nombres[evento.dataIndex]}}\n{value|${valores[evento.dataIndex].toFixed(2)}}`, position: 'top', distance: 14, rich: { badge: { color: '#eaffff', backgroundColor: '#12374b', borderColor: '#3c8ca8', borderWidth: 1, borderRadius: 9, padding: [5, 8], fontWeight: 700 }, value: { color: '#b7dce4', fontSize: 11, fontWeight: 800, lineHeight: 20 } } }, emphasis: { scale: 1.35, focus: 'series', label: { show: true }, itemStyle: { shadowBlur: 36 } }, z: 5 },
      ],
    }, { notMerge: true });
  }
  private t(key: string): string { return this.localization.traducir(key); }
}

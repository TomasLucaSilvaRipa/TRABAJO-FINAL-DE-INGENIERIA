import { AfterViewInit, Component, effect, ElementRef, EventEmitter, inject, Input, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild } from '@angular/core';
import * as echarts from 'echarts/core';
import { LineChart, ScatterChart } from 'echarts/charts';
import { GridComponent, TooltipComponent } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import { SobrecalificacionTarea } from '../../../../services/analitica-operativa.service';
import { LocalizationService } from '../../../../services/localization.service';

echarts.use([LineChart, ScatterChart, GridComponent, TooltipComponent, CanvasRenderer]);

interface NodoEmpleado { nombre: string; tareas: SobrecalificacionTarea[]; nivel: number; horas: number; costo: number; criticidad: string; }

@Component({ selector: 'app-overqualification-map', standalone: true, templateUrl: './overqualification-map.component.html', styleUrl: './overqualification-map.component.css' })
export class OverqualificationMapComponent implements AfterViewInit, OnChanges, OnDestroy {
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
  protected volver(): void { this.empleado = null; this.empleadoCambiado.emit(null); this.dibujar(); }

  private inicializar(): void {
    if (!this.chartElement) return;
    this.chart = echarts.init(this.chartElement.nativeElement, undefined, { renderer: 'canvas' });
    this.chart.on('click', (evento: { dataIndex?: number }) => this.seleccionar(evento.dataIndex));
    this.resizeObserver = new ResizeObserver(() => this.chart?.resize()); this.resizeObserver.observe(this.chartElement.nativeElement);
    this.dibujar();
  }

  private seleccionar(indice?: number): void {
    if (indice === undefined) return;
    if (!this.empleado) { const nodo = this.nodos()[indice]; if (!nodo) return; this.empleado = nodo; this.empleadoCambiado.emit(nodo); this.dibujar(); return; }
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
    const nombres = nodos.map((nodo) => nodo.nombre); const valores = nodos.map((nodo) => nodo.nivel); const maximo = Math.max(3, ...valores.map((valor) => Math.abs(valor) + 1));
    const alto = Math.max(430, nodos.length * 70 + 150); this.chart.resize({ height: alto });
    this.chart.setOption({
      animationDuration: 850, animationDurationUpdate: 560, animationEasing: 'cubicOut', backgroundColor: 'transparent',
      grid: { left: 46, right: 42, top: 58, bottom: 75, containLabel: true },
      tooltip: { trigger: 'item', backgroundColor: '#061b2e', borderColor: '#3185a4', borderWidth: 1, padding: 14, textStyle: { color: '#edfaff' }, extraCssText: 'box-shadow:0 18px 42px rgba(0,0,0,.36);border-radius:12px;min-width:205px;line-height:1.65;', formatter: (evento: { dataIndex: number }) => { const nodo = nodos[evento.dataIndex]; const tarea = nodo.tareas[0]; const detalle = this.empleado ? `<div><b>${this.t('static.project')}:</b> ${tarea.nombreProyecto}</div><div><b>${this.t('static.required')}:</b> ${tarea.seniorityRequerido}</div><div><b>${this.t('static.assigned')}:</b> ${tarea.seniorityAsignado}</div>` : `<div><b>${this.t('static.affectedTasks')}:</b> ${nodo.tareas.length}</div>`; return `<strong style="display:block;margin-bottom:7px">${nodo.nombre}</strong><div style="display:grid;grid-template-columns:1fr auto;gap:3px 14px"><span>${this.t('static.mismatchLevel')}</span><b>${nodo.nivel.toFixed(2)}</b><span>${this.t('static.affectedHours')}</span><b>${nodo.horas} h</b><span>${this.t('static.impact')}</span><b>US$ ${nodo.costo.toFixed(2)}</b><span>${this.t('static.criticality')}</span><b>${this.localization.traducirTextoEstatico(nodo.criticidad)}</b></div><div style="margin-top:8px;color:#bad8df">${detalle}</div><small style="display:block;margin-top:9px;color:#6ce5ff">${this.t('static.clickDrillDown')}</small>`; } },
      xAxis: { type: 'category', data: nombres, axisLabel: { color: '#d8f3f7', interval: 0, rotate: nodos.length > 6 ? 28 : 0, width: 115, overflow: 'truncate' }, axisLine: { lineStyle: { color: '#427387' } }, axisTick: { show: false } },
      yAxis: { type: 'value', min: -maximo, max: maximo, interval: 1, name: this.t('static.fitLevel'), nameTextStyle: { color: '#9fc3cf', padding: [0, 0, 0, 10] }, axisLabel: { color: '#9fc3cf' }, axisLine: { lineStyle: { color: '#427387' } }, splitLine: { lineStyle: { color: 'rgba(119,180,201,.16)', type: 'dashed' } } },
      series: [
        { type: 'line', data: valores, smooth: 0.34, symbol: 'none', lineStyle: { width: 4, color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [{ offset: 0, color: '#22d3ee' }, { offset: .55, color: '#eab308' }, { offset: 1, color: '#fb5a70' }]), shadowBlur: 14, shadowColor: 'rgba(31,196,227,.4)' }, emphasis: { lineStyle: { width: 6 } }, z: 2 },
        { type: 'scatter', data: nodos.map((nodo, indice) => ({ value: [indice, nodo.nivel], itemStyle: { color: this.color(nodo.nivel), shadowBlur: nodo.nivel >= 2 ? 22 : 12, shadowColor: this.color(nodo.nivel) }, symbolSize: 18 + Math.min(18, nodo.horas / 9) })), symbol: 'circle', label: { show: true, formatter: (evento: { dataIndex: number }) => `{badge|${nombres[evento.dataIndex]}}`, position: 'top', rich: { badge: { color: '#eaffff', backgroundColor: '#12374b', borderColor: '#3c8ca8', borderWidth: 1, borderRadius: 9, padding: [5, 8], fontWeight: 700 } } }, emphasis: { scale: 1.5, focus: 'series', label: { show: true } }, z: 4 },
      ],
    }, { notMerge: true });
  }
  private t(key: string): string { return this.localization.traducir(key); }
}

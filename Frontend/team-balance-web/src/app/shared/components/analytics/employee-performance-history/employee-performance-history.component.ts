import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, OnDestroy, ViewChild, effect, inject, input, signal } from '@angular/core';
import { LineChart } from 'echarts/charts';
import { AriaComponent, DataZoomComponent, GridComponent, TooltipComponent } from 'echarts/components';
import * as echarts from 'echarts/core';
import { CanvasRenderer } from 'echarts/renderers';
import { PuntoRendimientoEmpleado, ResourcesService } from '../../../../services/resources.service';

echarts.use([LineChart, AriaComponent, DataZoomComponent, GridComponent, TooltipComponent, CanvasRenderer]);
type MetricaRendimiento = 'cumplimiento' | 'desvio';

@Component({ selector: 'app-employee-performance-history', standalone: true, templateUrl: './employee-performance-history.component.html', styleUrl: './employee-performance-history.component.css', changeDetection: ChangeDetectionStrategy.OnPush })
export class EmployeePerformanceHistoryComponent implements AfterViewInit, OnDestroy {
  readonly idUsuario = input.required<number>();
  private readonly resources = inject(ResourcesService);
  @ViewChild('chart') private readonly chartElement?: ElementRef<HTMLDivElement>;
  readonly meses = signal(6);
  readonly metrica = signal<MetricaRendimiento>('cumplimiento');
  readonly puntos = signal<PuntoRendimientoEmpleado[]>([]);
  readonly cargando = signal(false);
  readonly error = signal('');
  private chart?: echarts.ECharts;
  private resizeObserver?: ResizeObserver;

  constructor() { effect(() => this.cargar(this.idUsuario(), this.meses())); effect(() => { this.metrica(); this.dibujar(); }); }
  ngAfterViewInit(): void { if (!this.chartElement) { return; } this.chart = echarts.init(this.chartElement.nativeElement, undefined, { renderer: 'canvas' }); this.resizeObserver = new ResizeObserver(() => this.chart?.resize()); this.resizeObserver.observe(this.chartElement.nativeElement); this.dibujar(); }
  ngOnDestroy(): void { this.resizeObserver?.disconnect(); this.chart?.dispose(); }
  cambiarMeses(meses: number): void { this.meses.set(meses); }
  cambiarMetrica(metrica: MetricaRendimiento): void { this.metrica.set(metrica); }
  promedioCumplimiento(): number { const puntos = this.puntos(); return puntos.length ? Math.round(puntos.reduce((total, punto) => total + punto.cumplimientoPlazos, 0) / puntos.length * 10) / 10 : 0; }
  promedioDesvio(): number { const puntos = this.puntos(); return puntos.length ? Math.round(puntos.reduce((total, punto) => total + punto.desvioHoras, 0) / puntos.length * 10) / 10 : 0; }
  totalReasignaciones(): number { return this.puntos().reduce((total, punto) => total + punto.reasignaciones, 0); }
  totalTareas(): number { return this.puntos().reduce((total, punto) => total + punto.tareasFinalizadas, 0); }
  private cargar(idUsuario: number, meses: number): void { this.cargando.set(true); this.error.set(''); this.resources.consultarRendimientoEmpleado(idUsuario, meses).subscribe({ next: puntos => { this.puntos.set(puntos); this.cargando.set(false); this.dibujar(); }, error: respuesta => { this.cargando.set(false); const error = respuesta as { error?: string }; this.error.set(typeof error.error === 'string' ? error.error : 'No se pudo consultar el rendimiento histórico.'); this.puntos.set([]); this.dibujar(); } }); }
  private dibujar(): void {
    if (!this.chart) { return; }
    const puntos = this.puntos(); const esCumplimiento = this.metrica() === 'cumplimiento'; const valores = puntos.map(punto => esCumplimiento ? punto.cumplimientoPlazos : punto.desvioHoras);
    const reducido = globalThis.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
    this.chart.setOption({ animation: !reducido, animationDuration: 900, aria: { enabled: true }, grid: { left: 52, right: 24, top: 34, bottom: puntos.length > 6 ? 58 : 38, containLabel: true }, tooltip: { trigger: 'axis', backgroundColor: '#061a2c', borderColor: '#2c9fbd', textStyle: { color: '#effdff' }, formatter: (entrada: unknown) => { const lista = entrada as Array<{ dataIndex: number }>; const punto = puntos[lista[0]?.dataIndex ?? 0]; return punto ? `<strong>${this.periodo(punto.periodo)}</strong><br/>Cumplimiento: ${punto.cumplimientoPlazos}%<br/>Desvío: ${punto.desvioHoras}%<br/>Tareas: ${punto.tareasFinalizadas}<br/>Reasignaciones: ${punto.reasignaciones}<br/>Horas: ${punto.horasTrabajadas} / ${punto.horasEstimadas}` : ''; } }, xAxis: { type: 'category', data: puntos.map(punto => this.periodo(punto.periodo)), boundaryGap: false, axisLabel: { color: '#9bc5d0' }, axisLine: { lineStyle: { color: '#3d6f80' } } }, yAxis: { type: 'value', name: esCumplimiento ? 'Cumplimiento (%)' : 'Desvío (%)', min: esCumplimiento ? 0 : undefined, max: esCumplimiento ? 100 : undefined, scale: !esCumplimiento, nameTextStyle: { color: '#86b9c6' }, axisLabel: { color: '#9bc5d0', formatter: '{value}%' }, splitLine: { lineStyle: { color: 'rgba(108,177,195,.13)', type: 'dashed' } } }, dataZoom: puntos.length > 8 ? [{ type: 'inside' }, { type: 'slider', height: 16, bottom: 4 }] : [], series: [{ type: 'line', data: valores, smooth: .35, symbol: 'circle', symbolSize: 10, lineStyle: { width: 4, color: esCumplimiento ? '#34d6ad' : '#ffb347', shadowBlur: 12, shadowColor: esCumplimiento ? '#34d6ad66' : '#ffb34766' }, itemStyle: { color: esCumplimiento ? '#34d6ad' : '#ffb347', borderColor: '#eaffff', borderWidth: 2 }, areaStyle: { color: esCumplimiento ? 'rgba(52,214,173,.12)' : 'rgba(255,179,71,.12)' } }] }, true);
  }
  private periodo(valor: string): string { return new Intl.DateTimeFormat('es-AR', { month: 'short', year: '2-digit', timeZone: 'UTC' }).format(new Date(valor)); }
}

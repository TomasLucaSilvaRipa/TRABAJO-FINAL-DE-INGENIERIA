import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { Proyecto, ProjectsService } from '../../../../services/projects.service';
import { EventoCalendarioEquipo, ResourcesService } from '../../../../services/resources.service';
import { LocalizationService } from '../../../../services/localization.service';

type VistaCalendario = 'semana' | 'mes';
interface FilaCalendario { idEmpleado: number; nombreEmpleado: string; horasSemanales: number; eventos: EventoCalendarioEquipo[]; }

@Component({
  selector: 'app-team-calendar',
  imports: [DatePipe, FormsModule, RouterLink],
  templateUrl: './team-calendar.component.html',
  styleUrl: './team-calendar.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeamCalendar {
  private readonly resources = inject(ResourcesService);
  private readonly projectsService = inject(ProjectsService);
  readonly localization = inject(LocalizationService);
  readonly vista = signal<VistaCalendario>('semana');
  readonly fechaReferencia = signal(this.isoLocal(new Date()));
  readonly idProyecto = signal<number | null>(null);
  readonly idEmpleado = signal<number | null>(null);
  readonly eventos = signal<EventoCalendarioEquipo[]>([]);
  readonly proyectos = signal<Proyecto[]>([]);
  readonly cargando = signal(false);
  readonly error = signal('');
  readonly detalle = signal<EventoCalendarioEquipo | null>(null);
  readonly periodo = computed(() => this.calcularPeriodo(this.fechaReferencia(), this.vista()));
  readonly fechas = computed(() => {
    const periodo = this.periodo(); const inicio = this.desdeIso(periodo.desde); const fin = this.desdeIso(periodo.hasta); const resultado: Date[] = [];
    for (const cursor = new Date(inicio); cursor <= fin; cursor.setDate(cursor.getDate() + 1)) { resultado.push(new Date(cursor)); }
    return resultado;
  });
  readonly filas = computed<FilaCalendario[]>(() => {
    const mapa = new Map<number, FilaCalendario>();
    for (const evento of this.eventos()) {
      const existente = mapa.get(evento.idEmpleado) ?? { idEmpleado: evento.idEmpleado, nombreEmpleado: evento.nombreEmpleado, horasSemanales: evento.horasSemanales, eventos: [] };
      if (evento.idAusencia) { existente.eventos.push(evento); }
      mapa.set(evento.idEmpleado, existente);
    }
    return [...mapa.values()].sort((a, b) => a.nombreEmpleado.localeCompare(b.nombreEmpleado));
  });

  constructor() { this.cargar(true); }

  cargar(cargarProyectos = false): void {
    this.error.set(''); this.cargando.set(true);
    const periodo = this.periodo();
    if (cargarProyectos) {
      this.projectsService.consultar().subscribe({ next: proyectos => this.proyectos.set(proyectos.filter(item => item.activo)), error: respuesta => this.mostrarError(respuesta, false) });
    }
    this.resources.consultarCalendarioEquipo({ fechaDesde: periodo.desde, fechaHasta: periodo.hasta, idProyecto: this.idProyecto(), idEmpleado: this.idEmpleado() })
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({ next: eventos => this.eventos.set(eventos), error: respuesta => this.mostrarError(respuesta) });
  }

  cambiarVista(vista: VistaCalendario): void { this.vista.set(vista); this.detalle.set(null); this.cargar(); }
  cambiarFecha(fecha: string): void { this.fechaReferencia.set(fecha); this.detalle.set(null); this.cargar(); }
  cambiarProyecto(idProyecto: number | null): void { this.idProyecto.set(idProyecto); this.idEmpleado.set(null); this.detalle.set(null); this.cargar(); }
  cambiarEmpleado(idEmpleado: number | null): void { this.idEmpleado.set(idEmpleado); this.detalle.set(null); this.cargar(); }
  moverPeriodo(direccion: number): void { const fecha = this.desdeIso(this.fechaReferencia()); if (this.vista() === 'semana') { fecha.setDate(fecha.getDate() + direccion * 7); } else { fecha.setMonth(fecha.getMonth() + direccion, 1); } this.fechaReferencia.set(this.isoLocal(fecha)); this.detalle.set(null); this.cargar(); }
  eventoEnDia(fila: FilaCalendario, fecha: Date): EventoCalendarioEquipo | null { const clave = this.isoLocal(fecha); return fila.eventos.find(evento => !!evento.fechaInicio && !!evento.fechaFin && evento.fechaInicio.slice(0, 10) <= clave && evento.fechaFin.slice(0, 10) >= clave) ?? null; }
  claseEvento(evento: EventoCalendarioEquipo): string { const estado = (evento.estado ?? '').toLowerCase(); if (estado.includes('pendiente')) { return 'tb-calendar-cell--pending'; } if (estado.includes('rechazada')) { return 'tb-calendar-cell--rejected'; } if ((evento.tipoPeriodo ?? '').toLowerCase().includes('vac')) { return 'tb-calendar-cell--vacation'; } if ((evento.tipoPeriodo ?? '').toLowerCase().includes('enferm')) { return 'tb-calendar-cell--sick'; } return 'tb-calendar-cell--leave'; }
  textoCorto(evento: EventoCalendarioEquipo): string { if ((evento.estado ?? '').toLowerCase().includes('pendiente')) { return 'Pend.'; } if ((evento.estado ?? '').toLowerCase().includes('rechazada')) { return 'Rech.'; } return evento.tipoPeriodo?.slice(0, 5) || 'Aus.'; }
  etiquetaPeriodo(): string { const periodo = this.periodo(); return `${periodo.desde.split('-').reverse().join('/')} — ${periodo.hasta.split('-').reverse().join('/')}`; }
  private calcularPeriodo(fechaIso: string, vista: VistaCalendario): { desde: string; hasta: string } { const fecha = this.desdeIso(fechaIso); if (vista === 'mes') { const desde = new Date(fecha.getFullYear(), fecha.getMonth(), 1); const hasta = new Date(fecha.getFullYear(), fecha.getMonth() + 1, 0); return { desde: this.isoLocal(desde), hasta: this.isoLocal(hasta) }; } const indice = fecha.getDay() === 0 ? 6 : fecha.getDay() - 1; const desde = new Date(fecha); desde.setDate(fecha.getDate() - indice); const hasta = new Date(desde); hasta.setDate(desde.getDate() + 6); return { desde: this.isoLocal(desde), hasta: this.isoLocal(hasta) }; }
  private desdeIso(valor: string): Date { const [anio, mes, dia] = valor.split('-').map(Number); return new Date(anio, mes - 1, dia); }
  private isoLocal(fecha: Date): string { const mes = String(fecha.getMonth() + 1).padStart(2, '0'); const dia = String(fecha.getDate()).padStart(2, '0'); return `${fecha.getFullYear()}-${mes}-${dia}`; }
  private mostrarError(respuesta: unknown, finalizarCarga = true): void { if (finalizarCarga) { this.cargando.set(false); } const posible = respuesta as { error?: string }; this.error.set(typeof posible?.error === 'string' ? posible.error : 'No se pudo cargar el calendario operativo.'); }
}

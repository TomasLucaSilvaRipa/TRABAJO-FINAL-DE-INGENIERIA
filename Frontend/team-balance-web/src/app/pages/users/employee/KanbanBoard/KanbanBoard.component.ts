import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ArchivoAdjuntoTarea, EstadoTarea, Tarea, TasksService } from '../../../../services/tasks.service';
import { forkJoin, map } from 'rxjs';
import { LocalizationService } from '../../../../services/localization.service';
import { FocusTimerService } from '../../../../services/focus-timer.service';

interface ChecklistItem {
  texto: string;
  completada: boolean;
}

interface ComentarioTarea {
  autor: string;
  texto: string;
  fecha: string;
}

@Component({
  selector: 'app-kanban-board',
  imports: [CommonModule, FormsModule],
  templateUrl: './KanbanBoard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class KanbanBoard {
  private readonly tasksService = inject(TasksService);
  private readonly router = inject(Router);
  private readonly foco = inject(FocusTimerService);
  readonly localization = inject(LocalizationService);
  readonly tareasPropias = signal<Tarea[]>([]);
  readonly tareasTablero = signal<Tarea[]>([]);
  readonly cargando = signal(true);
  readonly error = signal('');
  readonly tareaArrastrada = signal<Tarea | null>(null);
  readonly tareaSeleccionada = signal<Tarea | null>(null);
  readonly checklist = signal<ChecklistItem[]>([]);
  readonly comentarios = signal<ComentarioTarea[]>([]);
  readonly nuevoItem = signal('');
  readonly nuevoComentario = signal('');
  readonly estadoSeleccionado = signal('Pendiente');
  readonly motivoBloqueo = signal('');
  readonly guardando = signal(false);
  readonly vistaProyecto = signal(false);
  readonly proyectoGeneral = signal<number | null>(null);
  readonly panelAbierto = signal(false);
  readonly estados = signal<EstadoTarea[]>([]);
  readonly adjuntosReales = signal<Array<{ archivo: ArchivoAdjuntoTarea; url: string }>>([]);
  readonly subiendoAdjunto = signal(false);
  private cierrePanel: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    this.tasksService.consultarEstados().subscribe({ next: estados => this.estados.set(estados), error: () => this.estados.set([]) });
    this.cargarTableroPersonal();
  }

  columnas(): string[] { return this.estados().length ? this.estados().map(estado => estado.nombre) : ['Pendiente', 'En progreso', 'Bloqueada', 'Finalizada']; }

  tareasPorEstado(estado: string): Tarea[] {
    return this.tareasMostradas()
      .filter(tarea => (tarea.estado || 'Pendiente').toLowerCase() === estado.toLowerCase())
      .sort((primera, segunda) => this.ordenarPorPrioridad(primera, segunda));
  }

  tareasMostradas(): Tarea[] {
    return this.vistaProyecto() ? this.tareasTablero() : this.tareasPropias();
  }

  esPropia(tarea: Tarea): boolean {
    return this.tareasPropias().some(item => item.id === tarea.id);
  }

  abrirTarea(tarea: Tarea, estadoDestino?: string): void {
    if (this.cierrePanel) {
      clearTimeout(this.cierrePanel);
      this.cierrePanel = null;
    }
    this.error.set('');
    this.tareaSeleccionada.set(tarea);
    this.checklist.set(this.leerChecklist(tarea.checklistJson));
    this.comentarios.set(this.leerComentarios(tarea.comentariosJson));
    this.nuevoItem.set('');
    this.nuevoComentario.set('');
    this.estadoSeleccionado.set(estadoDestino || tarea.estado || 'Pendiente');
    this.motivoBloqueo.set(tarea.motivoBloqueo || '');
    this.cargarAdjuntos(tarea.id);
    requestAnimationFrame(() => this.panelAbierto.set(true));
  }

  cerrarTarea(): void {
    this.adjuntosReales().forEach(item => URL.revokeObjectURL(item.url)); this.adjuntosReales.set([]);
    this.panelAbierto.set(false);
    if (this.cierrePanel) {
      clearTimeout(this.cierrePanel);
    }
    this.cierrePanel = setTimeout(() => {
      this.tareaSeleccionada.set(null);
      this.cierrePanel = null;
    }, 180);
  }

  abrirTableroProyecto(tarea: Tarea): void {
    this.tasksService.consultarTableroProyectoPropio(tarea).subscribe({
      next: tareas => {
        this.tareasTablero.set(tareas);
        this.proyectoGeneral.set(tarea.idProyecto);
        this.vistaProyecto.set(true);
        this.cerrarTarea();
      },
      error: error => this.error.set(this.obtenerMensajeError(error, 'No se pudo cargar el tablero general del proyecto.')),
    });
  }

  volverAMiTablero(): void {
    this.vistaProyecto.set(false);
    this.proyectoGeneral.set(null);
    this.cerrarTarea();
  }

  iniciarArrastre(tarea: Tarea): void {
    if (this.esPropia(tarea)) {
      this.tareaArrastrada.set(tarea);
    }
  }

  soltarEnEstado(estado: string): void {
    const tarea = this.tareaArrastrada();
    this.tareaArrastrada.set(null);
    if (!tarea || tarea.estado === estado) {
      return;
    }
    if (estado === 'Bloqueada') {
      this.abrirTarea(tarea, estado);
      return;
    }
    this.actualizarAvance(tarea, estado, this.leerChecklist(tarea.checklistJson), tarea.motivoBloqueo || '');
  }

  agregarChecklist(): void {
    const texto = this.nuevoItem().trim();
    if (!texto) {
      this.error.set('Escribí una subactividad antes de agregarla.');
      return;
    }
    this.checklist.update(items => [...items, { texto, completada: false }]);
    this.nuevoItem.set('');
    this.error.set('');
    this.actualizarPorChecklist();
  }

  alternarChecklist(indice: number): void {
    this.checklist.update(items => items.map((item, posicion) => posicion === indice ? { ...item, completada: !item.completada } : item));
    this.actualizarPorChecklist();
  }

  eliminarChecklist(indice: number): void {
    this.checklist.update(items => items.filter((_, posicion) => posicion !== indice));
    this.actualizarPorChecklist();
  }

  porcentajeChecklist(): number {
    const items = this.checklist();
    if (!items.length) {
      return this.tareaSeleccionada()?.porcentajeAvance || 0;
    }
    return Math.round((items.filter(item => item.completada).length / items.length) * 100);
  }

  obtenerAdjuntos(tarea: Tarea): string[] {
    if (!tarea.archivosAdjuntosJson) {
      return [];
    }
    try {
      const adjuntos = JSON.parse(tarea.archivosAdjuntosJson);
      return Array.isArray(adjuntos) ? adjuntos.filter((adjunto: unknown): adjunto is string => typeof adjunto === 'string' && /^https?:\/\//i.test(adjunto)) : [];
    }
    catch {
      return [];
    }
  }

  descargarAdjunto(item: { archivo: ArchivoAdjuntoTarea; url: string }): void { const enlace = document.createElement('a'); enlace.href = item.url; enlace.download = item.archivo.nombreOriginal; enlace.click(); }
  async adjuntarArchivos(evento: Event): Promise<void>
  {
    const tarea = this.tareaSeleccionada(); const input = evento.target as HTMLInputElement; if (!tarea || !this.esPropia(tarea)) return;
    const seleccionados = Array.from(input.files ?? []); input.value = ''; if (!seleccionados.length) return;
    if (seleccionados.some(archivo => archivo.size > 25 * 1024 * 1024)) { this.error.set('Cada archivo debe pesar como máximo 25 MB.'); return; }
    this.subiendoAdjunto.set(true); const preparados: File[] = []; for (const archivo of seleccionados) preparados.push(await this.optimizarImagen(archivo));
    forkJoin(preparados.map(archivo => this.tasksService.subirAdjunto(tarea.id, archivo))).subscribe({ next: () => { this.subiendoAdjunto.set(false); this.cargarAdjuntos(tarea.id); }, error: error => { this.subiendoAdjunto.set(false); this.error.set(this.obtenerMensajeError(error, 'No se pudieron cargar los adjuntos.')); } });
  }
  private cargarAdjuntos(idTarea: number): void
  {
    this.tasksService.consultarAdjuntos(idTarea).subscribe({ next: archivos => {
      if (!archivos.length) { this.adjuntosReales.set([]); return; }
      forkJoin(archivos.map(archivo => this.tasksService.descargarAdjunto(idTarea, archivo.id).pipe(map(blob => ({ archivo, url: URL.createObjectURL(blob) }))))).subscribe({ next: items => this.adjuntosReales.set(items), error: () => this.adjuntosReales.set([]) });
    }, error: () => this.adjuntosReales.set([]) });
  }
  private async optimizarImagen(archivo: File): Promise<File>
  {
    if (!archivo.type.startsWith('image/') || archivo.type === 'image/gif') return archivo;
    const bitmap = await createImageBitmap(archivo); const escala = Math.min(1, 1800 / Math.max(bitmap.width, bitmap.height)); const canvas = document.createElement('canvas'); canvas.width = Math.max(1, Math.round(bitmap.width * escala)); canvas.height = Math.max(1, Math.round(bitmap.height * escala)); canvas.getContext('2d')!.drawImage(bitmap, 0, 0, canvas.width, canvas.height); bitmap.close();
    const blob = await new Promise<Blob | null>(resolve => canvas.toBlob(resolve, 'image/webp', .82)); return blob && blob.size < archivo.size ? new File([blob], archivo.name.replace(/\.[^.]+$/, '') + '.webp', { type: 'image/webp' }) : archivo;
  }

  guardarAvance(): void {
    const tarea = this.tareaSeleccionada();
    if (!tarea || !this.esPropia(tarea)) {
      return;
    }
    const estado = this.estadoSeleccionado();
    const motivo = this.motivoBloqueo().trim();
    if (estado === 'Bloqueada' && !motivo) {
      this.error.set('Indicá el motivo del bloqueo para avisar al responsable del proyecto.');
      return;
    }
    this.actualizarAvance(tarea, estado, this.checklist(), motivo, true);
  }

  guardarComentario(): void {
    const tarea = this.tareaSeleccionada();
    const mensaje = this.nuevoComentario().trim();
    if (!tarea || !this.esPropia(tarea) || !mensaje) {
      return;
    }
    const comentarios = [...this.comentarios(), { autor: 'Yo', texto: mensaje, fecha: new Date().toISOString() }];
    this.guardando.set(true);
    this.tasksService.guardarComentarios({ ...tarea, comentariosJson: JSON.stringify(comentarios) }).subscribe({
      next: () => {
        this.guardando.set(false);
        this.comentarios.set(comentarios);
        this.nuevoComentario.set('');
        this.actualizarListaLocal({ ...tarea, comentariosJson: JSON.stringify(comentarios) });
      },
      error: error => {
        this.guardando.set(false);
        this.error.set(this.obtenerMensajeError(error, 'No se pudo guardar el comentario.'));
      },
    });
  }

  iniciarFoco(tarea: Tarea): void {
    this.foco.iniciar(tarea);
    this.cerrarTarea();
    this.router.navigate(['/dashboard']);
  }

  private cargarTableroPersonal(): void {
    this.tasksService.consultarMias().subscribe({
      next: tareas => {
        this.tareasPropias.set(tareas);
        this.cargando.set(false);
      },
      error: error => {
        this.error.set(this.obtenerMensajeError(error, 'No se pudo cargar el tablero personal.'));
        this.cargando.set(false);
      },
    });
  }

  private actualizarPorChecklist(): void {
    const tarea = this.tareaSeleccionada();
    if (!tarea || !this.esPropia(tarea)) {
      return;
    }
    this.error.set('');
  }

  private actualizarAvance(tarea: Tarea, estado: string, checklist: ChecklistItem[], motivoBloqueo: string, cerrar = false): void {
    if (estado === 'Bloqueada' && !motivoBloqueo) {
      this.abrirTarea(tarea, estado);
      this.error.set('Indicá el motivo del bloqueo para continuar.');
      return;
    }
    const porcentajeAvance = checklist.length ? Math.round((checklist.filter(item => item.completada).length / checklist.length) * 100) : tarea.porcentajeAvance;
    this.guardando.set(true);
    this.error.set('');
    const estadoConfigurado = this.estados().find(item => item.nombre.toLocaleLowerCase() === estado.toLocaleLowerCase());
    this.tasksService.actualizarAvancePropio({ ...tarea, idEstadoTarea: estadoConfigurado?.id ?? tarea.idEstadoTarea, estado, checklistJson: JSON.stringify(checklist), porcentajeAvance, motivoBloqueo: motivoBloqueo || null }).subscribe({
      next: resultado => {
        this.guardando.set(false);
        this.actualizarListaLocal(resultado);
        this.tareaSeleccionada.set(resultado);
        this.checklist.set(this.leerChecklist(resultado.checklistJson));
        if (cerrar) {
          this.cerrarTarea();
        }
      },
      error: error => {
        this.guardando.set(false);
        this.error.set(this.obtenerMensajeError(error, 'No se pudo actualizar el avance de la tarea.'));
      },
    });
  }

  private actualizarListaLocal(tarea: Tarea): void {
    this.tareasPropias.update(tareas => tareas.map(item => item.id === tarea.id ? tarea : item));
    this.tareasTablero.update(tareas => tareas.map(item => item.id === tarea.id ? tarea : item));
  }

  private ordenarPorPrioridad(primera: Tarea, segunda: Tarea): number {
    const peso = (tarea: Tarea): number => ({ Alta: 3, Media: 2, Baja: 1 }[tarea.prioridad || ''] || 0);
    const diferenciaPrioridad = peso(segunda) - peso(primera);
    if (diferenciaPrioridad !== 0) {
      return diferenciaPrioridad;
    }

    const deadlinePrimera = primera.deadline ? new Date(primera.deadline).getTime() : Number.MAX_SAFE_INTEGER;
    const deadlineSegunda = segunda.deadline ? new Date(segunda.deadline).getTime() : Number.MAX_SAFE_INTEGER;
    return deadlinePrimera - deadlineSegunda;
  }

  private leerChecklist(checklistJson?: string | null): ChecklistItem[] {
    if (!checklistJson) {
      return [];
    }
    try {
      const items = JSON.parse(checklistJson);
      return Array.isArray(items) ? items.filter(item => typeof item?.texto === 'string').map(item => ({ texto: item.texto, completada: Boolean(item.completada) })) : [];
    }
    catch {
      return [];
    }
  }

  private leerComentarios(comentariosJson?: string | null): ComentarioTarea[] {
    if (!comentariosJson) {
      return [];
    }
    try {
      const comentarios = JSON.parse(comentariosJson);
      return Array.isArray(comentarios) ? comentarios.filter(comentario => typeof comentario?.texto === 'string') : [];
    }
    catch {
      return [];
    }
  }

  private obtenerMensajeError(error: any, mensajePredeterminado: string): string {
    if (typeof error?.error === 'string') {
      return error.error;
    }
    return error?.error?.message || error?.error?.title || mensajePredeterminado;
  }
}

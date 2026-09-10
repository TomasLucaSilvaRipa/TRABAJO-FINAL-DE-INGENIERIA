import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { AuthService } from '../../../../services/auth.service';
import { Tarea, TasksService } from '../../../../services/tasks.service';

interface ComentarioTarea { autor: string; fecha: string; texto: string; }

@Component({
  selector: 'app-task-detail.component',
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './TaskDetail.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskDetailComponent {
  private readonly tasksService = inject(TasksService); private readonly route = inject(ActivatedRoute); private readonly authService = inject(AuthService); private readonly formBuilder = inject(FormBuilder);
  readonly tarea = signal<Tarea | null>(null); readonly comentarios = signal<ComentarioTarea[]>([]); readonly adjuntos = signal<string[]>([]); readonly cargando = signal(true); readonly error = signal(''); readonly guardando = signal(false);
  readonly comentarioForm = this.formBuilder.group({ texto: ['', [Validators.required, Validators.maxLength(1000)]] });
  constructor() { const id = Number(this.route.snapshot.queryParamMap.get('id')); this.tasksService.consultarMias().subscribe({ next: (tareas: Tarea[]) => { const tarea = tareas.find((item: Tarea) => item.id === id) ?? null; this.tarea.set(tarea); this.comentarios.set(this.leerComentarios(tarea?.comentariosJson)); this.adjuntos.set(this.leerAdjuntos(tarea?.archivosAdjuntosJson)); this.cargando.set(false); }, error: () => { this.error.set('No se pudo cargar el detalle de la tarea.'); this.cargando.set(false); } }); }
  guardarComentario(): void { const tarea = this.tarea(); if (!tarea || this.comentarioForm.invalid) { return; } const texto = this.comentarioForm.controls.texto.value?.trim() ?? ''; const usuario = this.authService.usuarioActual(); const comentario: ComentarioTarea = { autor: usuario ? `${usuario.nombre} ${usuario.apellido}` : 'Usuario TeamBalance', fecha: new Date().toISOString(), texto }; const comentarios = [...this.comentarios(), comentario]; this.guardando.set(true); this.tasksService.guardarComentarios(tarea.id, JSON.stringify(comentarios)).subscribe({ next: () => { this.comentarios.set(comentarios); this.comentarioForm.reset(); this.guardando.set(false); }, error: () => { this.error.set('No se pudo guardar el comentario.'); this.guardando.set(false); } }); }
  private leerComentarios(json: string | null | undefined): ComentarioTarea[] { if (!json) { return []; } try { const comentarios = JSON.parse(json) as ComentarioTarea[]; return Array.isArray(comentarios) ? comentarios : []; } catch { return []; } }
  private leerAdjuntos(json: string | null | undefined): string[] { if (!json) { return []; } try { const adjuntos = JSON.parse(json); return Array.isArray(adjuntos) ? adjuntos.filter((adjunto: unknown): adjunto is string => typeof adjunto === 'string') : []; } catch { return []; } }
}

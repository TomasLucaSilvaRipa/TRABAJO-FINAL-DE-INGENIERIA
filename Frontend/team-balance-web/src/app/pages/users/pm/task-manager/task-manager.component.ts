import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Skill } from '../../../../services/resources.service';
import { Tarea, TareaOpciones, TasksService } from '../../../../services/tasks.service';
import { LocalizationService } from '../../../../services/localization.service';

@Component({ selector: 'app-task-manager.component', imports: [ReactiveFormsModule], templateUrl: './task-manager.component.html' })
export class TaskManagerComponent {
  private readonly service = inject(TasksService); private readonly fb = inject(FormBuilder);
  readonly localization = inject(LocalizationService);
  readonly tareas = signal<Tarea[]>([]); readonly opciones = signal<TareaOpciones>({ proyectos: [], estados: [], empleados: [], skills: [] }); readonly mostrar = signal(false); readonly mostrarSkill = signal(false); readonly error = signal('');
  readonly form = this.fb.group({ id: [0], idProyecto: [0, Validators.min(1)], idEstadoTarea: [0, Validators.min(1)], idEmpleadoAsignado: [null as number | null], idSkillRequerido: [0, Validators.min(1)], titulo: ['', Validators.required], descripcion: [''], prioridad: ['Media'], complejidad: ['Media'], fechaInicio: [''], deadline: [''], seniorityRequerido: [''], porcentajeAvance: [0], horasEstimadas: [0], archivosAdjuntosJson: [''] });
  readonly skillForm = this.fb.group({ nombre: ['', Validators.required], categoria: [''] });
  constructor() { this.cargar(); }
  cargar(): void { this.service.consultar().subscribe((tareas: Tarea[]) => this.tareas.set(tareas)); this.service.opciones().subscribe((opciones: TareaOpciones) => this.opciones.set(opciones)); }
  nuevo(): void { this.form.reset({ id: 0, idProyecto: 0, idEstadoTarea: 0, idEmpleadoAsignado: null, idSkillRequerido: 0, titulo: '', descripcion: '', prioridad: 'Media', complejidad: 'Media', fechaInicio: '', deadline: '', seniorityRequerido: '', porcentajeAvance: 0, horasEstimadas: 0, archivosAdjuntosJson: '' }); this.error.set(''); this.mostrar.set(true); }
  editar(tarea: Tarea): void { this.form.patchValue({ ...tarea, archivosAdjuntosJson: this.obtenerUrlsAdjuntas(tarea.archivosAdjuntosJson).join('\n') }); this.mostrar.set(true); }
  guardar(): void { if (this.form.invalid) { this.error.set('Completá título, proyecto, skill y estado.'); return; } const datos = this.form.getRawValue(); const urls = this.obtenerUrlsAdjuntas(datos.archivosAdjuntosJson); if (urls.length > 5 || urls.some(url => !this.esUrlImagen(url))) { this.error.set('Indicá hasta 5 URLs válidas de imágenes externas.'); return; } const tarea = { ...datos, archivosAdjuntosJson: urls.length === 0 ? null : JSON.stringify(urls) } as unknown as Partial<Tarea>; this.service.guardar(tarea).subscribe({ next: () => { this.mostrar.set(false); this.cargar(); }, error: () => this.error.set('No se pudo guardar la tarea.') }); }
  abrirSkills(): void { this.error.set(''); this.mostrarSkill.set(true); }
  crearSkill(): void { if (this.skillForm.invalid) { return; } const datos = this.skillForm.getRawValue(); this.service.crearSkill({ nombre: datos.nombre ?? '', categoria: datos.categoria ?? '' }).subscribe({ next: (skill: Skill) => { this.skillForm.reset(); this.mostrarSkill.set(false); this.service.opciones().subscribe((opciones: TareaOpciones) => { this.opciones.set(opciones); this.form.controls.idSkillRequerido.setValue(skill.id); }); }, error: () => this.error.set('No se pudo crear la skill.') }); }
  cambiarEstadoSkill(skill: Skill): void { this.service.cambiarEstadoSkill(skill.id, !skill.activo).subscribe({ next: () => this.service.opciones().subscribe((opciones: TareaOpciones) => this.opciones.set(opciones)), error: () => this.error.set('No se pudo actualizar la skill.') }); }
  cambiar(tarea: Tarea): void { this.service.cambiarEstado(tarea.id, !tarea.activo).subscribe(() => this.cargar()); }
  private obtenerUrlsAdjuntas(archivosAdjuntosJson?: string | null): string[] { if (!archivosAdjuntosJson) { return []; } try { const archivos = JSON.parse(archivosAdjuntosJson); return Array.isArray(archivos) ? archivos.filter((archivo: unknown): archivo is string => typeof archivo === 'string') : archivosAdjuntosJson.split(/\r?\n/).map(url => url.trim()).filter(Boolean); } catch { return archivosAdjuntosJson.split(/\r?\n/).map(url => url.trim()).filter(Boolean); } }
  private esUrlImagen(url: string): boolean { try { const direccion = new URL(url); return direccion.protocol === 'https:' || direccion.protocol === 'http:'; } catch { return false; } }
}

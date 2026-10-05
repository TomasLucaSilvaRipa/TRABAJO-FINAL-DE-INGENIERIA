import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Skill } from '../../../../services/resources.service';
import { PlantillaTarea, TaskTemplatesService } from '../../../../services/task-templates.service';
import { Tarea, TareaOpciones, TasksService } from '../../../../services/tasks.service';

interface ChecklistItem {
  texto: string;
  completada: boolean;
}

@Component({
  selector: 'app-task-templates',
  imports: [DatePipe, ReactiveFormsModule],
  templateUrl: './task-templates.component.html',
})
export class TaskTemplatesComponent {
  private readonly service = inject(TaskTemplatesService);
  private readonly tasksService = inject(TasksService);
  private readonly fb = inject(FormBuilder);

  readonly plantillas = signal<PlantillaTarea[]>([]);
  readonly tareasBase = signal<Tarea[]>([]);
  readonly skills = signal<Skill[]>([]);
  readonly mostrarFormulario = signal(false);
  readonly mostrarDesdeTarea = signal(false);
  readonly mostrarBaja = signal<PlantillaTarea | null>(null);
  readonly error = signal('');
  readonly mensaje = signal('');
  readonly checklist = signal<ChecklistItem[]>([]);
  readonly nuevoChecklist = signal('');

  readonly form = this.fb.group({
    id: [0],
    nombre: ['', [Validators.required, Validators.maxLength(150)]],
    tituloSugerido: ['', [Validators.required, Validators.maxLength(150)]],
    descripcionBase: [''],
    horasEstimadas: [0, [Validators.required, Validators.min(0.25)]],
    complejidad: ['Media'],
    prioridadSugerida: ['Media'],
    idSkillRequerido: [0, Validators.min(1)],
    seniorityRecomendado: [''],
    archivosAdjuntosJson: [''],
  });

  readonly bajaForm = this.fb.group({ motivoBaja: ['', [Validators.required, Validators.maxLength(500)] ] });

  constructor() {
    this.cargar();
  }

  cargar(): void {
    this.service.consultar().subscribe({
      next: (plantillas: PlantillaTarea[]) => this.plantillas.set(plantillas),
      error: (respuesta) => this.error.set(this.obtenerError(respuesta, 'No se pudieron cargar las plantillas.')),
    });
    this.service.consultarTareasBase().subscribe({ next: (tareas: Tarea[]) => this.tareasBase.set(tareas) });
    this.tasksService.opciones().subscribe({ next: (opciones: TareaOpciones) => this.skills.set(opciones.skills.filter((skill: Skill) => skill.activo)) });
  }

  nueva(): void {
    this.form.reset({ id: 0, nombre: '', tituloSugerido: '', descripcionBase: '', horasEstimadas: 0, complejidad: 'Media', prioridadSugerida: 'Media', idSkillRequerido: 0, seniorityRecomendado: '', archivosAdjuntosJson: '' });
    this.checklist.set([]);
    this.nuevoChecklist.set('');
    this.limpiarMensajes();
    this.mostrarFormulario.set(true);
  }

  editar(plantilla: PlantillaTarea): void {
    this.form.patchValue({ ...plantilla, archivosAdjuntosJson: this.urls(plantilla.archivosAdjuntosJson).join('\n') });
    this.checklist.set(this.obtenerChecklist(plantilla.checklistBaseJson));
    this.nuevoChecklist.set('');
    this.limpiarMensajes();
    this.mostrarFormulario.set(true);
  }

  abrirDesdeTarea(): void {
    this.limpiarMensajes();
    this.mostrarDesdeTarea.set(true);
  }

  seleccionarTareaBase(tarea: Tarea): void {
    this.service.crearDesdeTarea(tarea).subscribe({
      next: (plantilla: PlantillaTarea) => {
        this.form.patchValue({ ...plantilla, id: 0, archivosAdjuntosJson: this.urls(plantilla.archivosAdjuntosJson).join('\n') });
        this.checklist.set(this.obtenerChecklist(plantilla.checklistBaseJson));
        this.mostrarDesdeTarea.set(false);
        this.mostrarFormulario.set(true);
      },
      error: (respuesta) => this.error.set(this.obtenerError(respuesta, 'No se pudo usar la tarea como base.')),
    });
  }

  guardar(): void {
    if (this.form.invalid) {
      this.error.set('Completá nombre, título sugerido, horas estimadas y skill requerida.');
      return;
    }

    const valores = this.form.getRawValue();
    const urls = this.urls(valores.archivosAdjuntosJson);
    if (urls.length > 5 || urls.some((url: string) => !this.esUrl(url))) {
      this.error.set('Indicá hasta cinco URLs de archivos modelo válidas.');
      return;
    }

    const plantilla: Partial<PlantillaTarea> = {
      id: valores.id ?? 0,
      nombre: valores.nombre ?? '',
      tituloSugerido: valores.tituloSugerido ?? '',
      descripcionBase: valores.descripcionBase ?? '',
      horasEstimadas: valores.horasEstimadas ?? 0,
      complejidad: valores.complejidad ?? '',
      prioridadSugerida: valores.prioridadSugerida ?? '',
      idSkillRequerido: valores.idSkillRequerido ?? 0,
      seniorityRecomendado: valores.seniorityRecomendado ?? '',
      estado: 'Activa',
      activo: true,
      checklistBaseJson: JSON.stringify(this.checklist()),
      archivosAdjuntosJson: urls.length ? JSON.stringify(urls) : null,
    };
    const esNueva = !plantilla.id;
    this.limpiarMensajes();

    this.service.guardar(plantilla).subscribe({
      next: () => {
        this.mostrarFormulario.set(false);
        this.mensaje.set(esNueva ? 'Plantilla de tarea creada correctamente.' : 'Plantilla modificada correctamente.');
        this.cargar();
      },
      error: (respuesta) => this.error.set(this.obtenerError(respuesta, 'No se pudo guardar la plantilla.')),
    });
  }

  solicitarBaja(plantilla: PlantillaTarea): void {
    this.bajaForm.reset({ motivoBaja: '' });
    this.mostrarBaja.set(plantilla);
    this.limpiarMensajes();
  }

  confirmarBaja(): void {
    const plantilla = this.mostrarBaja();
    if (!plantilla || this.bajaForm.invalid) {
      this.error.set('Ingresá el motivo de la baja.');
      return;
    }

    this.service.darBaja({ ...plantilla, motivoBaja: this.bajaForm.controls.motivoBaja.value || '' }).subscribe({
      next: () => {
        this.mostrarBaja.set(null);
        this.mensaje.set('Plantilla dada de baja correctamente. Ya no estará disponible al crear tareas nuevas.');
        this.cargar();
      },
      error: (respuesta) => this.error.set(this.obtenerError(respuesta, 'No se pudo dar de baja la plantilla.')),
    });
  }

  agregarChecklist(): void {
    const texto = this.nuevoChecklist().trim();
    if (!texto) return;
    this.checklist.update((items: ChecklistItem[]) => [...items, { texto, completada: false }]);
    this.nuevoChecklist.set('');
  }

  quitarChecklist(indice: number): void {
    this.checklist.update((items: ChecklistItem[]) => items.filter((_, posicion: number) => posicion !== indice));
  }

  private urls(valor?: string | null): string[] {
    if (!valor) return [];
    try {
      const archivos = JSON.parse(valor);
      return Array.isArray(archivos) ? archivos.filter((archivo: unknown): archivo is string => typeof archivo === 'string') : [];
    } catch {
      return valor.split(/\r?\n/).map((url: string) => url.trim()).filter(Boolean);
    }
  }

  private obtenerChecklist(valor?: string | null): ChecklistItem[] {
    if (!valor) return [];
    try {
      const items = JSON.parse(valor);
      return Array.isArray(items) ? items.filter((item) => typeof item?.texto === 'string').map((item) => ({ texto: item.texto, completada: Boolean(item.completada) })) : [];
    } catch {
      return [];
    }
  }

  private esUrl(url: string): boolean {
    try { const direccion = new URL(url); return direccion.protocol === 'https:' || direccion.protocol === 'http:'; } catch { return false; }
  }

  private limpiarMensajes(): void { this.error.set(''); this.mensaje.set(''); }
  private obtenerError(respuesta: any, predeterminado: string): string
  {
    if (typeof respuesta?.error === 'string') return respuesta.error;

    const errores = respuesta?.error?.errors;
    if (errores && typeof errores === 'object')
    {
      const mensajes = Object.values(errores).flat().filter((mensaje): mensaje is string => typeof mensaje === 'string');
      if (mensajes.length) return mensajes.join(' ');
    }

    return predeterminado;
  }
}

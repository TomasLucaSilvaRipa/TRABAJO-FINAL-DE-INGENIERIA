import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocalizationService } from '../../../../services/localization.service';
import { RegistroHorasService } from '../../../../services/registro-horas.service';
import { Tarea, TasksService } from '../../../../services/tasks.service';

@Component({
  selector: 'app-register-hours',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './register-hours.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterHoursComponent {
  readonly localization = inject(LocalizationService);
  private readonly tasksService = inject(TasksService);
  private readonly registroHorasService = inject(RegistroHorasService);
  readonly tareas = signal<Tarea[]>([]);
  readonly guardando = signal(false);
  readonly error = signal('');
  readonly form = new FormBuilder().nonNullable.group({
    tarea: [0, [Validators.required, Validators.min(1)]],
    fecha: [new Date().toISOString().slice(0, 10), Validators.required],
    horas: [0, [Validators.required, Validators.min(0.25), Validators.max(24)]],
    descripcion: ['', Validators.maxLength(500)],
  });

  enviado = false;

  constructor() { this.tasksService.consultarMias().subscribe({ next: tareas => this.tareas.set(tareas.filter(tarea => tarea.activo)), error: () => this.error.set('No se pudieron cargar tus tareas asignadas.') }); }

  registrar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const datos = this.form.getRawValue();
    this.guardando.set(true);
    this.error.set('');
    this.registroHorasService.registrar({ idTarea: Number(datos.tarea), fecha: datos.fecha, cantidadHoras: Number(datos.horas), descripcion: datos.descripcion }).subscribe({ next: () => { this.guardando.set(false); this.enviado = true; this.form.controls.horas.setValue(0); this.form.controls.descripcion.setValue(''); }, error: error => { this.guardando.set(false); this.error.set(error?.error || 'No se pudo registrar el tiempo trabajado.'); } });
  }
}

import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { LocalizationService } from '../../../../services/localization.service';
import { RegistroHora, RegistroHorasService } from '../../../../services/registro-horas.service';
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
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(FormBuilder);
  readonly tareas = signal<Tarea[]>([]);
  readonly guardando = signal(false);
  readonly error = signal('');
  readonly previsualizacion = signal<RegistroHora[]>([]);
  readonly requiereConfirmacion = signal(false);
  readonly aviso = signal('');
  readonly fechaMinima = this.obtenerFechaMinima();
  readonly fechaActual = this.obtenerFechaActual();
  readonly form = this.formBuilder.nonNullable.group({
    fecha: [this.fechaActual, Validators.required],
    registros: this.formBuilder.nonNullable.array([this.crearRegistro()]),
  });

  enviado = false;

  get registros() { return this.form.controls.registros; }

  constructor() {
    const idTarea = Number(this.route.snapshot.queryParamMap.get('tarea'));
    const horas = Number(this.route.snapshot.queryParamMap.get('horas'));
    const descripcion = this.route.snapshot.queryParamMap.get('descripcion');
    const aviso = this.route.snapshot.queryParamMap.get('aviso');
    if (idTarea > 0) { this.registros.at(0).controls.idTarea.setValue(idTarea); }
    if (horas >= 0.25 && horas <= 24) { this.registros.at(0).controls.cantidadHoras.setValue(horas); }
    if (descripcion) { this.registros.at(0).controls.descripcion.setValue(descripcion); }
    if (aviso) { this.aviso.set(aviso); }
    this.tasksService.consultarMias().subscribe({
      next: tareas => this.tareas.set(tareas.filter(tarea => tarea.activo)),
      error: () => this.error.set('No se pudieron cargar tus tareas asignadas.'),
    });
  }

  agregarTarea(): void { this.registros.push(this.crearRegistro()); this.validarFechaTareas(); this.limpiarPrevisualizacion(); }

  quitarTarea(indice: number): void {
    if (this.registros.length > 1) { this.registros.removeAt(indice); }
    this.validarFechaTareas();
    this.limpiarPrevisualizacion();
  }

  totalHoras(): number { return this.registros.getRawValue().reduce((total, registro) => total + Number(registro.cantidadHoras || 0), 0); }

  registrar(): void {
    if (this.requiereConfirmacion()) {
      this.guardarRegistros();
      return;
    }
    if (!this.fechaDentroDelLimite()) {
      this.error.set(this.mensajeFechaInvalida());
      return;
    }
    if (this.registros.getRawValue().some(registro => Number(registro.cantidadHoras) > 24)) {
      this.error.set('Cada registro diario puede tener como máximo 24 horas.');
      return;
    }
    if (this.totalHoras() > 24) {
      this.error.set('El total de horas de una jornada no puede superar 24 horas.');
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set('Completá la fecha, una tarea, las horas y la descripción del avance en cada registro.');
      return;
    }
    this.previsualizarRegistros();
  }

  limpiarPrevisualizacion(): void {
    this.previsualizacion.set([]);
    this.requiereConfirmacion.set(false);
    this.enviado = false;
  }

  private crearRegistro(idTarea = 0) {
    return this.formBuilder.nonNullable.group({
      idTarea: [idTarea, [Validators.required, Validators.min(1)]],
      cantidadHoras: [0, [Validators.required, Validators.min(0.25), Validators.max(24)]],
      descripcion: ['', [Validators.required, Validators.maxLength(500)]],
    });
  }

  private previsualizarRegistros(): void {
    const registros = this.obtenerRegistros();
    this.guardando.set(true);
    this.error.set('');
    this.previsualizacion.set([]);
    this.registroHorasService.previsualizar(registros).subscribe({
      next: resultado => {
        this.guardando.set(false);
        this.previsualizacion.set(resultado);
        if (resultado.some(registro => registro.requiereAdvertencia)) { this.requiereConfirmacion.set(true); }
        else { this.guardarRegistros(); }
      },
      error: error => {
        this.guardando.set(false);
        this.error.set(this.obtenerMensajeError(error));
      },
    });
  }

  private guardarRegistros(): void {
    const registros = this.obtenerRegistros();
    this.guardando.set(true);
    this.error.set('');
    this.registroHorasService.registrarImputaciones(registros).subscribe({
      next: () => {
        this.guardando.set(false);
        this.enviado = true;
        this.requiereConfirmacion.set(false);
        this.previsualizacion.set([]);
        this.registros.clear();
        this.registros.push(this.crearRegistro());
        this.form.controls.fecha.setValue(this.fechaActual);
      },
      error: error => {
        this.guardando.set(false);
        this.error.set(this.obtenerMensajeError(error));
      },
    });
  }

  private obtenerRegistros(): RegistroHora[] {
    const fecha = this.form.controls.fecha.value;
    return this.registros.getRawValue().map(registro => ({ idTarea: Number(registro.idTarea), fecha, cantidadHoras: Number(registro.cantidadHoras), descripcion: registro.descripcion.trim() }));
  }

  private obtenerMensajeError(error: any): string {
    if (typeof error?.error === 'string') { return error.error; }
    if (error?.error?.title) { return error.error.title; }
    return 'No se pudo registrar el tiempo trabajado.';
  }

  fechaMinimaPermitida(): string {
    let fechaMinima = this.fechaMinima;
    this.registros.getRawValue().forEach(registro => {
      const tarea = this.tareas().find(item => item.id === Number(registro.idTarea));
      const fechaInicio = tarea?.fechaInicio?.slice(0, 10);
      if (fechaInicio && fechaInicio > fechaMinima) { fechaMinima = fechaInicio; }
    });
    return fechaMinima;
  }

  validarFechaTareas(): void {
    if (!this.fechaDentroDelLimite()) { this.error.set(this.mensajeFechaInvalida()); }
    else { this.error.set(''); }
  }

  private fechaDentroDelLimite(): boolean {
    const fecha = this.form.controls.fecha.value;
    return fecha >= this.fechaMinimaPermitida() && fecha <= this.fechaActual;
  }

  private mensajeFechaInvalida(): string {
    if (this.form.controls.fecha.value < this.fechaMinimaPermitida()) { return 'La fecha de trabajo no puede ser anterior a la fecha de inicio de las tareas seleccionadas.'; }
    return 'Podés registrar horas únicamente desde los últimos 30 días hasta la fecha actual.';
  }

  private obtenerFechaActual(): string { return this.formatearFechaLocal(new Date()); }

  private obtenerFechaMinima(): string {
    const fecha = new Date();
    fecha.setDate(fecha.getDate() - 30);
    return this.formatearFechaLocal(fecha);
  }

  private formatearFechaLocal(fecha: Date): string {
    const anio = fecha.getFullYear();
    const mes = String(fecha.getMonth() + 1).padStart(2, '0');
    const dia = String(fecha.getDate()).padStart(2, '0');
    return `${anio}-${mes}-${dia}`;
  }
}

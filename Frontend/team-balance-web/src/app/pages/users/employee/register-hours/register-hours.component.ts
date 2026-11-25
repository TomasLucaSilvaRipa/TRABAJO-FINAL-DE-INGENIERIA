import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import { LocalizationService } from '../../../../services/localization.service';
import { HORAS_ADVERTENCIA_JORNADA, HORAS_MAXIMAS_JORNADA, RegistroHora, RegistroHorasService } from '../../../../services/registro-horas.service';
import { Tarea, TasksService } from '../../../../services/tasks.service';
import { AuthService } from '../../../../services/auth.service';
import { correspondeRecordatorioPausa } from './recordatorio-pausa';

@Component({
  selector: 'app-register-hours',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './register-hours.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterHoursComponent {
  readonly localization = inject(LocalizationService);
  private readonly authService = inject(AuthService);
  private readonly tasksService = inject(TasksService);
  private readonly registroHorasService = inject(RegistroHorasService);
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(FormBuilder);
  readonly tareas = signal<Tarea[]>([]);
  readonly guardando = signal(false);
  readonly error = signal('');
  readonly previsualizacion = signal<RegistroHora[]>([]);
  readonly requiereAvisoTarea = computed(() => this.previsualizacion().some(registro => registro.requiereAdvertencia));
  readonly requiereConfirmacion = signal(false);
  readonly totalJornadaAdvertencia = signal<number | null>(null);
  readonly horasMaximasJornada = HORAS_MAXIMAS_JORNADA;
  readonly aviso = signal('');
  readonly mostrarRecordatorioPausa = signal(false);
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
    if (horas >= 0.25 && horas <= this.horasMaximasJornada) { this.registros.at(0).controls.cantidadHoras.setValue(horas); }
    if (descripcion) { this.registros.at(0).controls.descripcion.setValue(descripcion); }
    if (aviso) { this.aviso.set(aviso); }
    this.tasksService.consultarMias().subscribe({
      next: tareas => this.tareas.set(tareas.filter(tarea => tarea.activo)),
      error: error => this.error.set(this.obtenerMensajeError(error, 'loadTasksError')),
    });
  }

  agregarTarea(): void { this.registros.push(this.crearRegistro()); this.validarFechaTareas(); this.limpiarPrevisualizacion(); }

  quitarTarea(indice: number): void {
    if (this.registros.length > 1) { this.registros.removeAt(indice); }
    this.validarFechaTareas();
    this.limpiarPrevisualizacion();
  }

  totalHoras(): number { return this.registros.getRawValue().reduce((total, registro) => total + Number(registro.cantidadHoras || 0), 0); }

  texto(clave: string): string { return this.localization.traducir(`private.hours.${clave}`); }

  textoAviso(): string {
    const aviso = this.aviso();
    if (aviso === 'focusLimitReached') { return this.texto(aviso); }
    return aviso.startsWith('La sesión de foco fue menor a 15 minutos') ? this.texto('shortFocusSession') : aviso;
  }

  registrar(): void {
    if (this.guardando()) { return; }
    if (!this.fechaDentroDelLimite()) {
      this.error.set(this.mensajeFechaInvalida());
      return;
    }
    if (this.registros.getRawValue().some(registro => Number(registro.cantidadHoras) > this.horasMaximasJornada)) {
      this.error.set('private.hours.entryLimit');
      return;
    }
    if (this.totalHoras() > this.horasMaximasJornada) {
      this.error.set('private.hours.dailyLimit');
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set('private.hours.invalidFields');
      return;
    }
    if (this.requiereConfirmacion()) { this.guardarRegistros(); }
    else { this.previsualizarRegistros(); }
  }

  limpiarPrevisualizacion(): void {
    this.previsualizacion.set([]);
    this.requiereConfirmacion.set(false);
    this.totalJornadaAdvertencia.set(null);
    this.enviado = false;
  }

  private crearRegistro(idTarea = 0) {
    return this.formBuilder.nonNullable.group({
      idTarea: [idTarea, [Validators.required, Validators.min(1)]],
      cantidadHoras: [0, [Validators.required, Validators.min(0.25), Validators.max(this.horasMaximasJornada)]],
      descripcion: ['', [Validators.required, Validators.maxLength(500)]],
    });
  }

  private previsualizarRegistros(): void {
    const registros = this.obtenerRegistros();
    const fecha = this.form.controls.fecha.value;
    const totalNuevo = registros.reduce((total, registro) => total + Math.round(registro.cantidadHoras * 100), 0);
    this.guardando.set(true);
    this.error.set('');
    this.previsualizacion.set([]);
    forkJoin({
      resultado: this.registroHorasService.previsualizar(registros),
      propios: this.registroHorasService.consultarPropios(),
    }).subscribe({
      next: ({ resultado, propios }) => {
        this.guardando.set(false);
        if (JSON.stringify(registros) !== JSON.stringify(this.obtenerRegistros())) { return; }
        const totalPrevio = propios.reduce((total, registro) => (
          registro.activo !== false && registro.fecha.slice(0, 10) === fecha
            ? total + Math.round(registro.cantidadHoras * 100)
            : total
        ), 0);
        const totalJornada = (totalPrevio + totalNuevo) / 100;
        if (totalJornada > this.horasMaximasJornada) {
          this.error.set('private.hours.dailyLimit');
          return;
        }

        this.previsualizacion.set(resultado);
        if (totalJornada > HORAS_ADVERTENCIA_JORNADA) { this.totalJornadaAdvertencia.set(totalJornada); }
        if (this.totalJornadaAdvertencia() !== null || this.requiereAvisoTarea()) { this.requiereConfirmacion.set(true); }
        else { this.guardarRegistros(); }
      },
      error: error => {
        this.guardando.set(false);
        this.error.set(this.obtenerMensajeError(error, 'reviewError'));
      },
    });
  }

  private guardarRegistros(): void {
    const registros = this.obtenerRegistros();
    const usuario = this.authService.usuarioActual();
    this.guardando.set(true);
    this.error.set('');
    this.registroHorasService.registrarImputaciones(registros).subscribe({
      next: confirmados => {
        this.guardando.set(false);
        this.enviado = true;
        this.requiereConfirmacion.set(false);
        this.totalJornadaAdvertencia.set(null);
        this.previsualizacion.set([]);
        this.registros.clear();
        this.registros.push(this.crearRegistro());
        this.form.controls.fecha.setValue(this.fechaActual);
        if (usuario?.idAgencia && correspondeRecordatorioPausa(usuario.idAgencia, usuario.id, this.obtenerFechaActual(), confirmados)) {
          this.mostrarRecordatorioPausa.set(true);
        }
      },
      error: error => {
        this.guardando.set(false);
        this.error.set(this.obtenerMensajeError(error, 'saveError'));
      },
    });
  }

  private obtenerRegistros(): RegistroHora[] {
    const fecha = this.form.controls.fecha.value;
    return this.registros.getRawValue().map(registro => ({ idTarea: Number(registro.idTarea), fecha, cantidadHoras: Number(registro.cantidadHoras), descripcion: registro.descripcion.trim() }));
  }

  private obtenerMensajeError(respuesta: HttpErrorResponse, alternativa: string): string {
    const detalle = typeof respuesta.error === 'string' ? respuesta.error : respuesta.error?.detail;
    const mensaje = typeof detalle === 'string' ? detalle : '';
    if (respuesta.status === 0) return 'projectForm.connection';
    if (/sesi[oó]n.*(v[aá]lida|venci)/i.test(mensaje) || (respuesta.status === 401 && !mensaje)) return 'projectForm.invalidSession';
    if (/perfil de empleado activo/i.test(mensaje)) return 'private.workload.noActiveEmployee';
    if (respuesta.status === 401 || respuesta.status === 403) return 'projectForm.notAllowed';
    if (/horas futuras|horas de los [uú]ltimos/i.test(mensaje)) return 'private.hours.dateRange';
    if (/fecha de inicio de la tarea/i.test(mensaje)) return 'private.hours.beforeTaskStart';
    if (/no est[aá] asignada a tu usuario/i.test(mensaje)) return 'private.hours.taskNotAssigned';
    if (/misma fecha/i.test(mensaje)) return 'private.hours.sameDate';
    if (/total de horas.*(?:15|24)/i.test(mensaje)) return 'private.hours.dailyLimit';
    if (/al menos una tarea|no se recibieron horas|horas v[aá]lidas/i.test(mensaje)) return 'private.hours.invalidFields';
    return `private.hours.${alternativa}`;
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
    const fecha = this.form.controls.fecha.value;
    if (fecha && fecha >= this.fechaMinima && fecha <= this.fechaActual) return 'private.hours.beforeTaskStart';
    return 'private.hours.dateRange';
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

import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AgencyService, Agencia } from '../../../../services/agency.service';
import { LocalizationService } from '../../../../services/localization.service';
import { EstadoTarea, TasksService } from '../../../../services/tasks.service';
import { AgencySetupComponent } from '../../../../shared/components/agency-setup/agency-setup.component';

@Component({
  selector: 'app-agency-configuration',
  imports: [DatePipe, ReactiveFormsModule, AgencySetupComponent],
  templateUrl: './agency-configuration.component.html',
  styleUrl: './agency-configuration.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AgencyConfiguration {
  private readonly formBuilder = inject(FormBuilder); private readonly agencyService = inject(AgencyService); private readonly tasksService = inject(TasksService);
  readonly localization = inject(LocalizationService);
  readonly cargando = signal(true); readonly error = signal(''); readonly mensaje = signal('');
  readonly errorEstado = signal(''); readonly mensajeEstado = signal('');
  readonly errorAlertas = signal(''); readonly mensajeAlertas = signal('');
  readonly estados = signal<EstadoTarea[]>([]); readonly estadoEditando = signal<EstadoTarea | null>(null); readonly guardandoEstado = signal(false);
  readonly form = this.formBuilder.group({ id: [0], nombreComercial: ['', Validators.required], razonSocial: [''], cuit: [{ value: '', disabled: true }], condicionFiscal: [''], emailContacto: ['', [Validators.required, Validators.email]], telefonoContacto: [''], fechaAlta: [{ value: '', disabled: true }] });
  readonly estadoForm = this.formBuilder.group({ nombre: ['', [Validators.required, Validators.maxLength(80)]] });
  readonly alertasForm = this.formBuilder.nonNullable.group({ umbralConsumoHoras: [80, [Validators.required, Validators.min(1), Validators.max(100)]], notificacionInternaActiva: [true], emailActivo: [true], slackActivo: [true] });

  constructor() {
    this.agencyService.consultarActual().subscribe({
      next: agencia => {
        this.form.patchValue({ ...agencia, condicionFiscal: this.normalizarCondicionFiscal(agencia.condicionFiscal) });
        this.cargando.set(false);
      },
      error: respuesta => {
        this.error.set(this.claveError(respuesta, 'agency.loadError'));
        this.cargando.set(false);
      }
    });
    this.cargarEstados();
    this.tasksService.consultarConfiguracionAlertas().subscribe({
      next: configuracion => this.alertasForm.patchValue(configuracion),
      error: respuesta => this.errorAlertas.set(this.claveError(respuesta, 'agency.loadAlertsError'))
    });
  }

  guardar(): void {
    this.error.set('');
    this.mensaje.set('');
    if (this.form.invalid || !this.form.controls.nombreComercial.value?.trim()) {
      this.error.set('agency.invalidFields');
      return;
    }

    const datos = this.form.getRawValue();
    const agencia: Partial<Agencia> = { id: datos.id ?? 0, nombreComercial: datos.nombreComercial ?? '', razonSocial: datos.razonSocial ?? '', cuit: datos.cuit ?? '', condicionFiscal: datos.condicionFiscal ?? '', emailContacto: datos.emailContacto ?? '', telefonoContacto: datos.telefonoContacto ?? '', fechaAlta: datos.fechaAlta ?? '' };
    this.agencyService.modificarActual(agencia).subscribe({
      next: agenciaActualizada => {
        this.form.patchValue({ ...agenciaActualizada, condicionFiscal: this.normalizarCondicionFiscal(agenciaActualizada.condicionFiscal) });
        this.mensaje.set('agency.saved');
      },
      error: respuesta => this.error.set(this.claveError(respuesta, 'agency.saveError'))
    });
  }

  cargarEstados(): void {
    this.tasksService.consultarEstados().subscribe({
      next: estados => this.estados.set(estados),
      error: respuesta => {
        this.mensajeEstado.set('');
        this.errorEstado.set(this.claveError(respuesta, 'agency.loadStatesError'));
      }
    });
  }
  editarEstado(estado: EstadoTarea): void { if (estado.esBase) return; this.estadoEditando.set(estado); this.estadoForm.setValue({ nombre: estado.nombre }); }
  cancelarEstado(): void { this.estadoEditando.set(null); this.estadoForm.reset({ nombre: '' }); }
  guardarEstado(): void {
    if (this.guardandoEstado()) return;
    this.errorEstado.set('');
    this.mensajeEstado.set('');
    const nombre = this.estadoForm.controls.nombre.value?.trim();
    if (!nombre || this.estadoForm.invalid) {
      this.errorEstado.set('agency.stateNameInvalid');
      return;
    }

    this.guardandoEstado.set(true);
    const actual = this.estadoEditando();
    this.tasksService.guardarEstado({ id: actual?.id ?? 0, nombre }).subscribe({
      next: () => { this.guardandoEstado.set(false); this.cancelarEstado(); this.mensajeEstado.set('agency.stateSaved'); this.cargarEstados(); },
      error: respuesta => { this.guardandoEstado.set(false); this.errorEstado.set(this.claveError(respuesta, 'agency.saveStateError')); }
    });
  }
  desactivarEstado(estado: EstadoTarea): void {
    if (estado.esBase || this.guardandoEstado()) return;
    this.guardandoEstado.set(true);
    this.errorEstado.set('');
    this.mensajeEstado.set('');
    this.tasksService.desactivarEstado(estado.id).subscribe({
      next: () => { this.guardandoEstado.set(false); this.mensajeEstado.set('agency.stateDeactivated'); this.cargarEstados(); },
      error: respuesta => { this.guardandoEstado.set(false); this.errorEstado.set(this.claveError(respuesta, 'agency.deactivateStateError')); }
    });
  }
  personalizadosActivos(): number { return this.estados().filter(estado => !estado.esBase && estado.activo).length; }
  guardarAlertas(): void {
    this.errorAlertas.set('');
    this.mensajeAlertas.set('');
    if (this.alertasForm.invalid) {
      this.errorAlertas.set('agency.thresholdInvalid');
      return;
    }

    this.tasksService.guardarConfiguracionAlertas(this.alertasForm.getRawValue()).subscribe({
      next: configuracion => {
        this.alertasForm.patchValue(configuracion);
        this.mensajeAlertas.set('agency.alertsSaved');
      },
      error: respuesta => this.errorAlertas.set(this.claveError(respuesta, 'agency.saveAlertsError'))
    });
  }

  private claveError(respuesta: HttpErrorResponse, alternativa: string): string {
    const detalle = typeof respuesta.error === 'string' ? respuesta.error : '';
    if (respuesta.status === 0) return 'projectForm.connection';
    if (respuesta.status === 401 || /sesi[oó]n ya no es v[aá]lida/i.test(detalle)) return 'projectForm.invalidSession';
    if (respuesta.status === 403) return 'projectForm.notAllowed';
    if (/nombre comercial y un email/i.test(detalle)) return 'agency.invalidFields';
    if (/umbral debe estar entre/i.test(detalle)) return 'agency.thresholdInvalid';
    if (/nombre (para el|del) estado/i.test(detalle)) return 'agency.stateNameInvalid';
    if (/tres estados personalizados/i.test(detalle)) return 'agency.stateLimit';
    if (/estado activo con ese nombre/i.test(detalle)) return 'agency.stateDuplicate';
    if (/tiene tareas activas asociadas/i.test(detalle)) return 'agency.stateInUse';
    return alternativa;
  }

  private normalizarCondicionFiscal(valor: string | null | undefined): string {
    const clave = (valor ?? '').trim().toLocaleLowerCase('es-AR').normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    const equivalencias: Record<string, string> = { registered: 'registered', 'responsable inscripto': 'registered', monotributo: 'monotributo', monotributista: 'monotributo', exempt: 'exempt', exento: 'exempt', consumer: 'consumer', 'consumidor final': 'consumer', other: 'other', otra: 'other' };
    return equivalencias[clave] ?? '';
  }
}

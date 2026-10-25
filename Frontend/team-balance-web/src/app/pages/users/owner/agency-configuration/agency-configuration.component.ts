import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AgencyService, Agencia } from '../../../../services/agency.service';
import { LocalizationService } from '../../../../services/localization.service';
import { ConfiguracionAlertaAgencia, EstadoTarea, TasksService } from '../../../../services/tasks.service';

@Component({
  selector: 'app-agency-configuration',
  imports: [DatePipe, ReactiveFormsModule],
  templateUrl: './agency-configuration.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AgencyConfiguration {
  private readonly formBuilder = inject(FormBuilder); private readonly agencyService = inject(AgencyService); private readonly tasksService = inject(TasksService);
  readonly localization = inject(LocalizationService);
  readonly cargando = signal(true); readonly error = signal(''); readonly mensaje = signal('');
  readonly estados = signal<EstadoTarea[]>([]); readonly estadoEditando = signal<EstadoTarea | null>(null); readonly guardandoEstado = signal(false);
  readonly form = this.formBuilder.group({ id: [0], nombreComercial: ['', Validators.required], razonSocial: [''], cuit: [{ value: '', disabled: true }], condicionFiscal: [''], emailContacto: ['', [Validators.required, Validators.email]], telefonoContacto: [''], fechaAlta: [{ value: '', disabled: true }] });
  readonly estadoForm = this.formBuilder.group({ nombre: ['', [Validators.required, Validators.maxLength(80)]] });
  readonly alertasForm = this.formBuilder.group({ umbralConsumoHoras: [80, [Validators.required, Validators.min(1), Validators.max(100)]], notificacionInternaActiva: [true], emailActivo: [true], slackActivo: [true] });

  constructor() { this.agencyService.consultarActual().subscribe({ next: (agencia: Agencia) => { this.form.patchValue({ ...agencia, condicionFiscal: this.normalizarCondicionFiscal(agencia.condicionFiscal) }); this.cargando.set(false); }, error: () => { this.error.set('No se pudieron cargar los datos de la agencia.'); this.cargando.set(false); } }); this.cargarEstados(); this.tasksService.consultarConfiguracionAlertas().subscribe({ next: configuracion => this.alertasForm.patchValue(configuracion) }); }

  guardar(): void { if (this.form.invalid) { this.error.set('Completá los datos obligatorios correctamente.'); return; } this.error.set(''); this.mensaje.set(''); const datos = this.form.getRawValue(); const agencia: Partial<Agencia> = { id: datos.id ?? 0, nombreComercial: datos.nombreComercial ?? '', razonSocial: datos.razonSocial ?? '', cuit: datos.cuit ?? '', condicionFiscal: datos.condicionFiscal ?? '', emailContacto: datos.emailContacto ?? '', telefonoContacto: datos.telefonoContacto ?? '', fechaAlta: datos.fechaAlta ?? '' }; this.agencyService.modificarActual(agencia).subscribe({ next: (agenciaActualizada: Agencia) => { this.form.patchValue(agenciaActualizada); this.mensaje.set('Los datos editables de la agencia fueron actualizados.'); }, error: (respuesta: { error?: string }) => this.error.set(respuesta.error || 'No se pudieron guardar los cambios.') }); }

  cargarEstados(): void { this.tasksService.consultarEstados().subscribe({ next: estados => this.estados.set(estados), error: () => this.error.set('No se pudieron cargar los estados de tareas.') }); }
  editarEstado(estado: EstadoTarea): void { if (estado.esBase) return; this.estadoEditando.set(estado); this.estadoForm.setValue({ nombre: estado.nombre }); }
  cancelarEstado(): void { this.estadoEditando.set(null); this.estadoForm.reset({ nombre: '' }); }
  guardarEstado(): void {
    if (this.estadoForm.invalid || this.guardandoEstado()) return;
    this.guardandoEstado.set(true); this.error.set(''); this.mensaje.set('');
    const actual = this.estadoEditando();
    this.tasksService.guardarEstado({ id: actual?.id ?? 0, nombre: this.estadoForm.controls.nombre.value?.trim() ?? '' }).subscribe({
      next: () => { this.guardandoEstado.set(false); this.cancelarEstado(); this.mensaje.set('El flujo de tareas fue actualizado.'); this.cargarEstados(); },
      error: respuesta => { this.guardandoEstado.set(false); this.error.set(typeof respuesta?.error === 'string' ? respuesta.error : 'No se pudo guardar el estado.'); }
    });
  }
  desactivarEstado(estado: EstadoTarea): void {
    if (estado.esBase || this.guardandoEstado()) return;
    this.guardandoEstado.set(true); this.error.set('');
    this.tasksService.desactivarEstado(estado.id).subscribe({ next: () => { this.guardandoEstado.set(false); this.mensaje.set('El estado personalizado fue desactivado.'); this.cargarEstados(); }, error: respuesta => { this.guardandoEstado.set(false); this.error.set(typeof respuesta?.error === 'string' ? respuesta.error : 'No se pudo desactivar el estado.'); } });
  }
  personalizadosActivos(): number { return this.estados().filter(estado => !estado.esBase && estado.activo).length; }
  guardarAlertas(): void { if (this.alertasForm.invalid) return; this.error.set(''); this.tasksService.guardarConfiguracionAlertas(this.alertasForm.getRawValue() as Partial<ConfiguracionAlertaAgencia>).subscribe({ next: configuracion => { this.alertasForm.patchValue(configuracion); this.mensaje.set('La configuración de alertas fue actualizada.'); }, error: respuesta => this.error.set(typeof respuesta?.error === 'string' ? respuesta.error : 'No se pudo guardar la configuración de alertas.') }); }

  private normalizarCondicionFiscal(valor: string | null | undefined): string {
    const clave = (valor ?? '').trim().toLocaleLowerCase('es-AR').normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    const equivalencias: Record<string, string> = { registered: 'registered', 'responsable inscripto': 'registered', monotributo: 'monotributo', monotributista: 'monotributo', exempt: 'exempt', exento: 'exempt', consumer: 'consumer', 'consumidor final': 'consumer', other: 'other', otra: 'other' };
    return equivalencias[clave] ?? '';
  }
}

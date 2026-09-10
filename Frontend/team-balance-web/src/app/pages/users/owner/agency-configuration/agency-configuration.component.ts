import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AgencyService, Agencia } from '../../../../services/agency.service';
import { LocalizationService } from '../../../../services/localization.service';

@Component({
  selector: 'app-agency-configuration',
  imports: [ReactiveFormsModule],
  templateUrl: './agency-configuration.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AgencyConfiguration {
  private readonly formBuilder = inject(FormBuilder); private readonly agencyService = inject(AgencyService);
  readonly localization = inject(LocalizationService);
  readonly cargando = signal(true); readonly error = signal(''); readonly mensaje = signal('');
  readonly form = this.formBuilder.group({ id: [0], nombreComercial: ['', Validators.required], razonSocial: [''], cuit: [{ value: '', disabled: true }], condicionFiscal: [''], emailContacto: ['', [Validators.required, Validators.email]], telefonoContacto: [''], fechaAlta: [{ value: '', disabled: true }], estado: [{ value: '', disabled: true }] });

  constructor() { this.agencyService.consultarActual().subscribe({ next: (agencia: Agencia) => { this.form.patchValue(agencia); this.cargando.set(false); }, error: () => { this.error.set('No se pudieron cargar los datos de la agencia.'); this.cargando.set(false); } }); }

  guardar(): void { if (this.form.invalid) { this.error.set('Completá los datos obligatorios correctamente.'); return; } this.error.set(''); this.mensaje.set(''); const datos = this.form.getRawValue(); const agencia: Partial<Agencia> = { id: datos.id ?? 0, nombreComercial: datos.nombreComercial ?? '', razonSocial: datos.razonSocial ?? '', cuit: datos.cuit ?? '', condicionFiscal: datos.condicionFiscal ?? '', emailContacto: datos.emailContacto ?? '', telefonoContacto: datos.telefonoContacto ?? '', fechaAlta: datos.fechaAlta ?? '', estado: datos.estado ?? '' }; this.agencyService.modificarActual(agencia).subscribe({ next: (agenciaActualizada: Agencia) => { this.form.patchValue(agenciaActualizada); this.mensaje.set('Los datos editables de la agencia fueron actualizados.'); }, error: (respuesta: { error?: string }) => this.error.set(respuesta.error || 'No se pudieron guardar los cambios.') }); }
}

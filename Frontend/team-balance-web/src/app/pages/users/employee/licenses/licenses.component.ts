import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AusenciaEmpleado, ResourcesService } from '../../../../services/resources.service';

@Component({ selector: 'app-licenses', standalone: true, imports: [RouterModule, ReactiveFormsModule, DatePipe], templateUrl: './licenses.component.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class LicensesComponent {
  private readonly formBuilder = inject(FormBuilder); private readonly resourcesService = inject(ResourcesService);
  readonly ausencias = signal<AusenciaEmpleado[]>([]); readonly error = signal(''); readonly mensaje = signal(''); readonly enviando = signal(false);
  readonly form = this.formBuilder.nonNullable.group({ tipoPeriodo: ['Licencia', Validators.required], fechaInicioSolicitada: ['', Validators.required], fechaFinSolicitada: ['', Validators.required], motivo: [''] });
  constructor() { this.cargar(); }
  cargar(): void { this.resourcesService.consultarMisAusencias().subscribe({ next: (ausencias: AusenciaEmpleado[]) => this.ausencias.set(ausencias), error: error => this.error.set(typeof error?.error === 'string' ? error.error : error?.error?.message || 'No se pudieron cargar las ausencias.') }); }
  enviar(): void { if (this.form.invalid) { this.form.markAllAsTouched(); return; } this.enviando.set(true); this.error.set(''); const ausencia: AusenciaEmpleado = { ...this.form.getRawValue(), activo: true }; this.resourcesService.registrarAusencia(ausencia).subscribe({ next: (resultado: AusenciaEmpleado) => { this.ausencias.set([resultado, ...this.ausencias()]); this.form.reset({ tipoPeriodo: 'Licencia', fechaInicioSolicitada: '', fechaFinSolicitada: '', motivo: '' }); this.enviando.set(false); this.mensaje.set('La solicitud fue enviada para su revisión.'); }, error: error => { this.enviando.set(false); this.error.set(typeof error?.error === 'string' ? error.error : error?.error?.message || error?.error?.title || 'No se pudo registrar la solicitud.'); } }); }
}

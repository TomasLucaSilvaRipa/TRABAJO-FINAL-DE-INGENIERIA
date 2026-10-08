import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AusenciaEmpleado, ResourcesService, TareaDisponibilidadAfectada } from '../../../../services/resources.service';

@Component({ selector: 'app-licenses', standalone: true, imports: [RouterModule, ReactiveFormsModule, DatePipe], templateUrl: './licenses.component.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class LicensesComponent {
  private readonly formBuilder = inject(FormBuilder); private readonly resourcesService = inject(ResourcesService);
  readonly ausencias = signal<AusenciaEmpleado[]>([]); readonly tareasAfectadas = signal<TareaDisponibilidadAfectada[]>([]); readonly error = signal(''); readonly mensaje = signal(''); readonly enviando = signal(false);
  readonly form = this.formBuilder.nonNullable.group({ tipoPeriodo: ['Licencia', Validators.required], fechaInicioSolicitada: ['', Validators.required], fechaFinSolicitada: ['', Validators.required], horasNoDisponiblesSolicitadas: [null as number | null], motivo: ['', Validators.required], comprobanteUrl: [''] });
  constructor() { this.cargar(); }
  cargar(): void { this.resourcesService.consultarMisAusencias().subscribe({ next: (ausencias: AusenciaEmpleado[]) => this.ausencias.set(ausencias), error: error => this.error.set(typeof error?.error === 'string' ? error.error : error?.error?.message || 'No se pudieron cargar las ausencias.') }); }
  enviar(): void { if (this.form.invalid) { this.form.markAllAsTouched(); return; } this.enviando.set(true); this.error.set(''); this.mensaje.set(''); this.tareasAfectadas.set([]); const ausencia: AusenciaEmpleado = { ...this.form.getRawValue(), activo: true }; this.resourcesService.registrarAusencia(ausencia).subscribe({ next: (resultado) => { this.ausencias.set([resultado.ausencia, ...this.ausencias()]); this.tareasAfectadas.set(resultado.tareasAfectadas); this.form.reset({ tipoPeriodo: 'Licencia', fechaInicioSolicitada: '', fechaFinSolicitada: '', horasNoDisponiblesSolicitadas: null, motivo: '', comprobanteUrl: '' }); this.enviando.set(false); this.mensaje.set('Solicitud registrada y enviada para aprobación.'); }, error: error => { this.enviando.set(false); this.error.set(typeof error?.error === 'string' ? error.error : error?.error?.message || error?.error?.title || 'No se pudo registrar la solicitud.'); } }); }
}

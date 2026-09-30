import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { LocalizationService } from '../../../../services/localization.service';
import { ResourcesService } from '../../../../services/resources.service';

@Component({
  selector: 'app-availability',
  imports: [ReactiveFormsModule, RouterModule],
  templateUrl: './availability.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AvailabilityComponent {
  readonly localization = inject(LocalizationService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly resourcesService = inject(ResourcesService);
  protected readonly cargando = signal(true);
  protected readonly guardando = signal(false);
  protected readonly error = signal('');
  protected readonly mensaje = signal('');
  protected readonly form = this.formBuilder.group({ horaInicio: ['09:00', Validators.required], horaFin: ['18:00', Validators.required], horasSemanales: [40, [Validators.required, Validators.min(1)]], observacion: [''] });

  constructor() {
    this.resourcesService.consultarMiDisponibilidad().subscribe({
      next: empleado => {
        const disponibilidad = empleado.disponibilidadBase;
        if (disponibilidad) { this.form.patchValue({ horaInicio: disponibilidad.horaInicio, horaFin: disponibilidad.horaFin, horasSemanales: disponibilidad.horasSemanales, observacion: disponibilidad.observacion ?? '' }); }
        this.cargando.set(false);
      },
      error: () => { this.error.set('No fue posible cargar tu disponibilidad.'); this.cargando.set(false); },
    });
  }

  protected guardar(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const datos = this.form.getRawValue();
    this.guardando.set(true);
    this.error.set('');
    this.mensaje.set('');
    this.resourcesService.guardarMiDisponibilidad({ disponibilidadBase: { horaInicio: datos.horaInicio ?? '09:00', horaFin: datos.horaFin ?? '18:00', horasSemanales: Number(datos.horasSemanales ?? 0), observacion: datos.observacion ?? '', activo: true } }).subscribe({
      next: () => { this.guardando.set(false); this.mensaje.set('Tu disponibilidad fue actualizada.'); },
      error: error => { this.guardando.set(false); this.error.set(error?.error || 'No fue posible actualizar tu disponibilidad.'); },
    });
  }
}

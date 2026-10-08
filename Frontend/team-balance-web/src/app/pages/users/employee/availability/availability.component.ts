import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../../services/auth.service';
import { LocalizationService } from '../../../../services/localization.service';
import { AusenciaEmpleado, EmpleadoDisponibilidadOpcion, ResolucionAusenciaEmpleado, ResourcesService, TareaDisponibilidadAfectada } from '../../../../services/resources.service';

@Component({
  selector: 'app-availability',
  imports: [DatePipe, FormsModule, ReactiveFormsModule, RouterModule],
  templateUrl: './availability.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AvailabilityComponent {
  readonly localization = inject(LocalizationService);
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly resourcesService = inject(ResourcesService);
  protected readonly cargando = signal(true);
  protected readonly guardando = signal(false);
  protected readonly error = signal('');
  protected readonly mensaje = signal('');
  protected readonly pendientes = signal<AusenciaEmpleado[]>([]);
  protected readonly empleados = signal<EmpleadoDisponibilidadOpcion[]>([]);
  protected readonly solicitudSeleccionada = signal<AusenciaEmpleado | null>(null);
  protected readonly tareasAfectadas = signal<TareaDisponibilidadAfectada[]>([]);
  protected readonly procesandoSolicitud = signal(false);
  protected readonly esGestor = this.authService.usuarioActual()?.roles?.some(rol => rol.tipoUsuario === 'PM' || rol.tipoUsuario === 'Dueno') ?? false;
  protected readonly form = this.formBuilder.group({ horaInicio: ['09:00', Validators.required], horaFin: ['18:00', Validators.required], horasSemanales: [40, [Validators.required, Validators.min(1)]], observacion: [''] });
  protected readonly directaForm = this.formBuilder.group({ idEmpleado: [0, [Validators.required, Validators.min(1)]], tipoPeriodo: ['Licencia', Validators.required], fechaInicioSolicitada: ['', Validators.required], fechaFinSolicitada: ['', Validators.required], horasNoDisponiblesSolicitadas: [null as number | null], motivo: ['', Validators.required], comprobanteUrl: [''] });
  protected fechaInicioAprobada = ''; protected fechaFinAprobada = ''; protected horasAprobadas: number | null = null; protected motivoResolucion = '';

  constructor() {
    if (this.esGestor) { this.cargarGestion(); return; }
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
    this.resourcesService.guardarMiDisponibilidad({ horaInicio: datos.horaInicio ?? '09:00', horaFin: datos.horaFin ?? '18:00', horasSemanales: Number(datos.horasSemanales ?? 0), observacion: datos.observacion ?? '', activo: true }).subscribe({
      next: () => { this.guardando.set(false); this.mensaje.set('Tu disponibilidad fue actualizada.'); },
      error: error => { this.guardando.set(false); this.error.set(typeof error?.error === 'string' ? error.error : error?.error?.message || error?.error?.title || 'No fue posible actualizar tu disponibilidad.'); },
    });
  }

  protected seleccionarSolicitud(solicitud: AusenciaEmpleado): void {
    this.solicitudSeleccionada.set(solicitud); this.fechaInicioAprobada = solicitud.fechaInicioSolicitada.slice(0, 10); this.fechaFinAprobada = solicitud.fechaFinSolicitada.slice(0, 10); this.horasAprobadas = solicitud.horasNoDisponiblesSolicitadas ?? null; this.motivoResolucion = ''; this.tareasAfectadas.set([]); this.error.set(''); this.mensaje.set('');
  }

  protected aprobar(parcial: boolean): void {
    const solicitud = this.solicitudSeleccionada(); if (!solicitud?.id) { return; }
    this.procesandoSolicitud.set(true); this.error.set(''); this.mensaje.set('');
    const resolucion: ResolucionAusenciaEmpleado = { fechaInicioAprobada: this.fechaInicioAprobada, fechaFinAprobada: this.fechaFinAprobada, horasNoDisponiblesAprobadas: this.horasAprobadas, motivoResolucion: this.motivoResolucion };
    const operacion = parcial ? this.resourcesService.aprobarParcialmenteAusencia(solicitud.id, resolucion) : this.resourcesService.aprobarAusencia(solicitud.id, resolucion);
    operacion.subscribe({ next: resultado => { this.procesandoSolicitud.set(false); this.pendientes.update(items => items.filter(item => item.id !== solicitud.id)); this.tareasAfectadas.set(resultado.tareasAfectadas); this.solicitudSeleccionada.set(null); this.mensaje.set(parcial ? 'Solicitud aprobada parcialmente y disponibilidad actualizada.' : 'Solicitud aprobada y disponibilidad actualizada.'); }, error: error => { this.procesandoSolicitud.set(false); this.error.set(this.mensajeError(error)); } });
  }

  protected rechazar(): void {
    const solicitud = this.solicitudSeleccionada(); if (!solicitud?.id) { return; }
    this.procesandoSolicitud.set(true); this.error.set(''); this.resourcesService.rechazarAusencia(solicitud.id, { motivoResolucion: this.motivoResolucion }).subscribe({ next: () => { this.procesandoSolicitud.set(false); this.pendientes.update(items => items.filter(item => item.id !== solicitud.id)); this.solicitudSeleccionada.set(null); this.mensaje.set('Solicitud rechazada. La disponibilidad no fue modificada.'); }, error: error => { this.procesandoSolicitud.set(false); this.error.set(this.mensajeError(error)); } });
  }

  protected registrarDirecta(): void {
    if (this.directaForm.invalid) { this.directaForm.markAllAsTouched(); return; }
    this.procesandoSolicitud.set(true); this.error.set(''); this.mensaje.set(''); const datos = this.directaForm.getRawValue();
    this.resourcesService.registrarAusenciaDirecta({ idEmpleado: Number(datos.idEmpleado), tipoPeriodo: datos.tipoPeriodo ?? '', fechaInicioSolicitada: datos.fechaInicioSolicitada ?? '', fechaFinSolicitada: datos.fechaFinSolicitada ?? '', horasNoDisponiblesSolicitadas: datos.horasNoDisponiblesSolicitadas, motivo: datos.motivo ?? '', comprobanteUrl: datos.comprobanteUrl ?? '' }).subscribe({ next: resultado => { this.procesandoSolicitud.set(false); this.tareasAfectadas.set(resultado.tareasAfectadas); this.directaForm.reset({ idEmpleado: 0, tipoPeriodo: 'Licencia', fechaInicioSolicitada: '', fechaFinSolicitada: '', horasNoDisponiblesSolicitadas: null, motivo: '', comprobanteUrl: '' }); this.mensaje.set('Período registrado directamente como aprobado.'); }, error: error => { this.procesandoSolicitud.set(false); this.error.set(this.mensajeError(error)); } });
  }

  private cargarGestion(): void { this.cargando.set(true); this.resourcesService.consultarSolicitudesPendientes().subscribe({ next: solicitudes => { this.pendientes.set(solicitudes); this.cargando.set(false); }, error: error => { this.error.set(this.mensajeError(error)); this.cargando.set(false); } }); this.resourcesService.consultarEmpleadosDisponibilidad().subscribe({ next: empleados => this.empleados.set(empleados), error: error => this.error.set(this.mensajeError(error)) }); }
  private mensajeError(error: any): string { return typeof error?.error === 'string' ? error.error : error?.error?.message || error?.error?.title || 'No fue posible completar la operación.'; }
}

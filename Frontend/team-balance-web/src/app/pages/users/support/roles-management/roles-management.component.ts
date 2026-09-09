import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { Permiso, Rol, RolesService } from '../../../../services/roles.service';

@Component({ selector: 'app-rol-management', imports: [ReactiveFormsModule], templateUrl: './roles-management.component.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class RolManagement {
  private readonly formBuilder = inject(FormBuilder);
  private readonly rolesService = inject(RolesService);
  readonly roles = signal<Rol[]>([]);
  readonly permisos = signal<Permiso[]>([]);
  readonly permisosSeleccionados = signal<number[]>([]);
  readonly editando = signal<Rol | null>(null);
  readonly error = signal('');
  readonly mensaje = signal('');
  readonly form = this.formBuilder.group({ nombre: ['', Validators.required], descripcion: [''] });

  constructor() { this.cargar(); }

  cargar(): void { forkJoin({ roles: this.rolesService.consultarRoles(), permisos: this.rolesService.consultarPermisos() }).subscribe({ next: ({ roles, permisos }) => { this.roles.set(roles); this.permisos.set(permisos); }, error: () => this.error.set('No fue posible cargar roles y permisos.') }); }
  editar(rol: Rol): void { this.editando.set(rol); this.permisosSeleccionados.set(rol.permisos.map(permiso => permiso.id)); this.form.patchValue({ nombre: rol.nombre, descripcion: rol.descripcion ?? '' }); }
  cancelar(): void { this.editando.set(null); this.permisosSeleccionados.set([]); this.form.reset(); }
  cambiarPermiso(idPermiso: number, seleccionado: boolean): void { this.permisosSeleccionados.update(permisos => seleccionado ? [...new Set([...permisos, idPermiso])] : permisos.filter(id => id !== idPermiso)); }
  guardar(): void {
    this.error.set(''); this.mensaje.set('');
    if (this.form.invalid || this.permisosSeleccionados().length === 0) { this.error.set('Completá el nombre y asigná al menos un permiso.'); return; }
    const datos = this.form.getRawValue();
    const rol: Rol = { id: this.editando()?.id ?? 0, nombre: datos.nombre ?? '', descripcion: datos.descripcion, esRolBase: false, activo: true, permisos: this.permisos().filter(permiso => this.permisosSeleccionados().includes(permiso.id)) };
    if (this.editando())
    {
      this.rolesService.modificarRol(rol).subscribe({ next: () => { this.mensaje.set('Rol guardado correctamente.'); this.cancelar(); this.cargar(); }, error: (respuesta: { error?: string }) => this.error.set(respuesta.error || 'No fue posible guardar el rol.') });
      return;
    }
    this.rolesService.registrarRol(rol).subscribe({ next: () => { this.mensaje.set('Rol guardado correctamente.'); this.cancelar(); this.cargar(); }, error: (respuesta: { error?: string }) => this.error.set(respuesta.error || 'No fue posible guardar el rol.') });
  }
  cambiarEstado(rol: Rol): void { this.rolesService.cambiarEstado(rol.id, !rol.activo).subscribe({ next: () => this.cargar(), error: (respuesta: { error?: string }) => this.error.set(respuesta.error || 'No fue posible actualizar el rol.') }); }
}

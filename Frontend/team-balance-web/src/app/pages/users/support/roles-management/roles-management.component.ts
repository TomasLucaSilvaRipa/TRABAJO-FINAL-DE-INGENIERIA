import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { Permiso, Rol, RolesService, TipoUsuarioRol } from '../../../../services/roles.service';
import { AuthService } from '../../../../services/auth.service';
import { LocalizationService } from '../../../../services/localization.service';

@Component({ selector: 'app-rol-management', imports: [ReactiveFormsModule], templateUrl: './roles-management.component.html', styleUrl: './roles-management.component.css', changeDetection: ChangeDetectionStrategy.OnPush })
export class RolManagement {
  private readonly formBuilder = inject(FormBuilder);
  private readonly rolesService = inject(RolesService);
  private readonly authService = inject(AuthService);
  readonly localization = inject(LocalizationService);
  readonly roles = signal<Rol[]>([]);
  readonly permisos = signal<Permiso[]>([]);
  readonly permisosSeleccionados = signal<number[]>([]);
  readonly editando = signal<Rol | null>(null);
  readonly error = signal('');
  readonly mensaje = signal('');
  private readonly permisosInternos = new Set([
    'consultarbitacora', 'gestionarplanes', 'gestionarbandejasoporte', 'gestionarnovedades', 'gestionarfaqs',
    'gestionarencuestas', 'gestionaroperadores', 'gestionarrespaldos', 'consultaringresosplataforma', 'gestionaridiomas',
  ]);
  readonly form = this.formBuilder.group({ nombre: ['', Validators.required], descripcion: [''], tipoUsuario: ['', Validators.required] });
  readonly tiposUsuario: { value: TipoUsuarioRol; label: string; description: string }[] = [
    { value: 'Dueno', label: 'roles.owner', description: 'roles.ownerHelp' },
    { value: 'PM', label: 'roles.pm', description: 'roles.pmHelp' },
    { value: 'Empleado', label: 'roles.employee', description: 'roles.employeeHelp' },
    { value: 'Soporte', label: 'roles.support', description: 'roles.supportHelp' },
  ];
  readonly tiposUsuarioVisibles = computed(() => this.authService.esSoporte() ? this.tiposUsuario : this.tiposUsuario.filter(tipo => tipo.value !== 'Soporte'));

  constructor() { this.cargar(); }

  cargar(): void { forkJoin({ roles: this.rolesService.consultarRoles(), permisos: this.rolesService.consultarPermisos() }).subscribe({ next: ({ roles, permisos }) => { this.roles.set(roles); this.permisos.set(permisos); }, error: () => this.error.set('roles.loadFailed') }); }
  editar(rol: Rol): void { this.editando.set(rol); this.form.patchValue({ nombre: rol.nombre, descripcion: rol.descripcion ?? '', tipoUsuario: rol.tipoUsuario }); this.permisosSeleccionados.set(rol.permisos.filter(permiso => this.puedeAsignarPermiso(permiso)).map(permiso => permiso.id)); }
  cancelar(): void { this.editando.set(null); this.permisosSeleccionados.set([]); this.form.reset({ tipoUsuario: '' }); }
  cambiarPermiso(idPermiso: number, seleccionado: boolean): void { this.permisosSeleccionados.update(permisos => seleccionado ? [...new Set([...permisos, idPermiso])] : permisos.filter(id => id !== idPermiso)); }
  puedeAsignarPermiso(permiso: Permiso): boolean {
    return !this.permisosInternos.has(permiso.codigo.toLowerCase()) ||
      (this.authService.esSoporte() && this.form.controls.tipoUsuario.value === 'Soporte');
  }
  cambiarTipoUsuario(tipoUsuario: TipoUsuarioRol): void {
    this.form.controls.tipoUsuario.setValue(tipoUsuario);
    const visibles = new Set(this.permisos().filter(permiso => this.puedeAsignarPermiso(permiso)).map(permiso => permiso.id));
    this.permisosSeleccionados.update(permisos => permisos.filter(id => visibles.has(id)));
  }
  guardar(): void {
    this.error.set(''); this.mensaje.set('');
    const datos = this.form.getRawValue();
    const tipoUsuario = datos.tipoUsuario as TipoUsuarioRol;
    const permisosValidos = this.permisos().filter(permiso => this.permisosSeleccionados().includes(permiso.id) && this.puedeAsignarPermiso(permiso));
    if (this.form.invalid || permisosValidos.length === 0) { this.error.set('roles.invalidSelection'); return; }
    const rol: Rol = { id: this.editando()?.id ?? 0, idAgencia: this.editando()?.idAgencia ?? null, nombre: datos.nombre ?? '', descripcion: datos.descripcion, tipoUsuario, esRolBase: false, activo: true, permisos: permisosValidos };
    if (this.editando())
    {
      this.rolesService.modificarRol(rol).subscribe({ next: () => { this.mensaje.set('roles.saved'); this.cancelar(); this.cargar(); }, error: (respuesta: { error?: unknown }) => this.mostrarErrorGuardado(respuesta) });
      return;
    }
    this.rolesService.registrarRol(rol).subscribe({ next: () => { this.mensaje.set('roles.saved'); this.cancelar(); this.cargar(); }, error: (respuesta: { error?: unknown }) => this.mostrarErrorGuardado(respuesta) });
  }
  cambiarEstado(rol: Rol): void { this.rolesService.cambiarEstado({ ...rol, activo: !rol.activo }).subscribe({ next: () => this.cargar(), error: () => this.error.set('roles.updateFailed') }); }
  private mostrarErrorGuardado(respuesta: { error?: unknown }): void {
    const esPermisoInterno = typeof respuesta.error === 'string' && /permisos internos de backoffice/i.test(respuesta.error);
    this.error.set(esPermisoInterno ? 'roles.internalPermissionsOnly' : 'roles.saveFailed');
  }
}

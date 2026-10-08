import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin, Observable } from 'rxjs';
import { LocalizationService } from '../../../../services/localization.service';
import { Rol, RolesService } from '../../../../services/roles.service';
import { UsuarioGestion, UsuariosService } from '../../../../services/usuarios.service';

@Component({
  selector: 'app-operators-management',
  imports: [ReactiveFormsModule],
  templateUrl: './operators-management.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OperatorsManagementComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly usuariosService = inject(UsuariosService);
  private readonly rolesService = inject(RolesService);
  readonly localization = inject(LocalizationService);
  readonly operadores = signal<UsuarioGestion[]>([]);
  readonly rolesSoporte = signal<Rol[]>([]);
  readonly rolesSeleccionados = signal<number[]>([]);
  readonly editando = signal<UsuarioGestion | null>(null);
  readonly guardando = signal(false);
  readonly error = signal('');
  readonly mensaje = signal('');
  readonly titulo = computed(() => this.localization.traducir(this.editando() ? 'operators.editTitle' : 'operators.newTitle'));
  readonly form = this.formBuilder.group({
    nombre: ['', [Validators.required, Validators.minLength(2)]],
    apellido: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    passwordHash: [''],
  });

  constructor() { this.cargar(); }

  cargar(): void {
    forkJoin({ operadores: this.usuariosService.consultarOperadores(), roles: this.rolesService.consultarRoles() }).subscribe({
      next: ({ operadores, roles }) => { this.operadores.set(operadores); this.rolesSoporte.set(roles.filter((rol) => rol.tipoUsuario === 'Soporte' && rol.activo)); },
      error: (respuesta) => this.error.set(this.mensajeError(respuesta, 'operators.loadError')),
    });
  }

  alternarRol(idRol: number, seleccionado: boolean): void {
    this.rolesSeleccionados.update((actuales) => seleccionado ? [...new Set([...actuales, idRol])] : actuales.filter((id) => id !== idRol));
  }

  tieneRol(idRol: number): boolean { return this.rolesSeleccionados().includes(idRol); }

  editar(operador: UsuarioGestion): void {
    this.editando.set(operador);
    this.error.set(''); this.mensaje.set('');
    this.rolesSeleccionados.set(operador.roles.map((rol) => rol.id));
    this.form.reset({ nombre: operador.nombre, apellido: operador.apellido, email: operador.email, passwordHash: '' });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  nuevo(): void {
    this.editando.set(null); this.rolesSeleccionados.set([]); this.error.set(''); this.mensaje.set('');
    this.form.reset({ nombre: '', apellido: '', email: '', passwordHash: '' });
  }

  guardar(): void {
    this.form.markAllAsTouched();
    const editando = this.editando();
    if (!editando && !this.form.controls.passwordHash.value?.trim()) this.form.controls.passwordHash.setErrors({ required: true });
    if (this.form.invalid || this.rolesSeleccionados().length === 0 || this.guardando()) {
      this.error.set(this.rolesSeleccionados().length === 0 ? this.localization.traducir('operators.roleRequired') : this.localization.traducir('operators.required'));
      return;
    }
    const valores = this.form.getRawValue();
    const operador: UsuarioGestion = {
      id: editando?.id ?? 0,
      nombre: valores.nombre?.trim() ?? '', apellido: valores.apellido?.trim() ?? '', email: valores.email?.trim() ?? '',
      passwordHash: valores.passwordHash?.trim() || undefined,
      estado: editando?.estado ?? 'PendienteValidacion', activo: editando?.activo ?? true,
      roles: this.rolesSoporte().filter((rol) => this.rolesSeleccionados().includes(rol.id)),
    };
    this.guardando.set(true); this.error.set(''); this.mensaje.set('');
    const operacion: Observable<void | UsuarioGestion> = editando ? this.usuariosService.modificarOperador(operador) : this.usuariosService.registrarOperador(operador);
    operacion.subscribe({
      next: () => { this.guardando.set(false); this.mensaje.set(this.localization.traducir(editando ? 'operators.updated' : 'operators.created')); this.nuevo(); this.mensaje.set(this.localization.traducir(editando ? 'operators.updated' : 'operators.created')); this.cargar(); },
      error: (respuesta: { error?: string }) => { this.guardando.set(false); this.error.set(this.mensajeError(respuesta, 'operators.saveError')); },
    });
  }

  cambiarEstado(operador: UsuarioGestion): void {
    this.error.set('');
    this.usuariosService.cambiarEstadoOperador({ id: operador.id, activo: !operador.activo }).subscribe({
      next: () => { this.mensaje.set(this.localization.traducir(operador.activo ? 'operators.deactivated' : 'operators.activated')); this.cargar(); },
      error: (respuesta) => this.error.set(this.mensajeError(respuesta, 'operators.statusError')),
    });
  }

  nombresRoles(operador: UsuarioGestion): string { return operador.roles.map((rol) => rol.nombre).join(', '); }

  private mensajeError(respuesta: { error?: string }, clave: string): string { return typeof respuesta?.error === 'string' ? respuesta.error : this.localization.traducir(clave); }
}

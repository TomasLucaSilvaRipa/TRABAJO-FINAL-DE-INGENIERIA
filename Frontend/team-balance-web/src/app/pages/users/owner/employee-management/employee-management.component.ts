import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { Rol, RolesService } from '../../../../services/roles.service';
import { PerfilEmpleado, UsuarioGestion, UsuariosService } from '../../../../services/usuarios.service';

@Component({ selector: 'app-employee-management', imports: [ReactiveFormsModule], templateUrl: './employee-management.component.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class EmployeeManagement {
  private readonly formBuilder = inject(FormBuilder);
  private readonly usuariosService = inject(UsuariosService);
  private readonly rolesService = inject(RolesService);
  readonly usuarios = signal<UsuarioGestion[]>([]);
  readonly roles = signal<Rol[]>([]);
  readonly soporteInicialDisponible = signal(false);
  readonly cargando = signal(true);
  readonly error = signal('');
  readonly mensaje = signal('');
  readonly editandoId = signal<number | null>(null);
  readonly rolesSeleccionados = signal<number[]>([]);
  readonly form = this.formBuilder.group({ nombre: ['', Validators.required], apellido: ['', Validators.required], email: ['', [Validators.required, Validators.email]], passwordHash: [''], costoHora: [0, Validators.min(0)], horasDisponiblesSemanales: [40, Validators.min(1)], seniority: ['Junior'], estadoLaboral: ['Activo'] });

  constructor() { this.cargar(); }

  cargar(): void {
    this.cargando.set(true);
    forkJoin({ usuarios: this.usuariosService.consultarUsuarios(), roles: this.rolesService.consultarRolesAsignablesAgencia(), soporte: this.usuariosService.soporteInicialDisponible() }).subscribe({
      next: ({ usuarios, roles, soporte }) => {
        this.usuarios.set(usuarios);
        this.soporteInicialDisponible.set(soporte.disponible);
        if (!soporte.disponible) { this.roles.set(roles); this.cargando.set(false); return; }
        this.usuariosService.consultarRolSoporteInicial().subscribe({
          next: (rolSoporte) => { this.roles.set([...roles, rolSoporte]); this.cargando.set(false); },
          error: () => { this.roles.set(roles); this.cargando.set(false); },
        });
      },
      error: () => { this.error.set('No fue posible cargar los usuarios y roles de la agencia.'); this.cargando.set(false); },
    });
  }

  guardar(): void {
    this.error.set(''); this.mensaje.set('');
    if (this.form.invalid || this.rolesSeleccionados().length === 0 || (this.editandoId() === null && !this.form.controls.passwordHash.value)) { this.error.set('Completá los datos obligatorios, la contraseña temporal y al menos un rol.'); return; }
    const datos = this.form.getRawValue();
    const usuario: Partial<UsuarioGestion> = { id: this.editandoId() ?? 0, nombre: datos.nombre ?? '', apellido: datos.apellido ?? '', email: datos.email ?? '', passwordHash: datos.passwordHash ?? '', roles: this.roles().filter(rol => this.rolesSeleccionados().includes(rol.id)), empleado: this.tieneRol('Empleado') ? this.crearPerfilEmpleado() : null };
    if (this.editandoId() === null)
    {
      this.usuariosService.registrarUsuario(usuario).subscribe({ next: () => { this.mensaje.set('Usuario registrado. Se envió un correo para validar su cuenta.'); this.cancelarEdicion(); this.cargar(); }, error: (respuesta: { error?: string }) => this.error.set(respuesta.error || 'No fue posible guardar el usuario.') });
      return;
    }
    this.usuariosService.modificarUsuario(usuario as UsuarioGestion).subscribe({ next: () => { this.mensaje.set('Usuario actualizado correctamente.'); this.cancelarEdicion(); this.cargar(); }, error: (respuesta: { error?: string }) => this.error.set(respuesta.error || 'No fue posible guardar el usuario.') });
  }

  editar(usuario: UsuarioGestion): void { this.editandoId.set(usuario.id); this.rolesSeleccionados.set(usuario.roles.map(rol => rol.id)); this.form.patchValue({ nombre: usuario.nombre, apellido: usuario.apellido, email: usuario.email, passwordHash: '', costoHora: usuario.empleado?.costoHora ?? 0, horasDisponiblesSemanales: usuario.empleado?.horasDisponiblesSemanales ?? 40, seniority: usuario.empleado?.seniority ?? 'Junior', estadoLaboral: usuario.empleado?.estadoLaboral ?? 'Activo' }); }
  cancelarEdicion(): void { this.editandoId.set(null); this.rolesSeleccionados.set([]); this.form.reset({ costoHora: 0, horasDisponiblesSemanales: 40, seniority: 'Junior', estadoLaboral: 'Activo' }); }
  cambiarRol(idRol: number, seleccionado: boolean): void { this.rolesSeleccionados.update(roles => seleccionado ? [...new Set([...roles, idRol])] : roles.filter(id => id !== idRol)); }
  cambiarEstado(usuario: UsuarioGestion): void { this.usuariosService.cambiarEstado(usuario.id, !usuario.activo).subscribe({ next: () => this.cargar(), error: (respuesta: { error?: string }) => this.error.set(respuesta.error || 'No fue posible actualizar el estado.') }); }
  tieneRol(nombre: string): boolean { return this.rolesSeleccionados().some(id => this.nombreRol(id) === nombre); }
  nombreRol(idRol: number): string { return this.roles().find(rol => rol.id === idRol)?.nombre ?? ''; }
  rolesTexto(usuario: UsuarioGestion): string { return usuario.roles.map(rol => rol.nombre).join(', '); }
  private crearPerfilEmpleado(): PerfilEmpleado { const datos = this.form.getRawValue(); return { costoHora: Number(datos.costoHora ?? 0), horasDisponiblesSemanales: Number(datos.horasDisponiblesSemanales ?? 40), seniority: datos.seniority ?? 'Junior', estadoLaboral: datos.estadoLaboral ?? 'Activo' }; }
}

import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { Rol, RolesService } from '../../../../services/roles.service';
import { AreaSkill, FichaEmpleado, ResourcesService, Skill } from '../../../../services/resources.service';
import { PerfilEmpleado, UsuarioGestion, UsuariosService } from '../../../../services/usuarios.service';
import { LocalizationService } from '../../../../services/localization.service';
import { AuthService } from '../../../../services/auth.service';

@Component({ selector: 'app-employee-management', imports: [ReactiveFormsModule], templateUrl: './employee-management.component.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class EmployeeManagement {
  private readonly formBuilder = inject(FormBuilder); private readonly usuariosService = inject(UsuariosService); private readonly rolesService = inject(RolesService); private readonly recursosService = inject(ResourcesService); private readonly authService = inject(AuthService);
  readonly localization = inject(LocalizationService);
  readonly usuarios = signal<UsuarioGestion[]>([]); readonly roles = signal<Rol[]>([]); readonly skills = signal<Skill[]>([]); readonly areasSkills = signal<AreaSkill[]>([]); readonly soporteInicialDisponible = signal(false); readonly cargando = signal(true); readonly error = signal(''); readonly mensaje = signal(''); readonly editandoId = signal<number | null>(null); readonly rolesSeleccionados = signal<number[]>([]); readonly skillsSeleccionadas = signal<number[]>([]); readonly usuarioFicha = signal<UsuarioGestion | null>(null); readonly fichaConsultada = signal<FichaEmpleado | null>(null); readonly cargandoFicha = signal(false); readonly exportandoFicha = signal(false); readonly panelFichaAbierto = signal(false);
  readonly busquedaSimple = signal(''); readonly mostrarBusquedaAvanzada = signal(false); readonly filtroNombre = signal(''); readonly filtroEmail = signal(''); readonly filtroRol = signal(''); readonly filtroEstado = signal(''); readonly filtroSeniority = signal('');
  private cierreFicha: ReturnType<typeof setTimeout> | null = null;
  readonly form = this.formBuilder.group({ nombre: ['', Validators.required], apellido: ['', Validators.required], email: ['', [Validators.required, Validators.email]], passwordHash: [''], costoHora: [0, Validators.min(0)], horasDisponiblesSemanales: [40, Validators.min(1)], seniority: ['Junior'], estadoLaboral: ['Activo'], horaInicio: ['09:00', Validators.required], horaFin: ['18:00', Validators.required], observacionDisponibilidad: [''] });
  readonly skillForm = this.formBuilder.group({ nombre: ['', Validators.required], idAreaSkill: [null as number | null, Validators.required] });
  constructor() { this.cargar(); }
  cargar(): void {
    this.cargando.set(true);
    forkJoin({ usuarios: this.usuariosService.consultarUsuarios(), roles: this.rolesService.consultarRolesAsignablesAgencia(), soporte: this.usuariosService.soporteInicialDisponible(),
      skills: this.recursosService.consultarSkills(), areasSkills: this.recursosService.consultarAreasSkills() }).subscribe({ next: ({ usuarios, roles, soporte, skills, areasSkills }) => {
        this.usuarios.set(usuarios);
        this.skills.set(skills);
        this.areasSkills.set(areasSkills);
        this.soporteInicialDisponible.set(soporte.disponible);
        if (!soporte.disponible) {
          this.roles.set(roles);
          this.cargando.set(false); return;
        }
        this.usuariosService.consultarRolSoporteInicial().subscribe({ next: (rolSoporte) => {
          this.roles.set([...roles, rolSoporte]); this.cargando.set(false);
        }, error: () => { this.roles.set(roles);
          this.cargando.set(false);
        }
      });
    }, error: () => { this.error.set('No fue posible cargar los usuarios, roles y skills de la agencia.'); this.cargando.set(false); } });
  }

  guardar(): void {
    this.error.set(''); this.mensaje.set('');
    if (this.form.invalid || this.rolesSeleccionados().length === 0 || (this.editandoId() === null && !this.form.controls.passwordHash.value)) { this.error.set('Completá los datos obligatorios, la contraseña temporal y al menos un rol.'); return; }
    if (this.tieneTipoUsuario('Empleado') && this.skillsSeleccionadas().length === 0) { this.error.set('Seleccioná al menos una skill para el empleado.'); return; }
    const datos = this.form.getRawValue(); const usuario: Partial<UsuarioGestion> = { id: this.editandoId() ?? 0, nombre: datos.nombre ?? '', apellido: datos.apellido ?? '', email: datos.email ?? '', passwordHash: datos.passwordHash ?? '', estado: 'PendienteValidacion', activo: true, roles: this.roles().filter(rol => this.rolesSeleccionados().includes(rol.id)), empleado: this.tieneTipoUsuario('Empleado') ? this.crearPerfilEmpleado() : null };
    if (this.editandoId() === null) { this.usuariosService.registrarUsuario(usuario).subscribe({ next: (registrado) => this.guardarFichaSiCorresponde(registrado.id, 'Usuario registrado. Se envió un correo para validar su cuenta.'), error: (respuesta: unknown) => this.error.set(this.obtenerMensajeError(respuesta, 'No fue posible guardar el usuario.')) }); return; }
    this.usuariosService.modificarUsuario(usuario as UsuarioGestion).subscribe({ next: () => this.guardarFichaSiCorresponde(this.editandoId()!, 'Usuario actualizado correctamente.'), error: (respuesta: unknown) => this.error.set(this.obtenerMensajeError(respuesta, 'No fue posible guardar el usuario.')) });
  }
  editar(usuario: UsuarioGestion): void {
    this.editandoId.set(usuario.id);
    this.rolesSeleccionados.set(usuario.roles.map(rol => rol.id)); this.skillsSeleccionadas.set([]);
    this.form.patchValue({ nombre: usuario.nombre, apellido: usuario.apellido, email: usuario.email, passwordHash: '', costoHora: usuario.empleado?.costoHora ?? 0, horasDisponiblesSemanales: usuario.empleado?.horasDisponiblesSemanales ?? 40, seniority: usuario.empleado?.seniority ?? 'Junior', estadoLaboral: usuario.empleado?.estadoLaboral ?? 'Activo', horaInicio: '09:00', horaFin: '18:00', observacionDisponibilidad: '' });
    if (usuario.empleado) {
      this.recursosService.consultarFichaEmpleado(usuario.id).subscribe({ next: (ficha) => { this.skillsSeleccionadas.set(ficha.empleadoSkills.map(skill => skill.idSkill));
        this.form.patchValue({ horaInicio: ficha.disponibilidadBase?.horaInicio ?? '09:00', horaFin: ficha.disponibilidadBase?.horaFin ?? '18:00', observacionDisponibilidad: ficha.disponibilidadBase?.observacion ?? '', horasDisponiblesSemanales: ficha.disponibilidadBase?.horasSemanales ?? usuario.empleado?.horasDisponiblesSemanales ?? 40 }); }, error: () => this.error.set('No fue posible cargar la ficha laboral del empleado.')
      });
    }
  }

  consultarFicha(usuario: UsuarioGestion): void {
    if (!usuario.empleado) { return; }
    if (this.cierreFicha) { clearTimeout(this.cierreFicha); this.cierreFicha = null; }
    this.error.set(''); this.usuarioFicha.set(usuario); this.fichaConsultada.set(null); this.cargandoFicha.set(true);
    requestAnimationFrame(() => this.panelFichaAbierto.set(true));
    this.recursosService.consultarFichaEmpleado(usuario.id).subscribe({
      next: ficha => { this.fichaConsultada.set(ficha); this.cargandoFicha.set(false); },
      error: respuesta => { this.cargandoFicha.set(false); this.usuarioFicha.set(null); this.error.set(this.obtenerMensajeError(respuesta, 'No fue posible consultar la ficha laboral del empleado.')); }
    });
  }

  cerrarFicha(): void {
    this.panelFichaAbierto.set(false);
    if (this.cierreFicha) { clearTimeout(this.cierreFicha); }
    this.cierreFicha = setTimeout(() => { this.usuarioFicha.set(null); this.fichaConsultada.set(null); this.cargandoFicha.set(false); this.cierreFicha = null; }, 180);
  }

  nombreSkill(idSkill: number): string { return this.skills().find(skill => skill.id === idSkill)?.nombre ?? `Skill #${idSkill}`; }

  esDueno(): boolean { return this.authService.usuarioActual()?.roles.some(rol => rol.tipoUsuario === 'Dueno') ?? false; }

  exportarFicha(): void {
    const usuario = this.usuarioFicha(); const ficha = this.fichaConsultada();
    if (!usuario || !ficha) { return; }
    const ventana = window.open('', '_blank', 'popup');
    if (!ventana) { this.error.set('El navegador bloqueó la ventana de exportación. Habilitá las ventanas emergentes e intentá nuevamente.'); return; }
    this.exportandoFicha.set(true); this.error.set('');
    this.recursosService.registrarExportacionFichaEmpleado(usuario.id).subscribe({
      next: () => { this.exportandoFicha.set(false); this.abrirImpresionLegajo(ventana, usuario, ficha); },
      error: respuesta => { ventana.close(); this.exportandoFicha.set(false); this.error.set(this.obtenerMensajeError(respuesta, 'No fue posible preparar la exportación del legajo.')); }
    });
  }

  cancelarEdicion(): void {
    this.editandoId.set(null); this.rolesSeleccionados.set([]);
    this.skillsSeleccionadas.set([]); this.form.reset({ costoHora: 0, horasDisponiblesSemanales: 40, seniority: 'Junior', estadoLaboral: 'Activo', horaInicio: '09:00', horaFin: '18:00', observacionDisponibilidad: '' });
  }

  cambiarRol(idRol: number, seleccionado: boolean): void { this.rolesSeleccionados.update(roles => seleccionado ? [...new Set([...roles, idRol])] : roles.filter(id => id !== idRol)); }
  cambiarSkill(idSkill: number, seleccionado: boolean): void { this.skillsSeleccionadas.update(skills => seleccionado ? [...new Set([...skills, idSkill])] : skills.filter(id => id !== idSkill)); }
  registrarSkill(): void { if (this.skillForm.invalid) { this.error.set('Completá el nombre y el área de la skill.'); return; } const datos = this.skillForm.getRawValue(); this.recursosService.registrarSkill({ nombre: datos.nombre ?? '', idAreaSkill: datos.idAreaSkill }).subscribe({ next: () => { this.skillForm.reset(); this.recursosService.consultarSkills().subscribe(skills => this.skills.set(skills)); }, error: (respuesta: unknown) => this.error.set(this.obtenerMensajeError(respuesta, 'No se pudo registrar la skill.')) }); }
  cambiarEstadoSkill(skill: Skill): void { this.recursosService.cambiarEstadoSkill({ ...skill, activo: !skill.activo }).subscribe({ next: () => this.recursosService.consultarSkills().subscribe(skills => this.skills.set(skills)), error: () => this.error.set('No se pudo actualizar la skill.') }); }
  cambiarEstado(usuario: UsuarioGestion): void {
    if (usuario.activo && !window.confirm(`¿Confirmás la baja lógica de ${usuario.nombre} ${usuario.apellido}? Si tiene tareas activas, primero deberás reasignarlas.`)) { return; }
    this.usuariosService.cambiarEstado({ ...usuario, activo: !usuario.activo }).subscribe({ next: () => this.cargar(), error: (respuesta: unknown) => this.error.set(this.obtenerMensajeError(respuesta, 'No fue posible actualizar el estado.')) });
  }
  usuariosFiltrados(): UsuarioGestion[] { const busqueda = this.busquedaSimple().trim().toLocaleLowerCase(); const nombre = this.filtroNombre().trim().toLocaleLowerCase(); const email = this.filtroEmail().trim().toLocaleLowerCase(); const rol = this.filtroRol(); const estado = this.filtroEstado(); const seniority = this.filtroSeniority(); return this.usuarios().filter((usuario: UsuarioGestion) => { const nombreCompleto = `${usuario.nombre} ${usuario.apellido}`.toLocaleLowerCase(); const coincideBusqueda = !busqueda || nombreCompleto.includes(busqueda) || usuario.email.toLocaleLowerCase().includes(busqueda); const coincideNombre = !nombre || nombreCompleto.includes(nombre); const coincideEmail = !email || usuario.email.toLocaleLowerCase().includes(email); const coincideRol = !rol || usuario.roles.some((rolUsuario: Rol) => rolUsuario.id.toString() === rol); const coincideEstado = !estado || (estado === 'activo' && usuario.activo) || (estado === 'inactivo' && !usuario.activo); const coincideSeniority = !seniority || usuario.empleado?.seniority === seniority; return coincideBusqueda && coincideNombre && coincideEmail && coincideRol && coincideEstado && coincideSeniority; }); }
  tieneTipoUsuario(tipoUsuario: string): boolean { return this.rolesSeleccionados().some(id => this.roles().find(rol => rol.id === id)?.tipoUsuario === tipoUsuario); }
  rolesTexto(usuario: UsuarioGestion): string { return usuario.roles.map(rol => rol.nombre).join(', '); }
  private guardarFichaSiCorresponde(idUsuario: number, mensajeExito: string): void { if (!this.tieneTipoUsuario('Empleado')) { this.finalizarGuardado(mensajeExito); return; } const ficha: FichaEmpleado = this.crearFichaEmpleado(); ficha.id = idUsuario; this.recursosService.guardarFichaEmpleado(ficha).subscribe({ next: () => this.finalizarGuardado(mensajeExito), error: (respuesta: unknown) => this.error.set(this.obtenerMensajeError(respuesta, 'El usuario fue guardado, pero no se pudo completar su ficha laboral.')) }); }
  private finalizarGuardado(mensajeExito: string): void { this.mensaje.set(mensajeExito); this.cancelarEdicion(); this.cargar(); }
  private crearPerfilEmpleado(): PerfilEmpleado { const datos = this.form.getRawValue(); return { costoHora: Number(datos.costoHora ?? 0), horasDisponiblesSemanales: Number(datos.horasDisponiblesSemanales ?? 40), seniority: datos.seniority ?? 'Junior', estadoLaboral: datos.estadoLaboral ?? 'Activo' }; }
  private crearFichaEmpleado(): FichaEmpleado { const datos = this.form.getRawValue(); return { empleadoSkills: this.skillsSeleccionadas().map(idSkill => ({ idSkill, nivel: 'Intermedio', activo: true })), disponibilidadBase: { horaInicio: datos.horaInicio ?? '09:00', horaFin: datos.horaFin ?? '18:00', horasSemanales: Number(datos.horasDisponiblesSemanales ?? 40), observacion: datos.observacionDisponibilidad ?? '', activo: true } }; }
  private abrirImpresionLegajo(ventana: Window, usuario: UsuarioGestion, ficha: FichaEmpleado): void {
    const disponibilidad = ficha.disponibilidadBase; const skills = ficha.empleadoSkills.map(skill => `${this.nombreSkill(skill.idSkill)}${skill.nivel ? ` (${skill.nivel})` : ''}`).join(', ') || 'Sin skills registradas';
    const costo = this.esDueno() ? `<p><strong>Costo por hora:</strong> ${usuario.empleado?.costoHora ?? 0}</p>` : '';
    const textoSeguro = (texto: string) => texto.replace(/[&<>"']/g, caracter => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[caracter] ?? caracter);
    ventana.document.write(`<!doctype html><html lang="es"><head><meta charset="utf-8"><title>Legajo - ${textoSeguro(usuario.nombre)} ${textoSeguro(usuario.apellido)}</title><style>body{font-family:Arial,sans-serif;color:#102a43;margin:42px;line-height:1.5}h1{color:#087e9d;margin-bottom:4px}.muted{color:#526b7a}section{margin-top:24px;padding:18px;border:1px solid #cbd5e1;border-radius:12px}p{margin:7px 0}@media print{body{margin:24px}}</style></head><body><h1>Legajo de empleado</h1><p class="muted">Generado el ${new Date().toLocaleString('es-AR')}</p><section><h2>${textoSeguro(usuario.nombre)} ${textoSeguro(usuario.apellido)}</h2><p><strong>Email:</strong> ${textoSeguro(usuario.email)}</p><p><strong>Roles:</strong> ${textoSeguro(this.rolesTexto(usuario))}</p><p><strong>Estado de cuenta:</strong> ${usuario.activo ? 'Activo' : 'Inactivo'}</p><p><strong>Seniority:</strong> ${textoSeguro(usuario.empleado?.seniority ?? 'Sin definir')}</p><p><strong>Estado laboral:</strong> ${textoSeguro(usuario.empleado?.estadoLaboral ?? 'Sin definir')}</p>${costo}</section><section><h2>Skills y disponibilidad</h2><p><strong>Skills:</strong> ${textoSeguro(skills)}</p><p><strong>Horario base:</strong> ${textoSeguro(disponibilidad?.horaInicio ?? 'Sin definir')} a ${textoSeguro(disponibilidad?.horaFin ?? 'Sin definir')}</p><p><strong>Disponibilidad semanal:</strong> ${disponibilidad?.horasSemanales ?? usuario.empleado?.horasDisponiblesSemanales ?? 0} h</p><p><strong>Observación:</strong> ${textoSeguro(disponibilidad?.observacion || 'Sin observaciones')}</p></section></body></html>`);
    ventana.document.close(); ventana.focus(); ventana.print();
  }
  private obtenerMensajeError(respuesta: unknown, mensajePredeterminado: string): string { const errorHttp = respuesta as { error?: unknown }; if (typeof errorHttp.error === 'string' && errorHttp.error.trim()) { return errorHttp.error; } if (typeof errorHttp.error === 'object' && errorHttp.error !== null) { const errorApi = errorHttp.error as { message?: unknown; title?: unknown; errors?: Record<string, string[]> }; if (errorApi.errors) { const errores = Object.values(errorApi.errors).flat(); if (errores.length > 0) { return errores.join(' '); } } if (typeof errorApi.message === 'string') { return errorApi.message; } if (typeof errorApi.title === 'string') { return errorApi.title; } } return mensajePredeterminado; }
}

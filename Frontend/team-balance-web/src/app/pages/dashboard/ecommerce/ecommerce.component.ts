import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AuthService, UsuarioSesion } from '../../../services/auth.service';
import { Proyecto, ProjectsService } from '../../../services/projects.service';
import { LocalizationService } from '../../../services/localization.service';

@Component({ selector: 'app-ecommerce', standalone: true, imports: [CommonModule, RouterModule], templateUrl: './ecommerce.component.html' })
export class EcommerceComponent {
  readonly authService = inject(AuthService); private readonly projectsService = inject(ProjectsService);
  readonly localization = inject(LocalizationService);
  readonly proyectos = signal<Proyecto[]>([]); readonly proyectosAbiertos = signal(true); readonly cargandoProyectos = signal(false);
  readonly usuario: UsuarioSesion | null = this.authService.usuarioActual();
  readonly accesos = [
    { permiso: 'GestionarUsuarios', titulo: 'Gestionar empleados', descripcion: 'Usuarios, roles y perfiles laborales.', ruta: '/dashboard/empleados', accion: 'Abrir' },
    { permiso: 'GestionarTareas', titulo: 'Planificar tareas', descripcion: 'Priorizá y asigná el trabajo.', ruta: '/dashboard/tareas', accion: 'Planificar' },
    { permiso: 'UsarBestFit', titulo: 'Best Fit', descripcion: 'Compará alternativas de asignación.', ruta: '/dashboard/best-fit', accion: 'Analizar' },
    { permiso: 'RegistrarHoras', titulo: 'Registrar horas', descripcion: 'Actualizá tu dedicación semanal.', ruta: '/dashboard/registrar-horas', accion: 'Registrar' },
    { permiso: 'GestionarDisponibilidad', titulo: 'Disponibilidad', descripcion: 'Mantené visible tu capacidad.', ruta: '/dashboard/disponibilidad', accion: 'Actualizar' },
    { permiso: 'GestionarRoles', titulo: 'Gestionar roles', descripcion: 'Definí accesos de la agencia.', ruta: '/dashboard/roles', accion: 'Gestionar' },
    { permiso: 'ConsultarBitacora', titulo: 'Bitácora', descripcion: 'Actividad y controles de seguridad.', ruta: '/dashboard/bitacora', accion: 'Revisar' },
  ];
  constructor() { if (this.puedeVer('GestionarProyectos')) { this.cargarProyectos(); } }
  puedeVer(permiso: string): boolean { return this.authService.tienePermiso(permiso); }
  totalAccesos(): number { return this.accesos.filter(acceso => this.puedeVer(acceso.permiso)).length; }
  cargarProyectos(): void { this.cargandoProyectos.set(true); this.projectsService.consultar().subscribe({ next: (proyectos: Proyecto[]) => { this.proyectos.set(proyectos.filter(proyecto => proyecto.activo)); this.cargandoProyectos.set(false); }, error: () => this.cargandoProyectos.set(false) }); }
  proyectosEnCurso(): number { return this.proyectos().filter(proyecto => proyecto.estado === 'En curso').length; }
}

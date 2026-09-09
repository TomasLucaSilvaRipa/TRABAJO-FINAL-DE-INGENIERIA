import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-ecommerce',
  imports: [CommonModule, RouterModule],
  templateUrl: './ecommerce.component.html',
})
export class EcommerceComponent {
  readonly authService = inject(AuthService);

  readonly accesos = [
    { permiso: 'GestionarUsuarios', titulo: 'Gestionar empleados', descripcion: 'Creá usuarios, asigná roles y actualizá perfiles laborales.', ruta: '/dashboard/empleados', accion: 'Abrir gestión' },
    { permiso: 'GestionarProyectos', titulo: 'Gestionar proyectos', descripcion: 'Revisá el avance, responsables y prioridades de cada proyecto.', ruta: '/dashboard/proyectos', accion: 'Ver proyectos' },
    { permiso: 'GestionarTareas', titulo: 'Planificar tareas', descripcion: 'Organizá el trabajo, asigná responsables y seguí el estado.', ruta: '/dashboard/tareas', accion: 'Ir a tareas' },
    { permiso: 'UsarBestFit', titulo: 'Sugerir mejor recurso', descripcion: 'Usá Best Fit dentro de la planificación para comparar alternativas.', ruta: '/dashboard/best-fit', accion: 'Abrir Best Fit' },
    { permiso: 'RegistrarHoras', titulo: 'Registrar horas', descripcion: 'Actualizá el tiempo dedicado a tus tareas de esta semana.', ruta: '/dashboard/registrar-horas', accion: 'Registrar horas' },
    { permiso: 'GestionarDisponibilidad', titulo: 'Actualizar disponibilidad', descripcion: 'Mantené visible tu capacidad para una mejor planificación.', ruta: '/dashboard/disponibilidad', accion: 'Ver disponibilidad' },
    { permiso: 'GestionarRoles', titulo: 'Gestionar roles', descripcion: 'Definí roles y asignales los permisos estáticos de la plataforma.', ruta: '/dashboard/roles', accion: 'Gestionar roles' },
    { permiso: 'ConsultarBitacora', titulo: 'Revisar bitácora', descripcion: 'Consultá los eventos de seguridad y actividad del sistema.', ruta: '/dashboard/bitacora', accion: 'Ver actividad' },
  ];

  puedeVer(permiso: string): boolean {
    return this.authService.tienePermiso(permiso);
  }
}

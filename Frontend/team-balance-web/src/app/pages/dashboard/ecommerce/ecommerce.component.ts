import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AuthService, UsuarioSesion } from '../../../services/auth.service';
import { Proyecto, ProjectsService } from '../../../services/projects.service';
import { LocalizationService } from '../../../services/localization.service';
import { AnalisisCoberturaSkill, SkillsDesiertosService } from '../../../services/skills-desiertos.service';
import { AgencySetupComponent } from '../../../shared/components/agency-setup/agency-setup.component';

interface AccesoDashboard { permiso: string; titulo?: string; tituloClave?: string; descripcion?: string; descripcionClave?: string; ruta: string; accion?: string; accionClave?: string; ocultarDueno?: boolean; }

@Component({ selector: 'app-ecommerce', standalone: true, imports: [CommonModule, RouterModule, AgencySetupComponent], templateUrl: './ecommerce.component.html' })
export class EcommerceComponent {
  readonly authService = inject(AuthService); private readonly projectsService = inject(ProjectsService); private readonly skillsService = inject(SkillsDesiertosService);
  readonly localization = inject(LocalizationService);
  tipoBrechaTraducido(tipo: string): string {
    const claves: Record<string, string> = { 'Skill desierto': 'desert', 'Cobertura insuficiente': 'insufficient', 'Skill saturado': 'saturated' };
    return claves[tipo] ? this.localization.traducir(`private.dashboard.gap.${claves[tipo]}`) : tipo;
  }
  readonly proyectos = signal<Proyecto[]>([]); readonly proyectosAbiertos = signal(true); readonly cargandoProyectos = signal(false);
  readonly skillsCriticas = signal<AnalisisCoberturaSkill[]>([]); readonly cargandoSkills = signal(false);
  readonly usuario: UsuarioSesion | null = this.authService.usuarioActual();
  readonly accesos: AccesoDashboard[] = [
    { permiso: 'GestionarUsuarios', tituloClave: 'static.manageEmployees', descripcionClave: 'static.usersRolesProfiles', ruta: '/dashboard/empleados', accionClave: 'static.open' },
    { permiso: 'GestionarTareas', tituloClave: 'static.planTasks', descripcionClave: 'static.prioritizeWork', ruta: '/dashboard/tareas', accionClave: 'static.plan', ocultarDueno: true },
    { permiso: 'UsarBestFit', titulo: 'Best Fit', descripcionClave: 'private.dashboard.comparaAlternativasDeAsignacion', ruta: '/dashboard/best-fit', accionClave: 'private.dashboard.analizar' },
    { permiso: 'AnalizarSkills', tituloClave: 'private.dashboard.coverage', descripcionClave: 'private.dashboard.detectaBrechasOperativasYPrioridades', ruta: '/dashboard/skills-desiertos', accionClave: 'static.review' },
    { permiso: 'RegistrarHoras', tituloClave: 'private.dashboard.logHours', descripcionClave: 'private.dashboard.actualizaTuDedicacionSemanal', ruta: '/dashboard/registrar-horas', accionClave: 'private.dashboard.registrar' },
    { permiso: 'GestionarDisponibilidad', tituloClave: 'static.availability', descripcionClave: 'static.keepCapacityVisible', ruta: '/dashboard/disponibilidad', accionClave: 'static.update' },
    { permiso: 'GestionarRoles', tituloClave: 'static.manageRoles', descripcionClave: 'static.defineAgencyAccess', ruta: '/dashboard/roles', accionClave: 'static.manage' },
    { permiso: 'ConsultarBitacora', tituloClave: 'menu.logs', descripcionClave: 'private.dashboard.actividadYControlesDeSeguridad', ruta: '/dashboard/bitacora', accionClave: 'static.review' },
  ];
  constructor() { if (this.puedeVer('GestionarProyectos')) { this.cargarProyectos(); } if (this.puedeVer('AnalizarSkills')) { this.cargarSkillsCriticas(); } }
  puedeVer(permiso: string): boolean { return this.authService.tienePermiso(permiso); }
  puedeVerAcceso(acceso: AccesoDashboard): boolean { return this.puedeVer(acceso.permiso) && !(acceso.ocultarDueno && this.authService.esDueno()); }
  totalAccesos(): number { return this.accesos.filter(acceso => this.puedeVerAcceso(acceso)).length; }
  cargarProyectos(): void { this.cargandoProyectos.set(true); this.projectsService.consultar().subscribe({ next: (proyectos: Proyecto[]) => { this.proyectos.set(proyectos.filter(proyecto => proyecto.activo)); this.cargandoProyectos.set(false); }, error: () => this.cargandoProyectos.set(false) }); }
  proyectosEnCurso(): number { return this.proyectos().filter(proyecto => proyecto.estado === 'En curso').length; }
  cargarSkillsCriticas(): void { this.cargandoSkills.set(true); this.skillsService.resumen().subscribe({ next: (skills) => { this.skillsCriticas.set(skills); this.cargandoSkills.set(false); }, error: () => this.cargandoSkills.set(false) }); }
  porcentajeSkill(skill: AnalisisCoberturaSkill): number { return Math.min(100, Math.max(3, skill.porcentajeCobertura)); }
}

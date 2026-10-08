import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AuthService, UsuarioSesion } from '../../../services/auth.service';
import { Proyecto, ProjectsService } from '../../../services/projects.service';
import { LocalizationService } from '../../../services/localization.service';
import { AnalisisCoberturaSkill, SkillsDesiertosService } from '../../../services/skills-desiertos.service';

@Component({ selector: 'app-ecommerce', standalone: true, imports: [CommonModule, RouterModule], templateUrl: './ecommerce.component.html' })
export class EcommerceComponent {
  readonly authService = inject(AuthService); private readonly projectsService = inject(ProjectsService); private readonly skillsService = inject(SkillsDesiertosService);
  readonly localization = inject(LocalizationService);
  readonly proyectos = signal<Proyecto[]>([]); readonly proyectosAbiertos = signal(true); readonly cargandoProyectos = signal(false);
  readonly skillsCriticas = signal<AnalisisCoberturaSkill[]>([]); readonly cargandoSkills = signal(false);
  readonly usuario: UsuarioSesion | null = this.authService.usuarioActual();
  readonly accesos = [
    { permiso: 'GestionarUsuarios', titulo: 'Gestionar empleados', descripcion: 'Usuarios, roles y perfiles laborales.', ruta: '/dashboard/empleados', accion: 'Abrir' },
    { permiso: 'GestionarTareas', titulo: 'Planificar tareas', descripcion: 'Priorizá y asigná el trabajo.', ruta: '/dashboard/tareas', accion: 'Planificar' },
    { permiso: 'UsarBestFit', titulo: 'Best Fit', descripcion: 'Compará alternativas de asignación.', ruta: '/dashboard/best-fit', accion: 'Analizar' },
    { permiso: 'AnalizarSkills', titulo: 'Cobertura de skills', descripcion: 'Detectá brechas operativas y prioridades.', ruta: '/dashboard/skills-desiertos', accion: 'Revisar' },
    { permiso: 'RegistrarHoras', titulo: 'Registrar horas', descripcion: 'Actualizá tu dedicación semanal.', ruta: '/dashboard/registrar-horas', accion: 'Registrar' },
    { permiso: 'GestionarDisponibilidad', titulo: 'Disponibilidad', descripcion: 'Mantené visible tu capacidad.', ruta: '/dashboard/disponibilidad', accion: 'Actualizar' },
    { permiso: 'GestionarRoles', titulo: 'Gestionar roles', descripcion: 'Definí accesos de la agencia.', ruta: '/dashboard/roles', accion: 'Gestionar' },
    { permiso: 'ConsultarBitacora', titulo: 'Bitácora', descripcion: 'Actividad y controles de seguridad.', ruta: '/dashboard/bitacora', accion: 'Revisar' },
  ];
  constructor() { if (this.puedeVer('GestionarProyectos')) { this.cargarProyectos(); } if (this.puedeVer('AnalizarSkills')) { this.cargarSkillsCriticas(); } }
  puedeVer(permiso: string): boolean { return this.authService.tienePermiso(permiso); }
  totalAccesos(): number { return this.accesos.filter(acceso => this.puedeVer(acceso.permiso)).length; }
  cargarProyectos(): void { this.cargandoProyectos.set(true); this.projectsService.consultar().subscribe({ next: (proyectos: Proyecto[]) => { this.proyectos.set(proyectos.filter(proyecto => proyecto.activo)); this.cargandoProyectos.set(false); }, error: () => this.cargandoProyectos.set(false) }); }
  proyectosEnCurso(): number { return this.proyectos().filter(proyecto => proyecto.estado === 'En curso').length; }
  cargarSkillsCriticas(): void { this.cargandoSkills.set(true); this.skillsService.resumen().subscribe({ next: (skills) => { this.skillsCriticas.set(skills); this.cargandoSkills.set(false); }, error: () => this.cargandoSkills.set(false) }); }
  porcentajeSkill(skill: AnalisisCoberturaSkill): number { return Math.min(100, Math.max(3, skill.porcentajeCobertura)); }
}

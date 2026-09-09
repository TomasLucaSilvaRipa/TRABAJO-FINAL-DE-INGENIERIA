import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { LocalizationService } from '../../../services/localization.service';
import { AuthService } from '../../../services/auth.service';
import { SidebarService } from '../../services/sidebar.service';

interface NavItem { key: string; path: string; permission: string; }

@Component({
  selector: 'app-sidebar',
  imports: [CommonModule, RouterModule],
  templateUrl: './app-sidebar.component.html',
})
export class AppSidebarComponent {
  readonly sidebarService = inject(SidebarService);
  readonly localization = inject(LocalizationService);
  readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  readonly isExpanded$ = this.sidebarService.isExpanded$;
  readonly isMobileOpen$ = this.sidebarService.isMobileOpen$;
  readonly isHovered$ = this.sidebarService.isHovered$;

  readonly navItems: NavItem[] = [
    { key: 'menu.dashboard', path: '/dashboard', permission: 'VerDashboard' },
    { key: 'menu.projects', path: '/dashboard/proyectos', permission: 'GestionarProyectos' },
    { key: 'menu.tasks', path: '/dashboard/tareas', permission: 'GestionarTareas' },
    { key: 'menu.employees', path: '/dashboard/empleados', permission: 'GestionarUsuarios' },
    { key: 'menu.reports', path: '/dashboard/reportes', permission: 'ConsultarTableroEjecutivo' },
    { key: 'menu.agency', path: '/dashboard/configuracion-agencia', permission: 'GestionarAgencia' },
    { key: 'menu.subscription', path: '/dashboard/suscripcion', permission: 'GestionarSuscripcion' },
    { key: 'menu.teamCalendar', path: '/dashboard/calendario-equipo', permission: 'VerCalendarioEquipo' },
    { key: 'menu.simulation', path: '/dashboard/simulacion-impacto', permission: 'SimularImpacto' },
    { key: 'menu.myTasks', path: '/dashboard/mis-tareas', permission: 'VerDashboard' },
    { key: 'menu.kanban', path: '/dashboard/kanban', permission: 'VerKanban' },
    { key: 'menu.registerHours', path: '/dashboard/registrar-horas', permission: 'RegistrarHoras' },
    { key: 'menu.availability', path: '/dashboard/disponibilidad', permission: 'GestionarDisponibilidad' },
    { key: 'menu.workload', path: '/dashboard/carga-operativa', permission: 'VerCargaOperativa' },
    { key: 'menu.roles', path: '/dashboard/roles', permission: 'GestionarRoles' },
    { key: 'menu.plansManagement', path: '/dashboard/planes', permission: 'GestionarPlanes' },
    { key: 'menu.logs', path: '/dashboard/bitacora', permission: 'ConsultarBitacora' },
  ];

  isActive(path: string): boolean {
    return path === '/dashboard' ? this.router.url === path : this.router.url.startsWith(path);
  }

  puedeVer(item: NavItem): boolean {
    return this.authService.tienePermiso(item.permission);
  }

  closeMobile(): void {
    this.sidebarService.setMobileOpen(false);
  }

  onSidebarMouseEnter(): void {
    this.sidebarService.setHovered(true);
  }
}

import { Component, computed, ElementRef, inject, signal, ViewChild } from '@angular/core';
import { SidebarService } from '../../services/sidebar.service';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ThemeToggleButtonComponent } from '../../components/common/theme-toggle/theme-toggle-button.component';
import { NotificationDropdownComponent } from '../../components/header/notification-dropdown/notification-dropdown.component';
import { UserDropdownComponent } from '../../components/header/user-dropdown/user-dropdown.component';
import { LanguageSelector } from '../../components/language-selector/language-selector.component';
import { AuthService } from '../../../services/auth.service';
import { LocalizationService } from '../../../services/localization.service';

interface PaginaBuscable {
  ruta: string;
  permiso: string;
  titulo: string;
  descripcion: string;
  palabras: string;
  soloSoporte?: boolean;
  ocultarSoporte?: boolean;
}

@Component({
  selector: 'app-header',
  imports: [
    CommonModule,
    RouterModule,
    ThemeToggleButtonComponent,
    NotificationDropdownComponent,
    UserDropdownComponent,
    LanguageSelector,
  ],
  templateUrl: './app-header.component.html',
})
export class AppHeaderComponent {
  readonly sidebarService = inject(SidebarService);
  readonly localization = inject(LocalizationService);
  private readonly authService = inject(AuthService);
  isApplicationMenuOpen = false;
  readonly isMobileOpen$;
  protected readonly busqueda = signal('');
  private readonly paginasBuscables: PaginaBuscable[] = [
    { ruta: '/dashboard', permiso: 'VerDashboard', titulo: 'menu.dashboard', descripcion: 'dashboardSearch.dashboard.description', palabras: 'inicio resumen herramientas panel dashboard' },
    { ruta: '/dashboard/profile', permiso: 'Perfil', titulo: 'dashboardSearch.profile.title', descripcion: 'dashboardSearch.profile.description', palabras: 'perfil datos personales preferencias cuenta' },
    { ruta: '/dashboard/seguridad', permiso: 'SeguridadCuenta', titulo: 'dashboardSearch.security.title', descripcion: 'dashboardSearch.security.description', palabras: 'seguridad contraseña password clave acceso' },
    { ruta: '/dashboard/planes', permiso: 'GestionarPlanes', titulo: 'menu.plansManagement', descripcion: 'dashboardSearch.plans.description', palabras: 'planes precios modalidades suscripcion' },
    { ruta: '/dashboard/bitacora', permiso: 'ConsultarBitacora', titulo: 'menu.logs', descripcion: 'dashboardSearch.logs.description', palabras: 'bitacora log auditoria eventos seguridad' },
    { ruta: '/dashboard/empleados', permiso: 'GestionarUsuarios', titulo: 'menu.employees', descripcion: 'dashboardSearch.employees.description', palabras: 'empleados usuarios colaboradores equipo' },
    { ruta: '/dashboard/proyectos', permiso: 'GestionarProyectos', titulo: 'menu.projects', descripcion: 'dashboardSearch.projects.description', palabras: 'proyectos seguimiento planificacion' },
    { ruta: '/dashboard/reportes', permiso: 'ConsultarTableroEjecutivo', titulo: 'menu.reports', descripcion: 'dashboardSearch.reports.description', palabras: 'reportes metricas indicadores tablero ejecutivo' },
    { ruta: '/dashboard/configuracion-agencia', permiso: 'GestionarAgencia', titulo: 'menu.agency', descripcion: 'dashboardSearch.agency.description', palabras: 'agencia configuracion datos comerciales facturacion' },
    { ruta: '/dashboard/suscripcion', permiso: 'GestionarSuscripcion', titulo: 'menu.subscription', descripcion: 'dashboardSearch.subscription.description', palabras: 'suscripcion renovacion plan pago vencimiento' },
    { ruta: '/dashboard/tareas', permiso: 'GestionarTareas', titulo: 'menu.tasks', descripcion: 'dashboardSearch.tasks.description', palabras: 'tareas asignar planificar prioridades' },
    { ruta: '/dashboard/plantillas-tareas', permiso: 'GestionarPlantillasTareas', titulo: 'menu.templates', descripcion: 'dashboardSearch.templates.description', palabras: 'plantillas tareas reutilizar' },
    { ruta: '/dashboard/best-fit', permiso: 'UsarBestFit', titulo: 'menu.bestFit', descripcion: 'dashboardSearch.bestFit.description', palabras: 'best fit sugerencia asignacion skills disponibilidad' },
    { ruta: '/dashboard/calendario-equipo', permiso: 'VerCalendarioEquipo', titulo: 'menu.teamCalendar', descripcion: 'dashboardSearch.teamCalendar.description', palabras: 'calendario equipo planificacion disponibilidad' },
    { ruta: '/dashboard/simulacion-impacto', permiso: 'SimularImpacto', titulo: 'menu.simulation', descripcion: 'dashboardSearch.simulation.description', palabras: 'simulacion impacto planificacion cambios' },
    { ruta: '/dashboard/skills-desiertos', permiso: 'AnalizarSkills', titulo: 'menu.skillsCoverage', descripcion: 'dashboardSearch.skills.description', palabras: 'skills habilidades cobertura brechas' },
    { ruta: '/dashboard/mis-tareas', permiso: 'VerMisTareas', titulo: 'menu.myTasks', descripcion: 'dashboardSearch.myTasks.description', palabras: 'mis tareas pendientes asignadas' },
    { ruta: '/dashboard/kanban', permiso: 'VerKanban', titulo: 'menu.kanban', descripcion: 'dashboardSearch.kanban.description', palabras: 'kanban tablero tareas avance estado' },
    { ruta: '/dashboard/kanban-proyectos', permiso: 'GestionarTareas', titulo: 'menu.kanban', descripcion: 'dashboardSearch.kanban.description', palabras: 'kanban proyectos tablero tareas avance estado' },
    { ruta: '/dashboard/registrar-horas', permiso: 'RegistrarHoras', titulo: 'menu.registerHours', descripcion: 'dashboardSearch.hours.description', palabras: 'registrar horas dedicacion tiempo' },
    { ruta: '/dashboard/disponibilidad', permiso: 'GestionarDisponibilidad', titulo: 'menu.availability', descripcion: 'dashboardSearch.availability.description', palabras: 'disponibilidad horario capacidad' },
    { ruta: '/dashboard/licencias', permiso: 'GestionarDisponibilidad', titulo: 'dashboardSearch.licenses.title', descripcion: 'dashboardSearch.licenses.description', palabras: 'licencias ausencias vacaciones' },
    { ruta: '/dashboard/carga-operativa', permiso: 'VerCargaOperativa', titulo: 'menu.workload', descripcion: 'dashboardSearch.workload.description', palabras: 'carga operativa capacidad dedicacion' },
    { ruta: '/dashboard/roles', permiso: 'GestionarRoles', titulo: 'menu.roles', descripcion: 'dashboardSearch.roles.description', palabras: 'roles permisos accesos usuarios' },
    { ruta: '/dashboard/notificaciones', permiso: 'ConsultarNotificaciones', titulo: 'dashboardSearch.notifications.title', descripcion: 'dashboardSearch.notifications.description', palabras: 'notificaciones avisos alertas' },
    { ruta: '/dashboard/ayuda', permiso: 'ConsultarAyuda', titulo: 'menu.help', descripcion: 'dashboardSearch.help.description', palabras: 'ayuda faq preguntas frecuentes soporte' },
    { ruta: '/dashboard/soporte', permiso: 'GestionarSoporte', titulo: 'menu.support', descripcion: 'dashboardSearch.support.description', palabras: 'soporte consultas helpdesk tickets', ocultarSoporte: true },
    { ruta: '/dashboard/newsletter', permiso: 'GestionarNewsletter', titulo: 'menu.newsletter', descripcion: 'dashboardSearch.newsletter.description', palabras: 'newsletter novedades email comunicaciones', ocultarSoporte: true },
    { ruta: '/dashboard/soporte/bandeja', permiso: 'GestionarBandejaSoporte', titulo: 'menu.supportInbox', descripcion: 'dashboardSearch.inbox.description', palabras: 'bandeja soporte consultas tickets respuestas', soloSoporte: true },
    { ruta: '/dashboard/gestion-novedades', permiso: 'GestionarNovedades', titulo: 'menu.newsManagement', descripcion: 'dashboardSearch.news.description', palabras: 'novedades noticias publicaciones comunicaciones', soloSoporte: true },
    { ruta: '/dashboard/gestion-faqs', permiso: 'GestionarFaqs', titulo: 'menu.faqManagement', descripcion: 'dashboardSearch.faq.description', palabras: 'faq preguntas frecuentes ayuda respuestas', soloSoporte: true },
    { ruta: '/dashboard/gestion-encuestas', permiso: 'GestionarEncuestas', titulo: 'menu.surveys', descripcion: 'dashboardSearch.surveys.description', palabras: 'encuestas opiniones producto captacion resultados graficos', soloSoporte: true },
    { ruta: '/dashboard/operadores', permiso: 'GestionarOperadores', titulo: 'menu.operators', descripcion: 'dashboardSearch.operators.description', palabras: 'operadores soporte backoffice usuarios internos', soloSoporte: true },
    { ruta: '/dashboard/respaldos', permiso: 'GestionarRespaldos', titulo: 'menu.backups', descripcion: 'dashboardSearch.backups.description', palabras: 'backups respaldos restore restauracion base de datos', soloSoporte: true },
  ];
  protected readonly resultadosBusqueda = computed(() => {
    this.localization.language();
    const termino = this.normalizar(this.busqueda());
    if (!termino) { return []; }
    return this.paginasBuscables.filter((pagina) => this.puedeVerPagina(pagina) && this.normalizar(`${this.localization.traducir(pagina.titulo)} ${this.localization.traducir(pagina.descripcion)} ${pagina.palabras}`).includes(termino)).slice(0, 7);
  });

  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;

  constructor() {
    this.isMobileOpen$ = this.sidebarService.isMobileOpen$;
  }

  handleToggle() {
    if (window.innerWidth >= 1280) {
      this.sidebarService.toggleExpanded();
    } else {
      this.sidebarService.toggleMobileOpen();
    }
  }

  toggleApplicationMenu(): void {
    this.isApplicationMenuOpen = !this.isApplicationMenuOpen;
  }

  ngAfterViewInit(): void {
    document.addEventListener('keydown', this.handleKeyDown);
  }

  ngOnDestroy(): void {
    document.removeEventListener('keydown', this.handleKeyDown);
  }

  handleKeyDown = (event: KeyboardEvent) => {
    if ((event.metaKey || event.ctrlKey) && event.key === 'k') {
      event.preventDefault();
      this.searchInput?.nativeElement.focus();
    }
  };

  protected actualizarBusqueda(valor: string): void {
    this.busqueda.set(valor);
  }

  protected cerrarBusqueda(): void {
    this.busqueda.set('');
  }

  protected etiquetaBusqueda(clave: string): string {
    return this.localization.traducir(clave);
  }

  private puedeVerPagina(pagina: PaginaBuscable): boolean {
    if (pagina.soloSoporte && !this.authService.esSoporte()) { return false; }
    if (pagina.ocultarSoporte && this.authService.esSoporte()) { return false; }
    return this.authService.tienePermiso(pagina.permiso);
  }

  private normalizar(texto: string): string {
    return texto.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase().trim();
  }
}

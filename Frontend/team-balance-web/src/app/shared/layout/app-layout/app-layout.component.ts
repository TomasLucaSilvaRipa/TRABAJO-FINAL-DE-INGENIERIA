import { Component, effect, inject, untracked } from '@angular/core';
import { HttpBackend, HttpClient } from '@angular/common/http';
import { SidebarService } from '../../services/sidebar.service';
import { CommonModule } from '@angular/common';
import { AppSidebarComponent } from '../app-sidebar/app-sidebar.component';
import { BackdropComponent } from '../backdrop/backdrop.component';
import { Router, RouterModule } from '@angular/router';
import { AppHeaderComponent } from '../app-header/app-header.component';
import { AuthService } from '../../../services/auth.service';
import { FocusTimerService, SEGUNDOS_MAXIMOS_FOCO } from '../../../services/focus-timer.service';
import { LocalizationService } from '../../../services/localization.service';
import { LocalizedStaticContentDirective } from '../../directives/localized-static-content.directive';
import { ThemeToggleButtonComponent } from '../../components/common/theme-toggle/theme-toggle-button.component';

@Component({
  selector: 'app-layout',
  imports: [
    CommonModule,
    RouterModule,
    AppHeaderComponent,
    AppSidebarComponent,
    BackdropComponent,
    LocalizedStaticContentDirective,
    ThemeToggleButtonComponent
  ],
  templateUrl: './app-layout.component.html',
})

export class AppLayoutComponent {
  private readonly router = inject(Router);
  private readonly accessObservationHttp = new HttpClient(inject(HttpBackend));
  readonly foco = inject(FocusTimerService);
  readonly localization = inject(LocalizationService);
  readonly isExpanded$;
  readonly isHovered$;
  readonly isMobileOpen$;

  constructor(public sidebarService: SidebarService, readonly authService: AuthService) {
    this.isExpanded$ = this.sidebarService.isExpanded$;
    this.isHovered$ = this.sidebarService.isHovered$;
    this.isMobileOpen$ = this.sidebarService.isMobileOpen$;
    effect(() => {
      if (this.foco.activo() && this.foco.segundos() >= SEGUNDOS_MAXIMOS_FOCO) {
        untracked(() => this.detenerFoco());
      }
    });
  }

  cerrarSesion(): void {
    this.authService.cerrarSesion().subscribe({
      next: () => this.finalizarSesion(),
      error: () => this.finalizarSesion(),
    });
  }

  private finalizarSesion(): void {
    this.authService.logOut();
    void this.router.navigate(['/']);
  }

  ngOnInit() {
    if (!this.authService.suscripcionRestringida())
    {
      this.authService.actualizarAutorizacionSesion();
      this.observarNavegador();
    }
  }

  private observarNavegador(): void {
    try {
      const token = this.authService.obtenerToken();
      if (!token || !globalThis.crypto?.randomUUID) return;

      const key = 'teamBalance.accessObservation.browser';
      const navegador = localStorage.getItem(key) ?? crypto.randomUUID();
      localStorage.setItem(key, navegador);
      this.accessObservationHttp.post('/api/seguridad/observacion-acceso', null, {
        headers: { Authorization: `Bearer ${token}`, 'X-TeamBalance-Browser': navegador },
      }).subscribe({ error: () => {} });
    } catch {
      // Browser storage can be disabled. Navigation still works.
    }
  }

  get containerClasses() {
    return [
      'flex-1',
      'transition-all',
      'duration-300',
      'ease-in-out',
      (this.isExpanded$ || this.isHovered$) ? 'xl:ml-[290px]' : 'xl:ml-[90px]',
      this.isMobileOpen$ ? 'ml-0' : ''
    ];
  }

  detenerFoco(): void {
    const sesion = this.foco.detener();
    if (!sesion.tarea) {
      return;
    }

    const queryParams: { tarea: number; descripcion: string; horas?: number; aviso?: string } = { tarea: sesion.tarea.id, descripcion: `Sesión de foco: ${sesion.tarea.titulo}` };
    if (sesion.segundos >= 15 * 60)
    {
      queryParams.horas = Math.ceil((sesion.segundos / 3600) * 4) / 4;
      if (sesion.segundos === SEGUNDOS_MAXIMOS_FOCO) {
        queryParams.aviso = 'focusLimitReached';
      }
    }
    else
    {
      queryParams.aviso = 'La sesión de foco fue menor a 15 minutos; no se precargaron horas porque la carga mínima es de 0,25 h.';
    }
    this.router.navigate(['/dashboard/registrar-horas'], { queryParams });
  }

}

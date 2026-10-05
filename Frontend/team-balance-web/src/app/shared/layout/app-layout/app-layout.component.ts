import { Component, inject } from '@angular/core';
import { SidebarService } from '../../services/sidebar.service';
import { CommonModule } from '@angular/common';
import { AppSidebarComponent } from '../app-sidebar/app-sidebar.component';
import { BackdropComponent } from '../backdrop/backdrop.component';
import { Router, RouterModule } from '@angular/router';
import { AppHeaderComponent } from '../app-header/app-header.component';
import { AuthService } from '../../../services/auth.service';
import { FocusTimerService } from '../../../services/focus-timer.service';

@Component({
  selector: 'app-layout',
  imports: [
    CommonModule,
    RouterModule,
    AppHeaderComponent,
    AppSidebarComponent,
    BackdropComponent
  ],
  templateUrl: './app-layout.component.html',
})

export class AppLayoutComponent {
  private readonly router = inject(Router);
  readonly foco = inject(FocusTimerService);
  readonly isExpanded$;
  readonly isHovered$;
  readonly isMobileOpen$;

  constructor(public sidebarService: SidebarService, private readonly authService: AuthService) {
    this.isExpanded$ = this.sidebarService.isExpanded$;
    this.isHovered$ = this.sidebarService.isHovered$;
    this.isMobileOpen$ = this.sidebarService.isMobileOpen$;
  }

  ngOnInit() {
    this.authService.actualizarAutorizacionSesion();
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
    }
    else
    {
      queryParams.aviso = 'La sesión de foco fue menor a 15 minutos; no se precargaron horas porque la carga mínima es de 0,25 h.';
    }
    this.router.navigate(['/dashboard/registrar-horas'], { queryParams });
  }

}

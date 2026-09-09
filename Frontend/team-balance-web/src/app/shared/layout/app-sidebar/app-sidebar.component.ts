import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { LocalizationService } from '../../../services/localization.service';
import { SidebarService } from '../../services/sidebar.service';

interface NavItem { key: string; path: string; }

@Component({
  selector: 'app-sidebar',
  imports: [CommonModule, RouterModule],
  templateUrl: './app-sidebar.component.html',
})
export class AppSidebarComponent {
  readonly sidebarService = inject(SidebarService);
  readonly localization = inject(LocalizationService);
  private readonly router = inject(Router);
  readonly isExpanded$ = this.sidebarService.isExpanded$;
  readonly isMobileOpen$ = this.sidebarService.isMobileOpen$;
  readonly isHovered$ = this.sidebarService.isHovered$;

  readonly navItems: NavItem[] = [
    { key: 'menu.dashboard', path: '/dashboard' },
    { key: 'menu.calendar', path: '/dashboard/calendar' },
    { key: 'menu.logs', path: '/dashboard/bitacora' },
    { key: 'menu.profile', path: '/dashboard/profile' },
    { key: 'menu.security', path: '/dashboard/seguridad' },
  ];

  isActive(path: string): boolean {
    return path === '/dashboard' ? this.router.url === path : this.router.url.startsWith(path);
  }

  closeMobile(): void {
    this.sidebarService.setMobileOpen(false);
  }

  onSidebarMouseEnter(): void {
    this.sidebarService.setHovered(true);
  }
}

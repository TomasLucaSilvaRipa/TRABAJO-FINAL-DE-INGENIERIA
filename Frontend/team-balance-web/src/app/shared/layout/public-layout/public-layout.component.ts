import { Component, effect, inject, signal, ViewEncapsulation } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { filter } from 'rxjs';
import { PublicHeader } from '../public-layout/public-header/public-header.component';
import { PublicFooterComponent } from '../public-layout/public-footer/public-footer.component';
import { LocalizedStaticContentDirective } from '../../directives/localized-static-content.directive';
import { LocalizationService } from '../../../services/localization.service';

@Component({
  selector: 'app-public-layout',
  imports: [
    RouterOutlet,
    PublicHeader,
    PublicFooterComponent,
    LocalizedStaticContentDirective
  ],
  templateUrl: './public-layout.component.html',
  styleUrl: './public-layout.component.css',
  encapsulation: ViewEncapsulation.None,
})
export class PublicLayoutComponent {
  publicheader = new PublicHeader();
  protected readonly localization = inject(LocalizationService);
  protected readonly contrastMode = signal(false);
  private readonly router = inject(Router);
  private readonly documentTitle = inject(Title);
  private readonly currentRoute = signal(this.routePath(this.router.url));
  private readonly titleKeys: Record<string, string> = {
    '/': 'public.title.home',
    '/novedades': 'public.title.news',
    '/privacy-policy': 'public.title.privacy',
    '/security-policy': 'public.title.security',
    '/terms-and-conditions': 'public.title.terms',
  };

  constructor() {
    this.router.events.pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd)).subscribe((event) => this.currentRoute.set(this.routePath(event.urlAfterRedirects)));
    effect(() => {
      const key = this.titleKeys[this.currentRoute()];
      if (key) this.documentTitle.setTitle(this.localization.traducir(key));
    });
  }

  protected toggleContrast(): void {
    this.contrastMode.update((enabled) => !enabled);
  }

  private routePath(url: string): string { return url.split('?')[0].split('#')[0]; }
}

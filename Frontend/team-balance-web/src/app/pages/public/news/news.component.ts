import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NovedadesService, Noticia } from '../../../services/novedades.service';
import { LocalizationService } from '../../../services/localization.service';

@Component({ selector: 'app-news', imports: [CommonModule], templateUrl: './news.component.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class NewsComponent {
  private readonly service = inject(NovedadesService); readonly localization = inject(LocalizationService); readonly noticias = signal<Noticia[]>([]); readonly cargando = signal(true);
  constructor() { this.service.publicas().subscribe({ next: n => { this.noticias.set(n); this.cargando.set(false); }, error: () => this.cargando.set(false) }); }

  readonly portadasNoDisponibles = signal<ReadonlySet<string>>(new Set());

  marcarPortadaNoDisponible(url: string): void {
    this.portadasNoDisponibles.update(portadas => new Set([...portadas, url]));
  }

  categoryLabel(category: string): string {
    const normalized = category.trim().toLocaleLowerCase('es').normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    const keys: Record<string, string> = {
      mantenimiento: 'public.news.category.maintenance',
      producto: 'public.news.category.product',
      novedades: 'public.news.category.news',
    };
    return keys[normalized] ? this.localization.traducir(keys[normalized]) : category;
  }
}

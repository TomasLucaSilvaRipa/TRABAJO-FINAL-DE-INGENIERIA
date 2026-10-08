import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NovedadesService, Noticia } from '../../../services/novedades.service';

@Component({ selector: 'app-news', imports: [CommonModule], templateUrl: './news.component.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class NewsComponent {
  private readonly service = inject(NovedadesService); readonly noticias = signal<Noticia[]>([]); readonly cargando = signal(true);
  constructor() { this.service.publicas().subscribe({ next: n => { this.noticias.set(n); this.cargando.set(false); }, error: () => this.cargando.set(false) }); }
}

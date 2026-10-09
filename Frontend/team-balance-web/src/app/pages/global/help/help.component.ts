import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CategoriaPreguntaFrecuente, FaqsService, PreguntaFrecuente } from '../../../services/faqs.service';
import { LocalizationService } from '../../../services/localization.service';

@Component({ selector: 'app-help', imports: [RouterLink], templateUrl: './help.component.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class HelpComponent {
  readonly localization = inject(LocalizationService);
  private readonly faqsService = inject(FaqsService);
  readonly preguntas = signal<PreguntaFrecuente[]>([]);
  readonly categoriaActiva = signal<CategoriaPreguntaFrecuente | 'all'>('all');
  readonly busqueda = signal('');
  readonly abierta = signal<number | null>(null);
  readonly categorias = computed<(CategoriaPreguntaFrecuente | 'all')[]>(() => ['all', ...new Set(this.preguntas().map(pregunta => pregunta.categoria))]);
  readonly preguntasFiltradas = computed(() => {
    const termino = this.busqueda().trim().toLocaleLowerCase(this.localization.language() === 'es' ? 'es-AR' : 'en-US');
    return this.preguntas().filter(pregunta => (this.categoriaActiva() === 'all' || pregunta.categoria === this.categoriaActiva()) && (!termino || `${this.pregunta(pregunta)} ${this.respuesta(pregunta)} ${this.etiquetaCategoria(pregunta.categoria)}`.toLocaleLowerCase().includes(termino)));
  });

  constructor() { this.faqsService.publicas().subscribe({ next: preguntas => this.preguntas.set(preguntas) }); }

  t(clave: string): string { return this.localization.traducir(clave); }
  etiquetaCategoria(categoria: CategoriaPreguntaFrecuente | 'all'): string {
    if (categoria === 'all') { return this.t('faq.category.all'); }
    if (categoria === CategoriaPreguntaFrecuente.PrimerosPasos) { return this.t('faqCategory.firstSteps'); }
    if (categoria === CategoriaPreguntaFrecuente.ProyectosYTareas) { return this.t('faqCategory.projectsTasks'); }
    if (categoria === CategoriaPreguntaFrecuente.EquipoYDisponibilidad) { return this.t('faqCategory.teamAvailability'); }
    if (categoria === CategoriaPreguntaFrecuente.CuentaYSuscripcion) { return this.t('faqCategory.accountSubscription'); }
    return this.t('faqCategory.general');
  }
  pregunta(item: PreguntaFrecuente): string { return this.localization.language() === 'es' ? item.preguntaEs : item.preguntaEn; }
  respuesta(item: PreguntaFrecuente): string { return this.localization.language() === 'es' ? item.respuestaEs : item.respuestaEn; }
  seleccionarCategoria(categoria: CategoriaPreguntaFrecuente | 'all'): void { this.categoriaActiva.set(categoria); }
  actualizarBusqueda(evento: Event): void { this.busqueda.set((evento.target as HTMLInputElement).value); }
  alternarPregunta(id: number): void { this.abierta.update(actual => actual === id ? null : id); }
}

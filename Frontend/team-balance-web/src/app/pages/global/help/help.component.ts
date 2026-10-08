import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LocalizationService } from '../../../services/localization.service';

interface FaqItem { id: string; category: 'firstSteps' | 'projects' | 'team' | 'account'; questionKey: string; answerKey: string; }

@Component({ selector: 'app-help', imports: [RouterLink], templateUrl: './help.component.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class HelpComponent {
  readonly localization = inject(LocalizationService);
  readonly categorias = ['all', 'firstSteps', 'projects', 'team', 'account'] as const;
  readonly categoriaActiva = signal<(typeof this.categorias)[number]>('all'); readonly busqueda = signal(''); readonly abierta = signal<string | null>('acceso');
  readonly preguntas: readonly FaqItem[] = [
    { id: 'acceso', category: 'firstSteps', questionKey: 'faq.access.question', answerKey: 'faq.access.answer' }, { id: 'proyecto', category: 'projects', questionKey: 'faq.project.question', answerKey: 'faq.project.answer' }, { id: 'kanban', category: 'projects', questionKey: 'faq.kanban.question', answerKey: 'faq.kanban.answer' }, { id: 'horas', category: 'projects', questionKey: 'faq.hours.question', answerKey: 'faq.hours.answer' }, { id: 'best-fit', category: 'projects', questionKey: 'faq.bestFit.question', answerKey: 'faq.bestFit.answer' }, { id: 'disponibilidad', category: 'team', questionKey: 'faq.availability.question', answerKey: 'faq.availability.answer' }, { id: 'skills', category: 'team', questionKey: 'faq.skills.question', answerKey: 'faq.skills.answer' }, { id: 'suscripcion', category: 'account', questionKey: 'faq.subscription.question', answerKey: 'faq.subscription.answer' }, { id: 'seguridad', category: 'account', questionKey: 'faq.security.question', answerKey: 'faq.security.answer' },
  ];
  t(key: string): string { return this.localization.traducir(key); }
  etiquetaCategoria(categoria: string): string { return this.t(`faq.category.${categoria}`); }
  readonly preguntasFiltradas = computed(() => { const termino = this.busqueda().trim().toLocaleLowerCase(this.localization.language() === 'es' ? 'es-AR' : 'en-US'); const categoria = this.categoriaActiva(); return this.preguntas.filter(pregunta => (categoria === 'all' || pregunta.category === categoria) && (!termino || `${this.t(pregunta.questionKey)} ${this.t(pregunta.answerKey)} ${this.etiquetaCategoria(pregunta.category)}`.toLocaleLowerCase().includes(termino))); });
  seleccionarCategoria(categoria: (typeof this.categorias)[number]): void { this.categoriaActiva.set(categoria); }
  actualizarBusqueda(evento: Event): void { this.busqueda.set((evento.target as HTMLInputElement).value); }
  alternarPregunta(id: string): void { this.abierta.update(actual => actual === id ? null : id); }
}

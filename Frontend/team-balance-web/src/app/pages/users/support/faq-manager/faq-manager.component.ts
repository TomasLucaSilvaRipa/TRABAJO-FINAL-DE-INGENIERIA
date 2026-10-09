import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CategoriaPreguntaFrecuente, FaqsService, PreguntaFrecuente } from '../../../../services/faqs.service';
import { LocalizationService } from '../../../../services/localization.service';

@Component({ selector: 'app-faq-manager', imports: [ReactiveFormsModule], templateUrl: './faq-manager.component.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class FaqManagerComponent {
  readonly localization = inject(LocalizationService);
  private readonly faqsService = inject(FaqsService);
  private readonly formBuilder = inject(FormBuilder);
  readonly preguntas = signal<PreguntaFrecuente[]>([]);
  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly mensaje = signal('');
  readonly error = signal('');
  readonly categorias = [
    { codigo: CategoriaPreguntaFrecuente.PrimerosPasos, clave: 'faqCategory.firstSteps' },
    { codigo: CategoriaPreguntaFrecuente.ProyectosYTareas, clave: 'faqCategory.projectsTasks' },
    { codigo: CategoriaPreguntaFrecuente.EquipoYDisponibilidad, clave: 'faqCategory.teamAvailability' },
    { codigo: CategoriaPreguntaFrecuente.CuentaYSuscripcion, clave: 'faqCategory.accountSubscription' },
    { codigo: CategoriaPreguntaFrecuente.General, clave: 'faqCategory.general' }
  ];
  readonly formulario = this.formBuilder.nonNullable.group({ id: 0, categoria: [CategoriaPreguntaFrecuente.PrimerosPasos, [Validators.required]], preguntaEs: ['', [Validators.required, Validators.maxLength(300)]], respuestaEs: ['', [Validators.required, Validators.maxLength(4000)]], preguntaEn: ['', [Validators.required, Validators.maxLength(300)]], respuestaEn: ['', [Validators.required, Validators.maxLength(4000)]], orden: 0, activo: true });

  constructor() { this.cargar(); }

  cargar(): void {
    this.cargando.set(true);
    this.faqsService.gestion().subscribe({ next: preguntas => { this.preguntas.set(preguntas); this.cargando.set(false); }, error: () => { this.error.set(this.t('faqManager.loadError')); this.cargando.set(false); } });
  }

  editar(pregunta: PreguntaFrecuente): void {
    this.formulario.setValue({ id: pregunta.id, categoria: pregunta.categoria, preguntaEs: pregunta.preguntaEs, respuestaEs: pregunta.respuestaEs, preguntaEn: pregunta.preguntaEn, respuestaEn: pregunta.respuestaEn, orden: pregunta.orden, activo: pregunta.activo });
    this.mensaje.set('');
    this.error.set('');
  }

  nueva(): void { this.formulario.reset({ id: 0, categoria: CategoriaPreguntaFrecuente.PrimerosPasos, preguntaEs: '', respuestaEs: '', preguntaEn: '', respuestaEn: '', orden: 0, activo: true }); this.mensaje.set(''); this.error.set(''); }

  guardar(): void {
    if (this.formulario.invalid) { this.formulario.markAllAsTouched(); return; }
    this.guardando.set(true);
    this.error.set('');
    this.faqsService.guardar(this.formulario.getRawValue()).subscribe({ next: () => { this.guardando.set(false); this.mensaje.set(this.t('faqManager.saved')); this.nueva(); this.cargar(); }, error: respuesta => { this.guardando.set(false); this.error.set(respuesta?.error ?? this.t('faqManager.saveError')); } });
  }

  baja(pregunta: PreguntaFrecuente): void {
    if (!confirm(this.t('faqManager.confirmDelete'))) { return; }
    this.faqsService.baja(pregunta).subscribe({ next: () => { this.mensaje.set(this.t('faqManager.deleted')); this.cargar(); }, error: () => this.error.set(this.t('faqManager.deleteError')) });
  }

  etiquetaCategoria(categoria: CategoriaPreguntaFrecuente): string {
    return this.t(this.categorias.find(item => item.codigo === categoria)?.clave ?? 'faqCategory.general');
  }

  t(clave: string): string { return this.localization.traducir(clave); }
}

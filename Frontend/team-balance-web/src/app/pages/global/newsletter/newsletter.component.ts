import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';
import { AuthService } from '../../../services/auth.service';
import { LocalizationService } from '../../../services/localization.service';
import { CategoriaNoticia, NovedadesService, Noticia } from '../../../services/novedades.service';

@Component({ selector: 'app-newsletter', imports: [CommonModule, ReactiveFormsModule], templateUrl: './newsletter.component.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class NewsletterComponent {
  private readonly service = inject(NovedadesService); private readonly fb = inject(FormBuilder);
  readonly auth = inject(AuthService); readonly localization = inject(LocalizationService);
  readonly categorias = signal<CategoriaNoticia[]>([]); readonly seleccionadas = signal<number[]>([]); readonly noticias = signal<Noticia[]>([]); readonly cargando = signal(true); readonly guardando = signal(false); readonly error = signal(''); readonly mensaje = signal('');
  readonly form = this.fb.group({ idCategoriaNoticia: [0, [Validators.required, Validators.min(1)]], titulo: ['', [Validators.required, Validators.maxLength(200)]], contenido: ['', [Validators.required, Validators.maxLength(8000)]], imagenUrl: ['', Validators.maxLength(1000)], fechaPublicacion: [''], fechaVencimiento: [''] });
  constructor() { this.recargar(); }
  t(key: string): string { return this.localization.traducir(key); }
  recargar(): void {
    this.cargando.set(true); this.error.set('');
    if (this.auth.esSoporte()) {
      forkJoin({ categorias: this.service.categorias(), gestion: this.service.gestion() }).pipe(finalize(() => this.cargando.set(false))).subscribe({ next: x => { this.categorias.set(x.categorias); this.noticias.set(x.gestion); this.seleccionadas.set([]); }, error: e => this.error.set(e?.error ?? this.t('news.loadError')) });
      return;
    }
    forkJoin({ categorias: this.service.categorias(), preferencias: this.service.preferencias() }).pipe(finalize(() => this.cargando.set(false))).subscribe({ next: x => { this.categorias.set(x.categorias); this.noticias.set([]); this.seleccionadas.set(x.preferencias); }, error: e => this.error.set(e?.error ?? this.t('news.loadError')) });
  }
  alternar(id: number): void { this.seleccionadas.update(actual => actual.includes(id) ? actual.filter(x => x !== id) : [...actual, id]); }
  guardarPreferencias(): void { this.guardando.set(true); this.service.guardarPreferencias(this.seleccionadas()).pipe(finalize(() => this.guardando.set(false))).subscribe({ next: () => this.mensaje.set(this.t('news.preferencesSaved')), error: e => this.error.set(e?.error ?? this.t('news.preferencesError')) }); }
  publicar(): void { this.form.markAllAsTouched(); if (this.form.invalid) return; const v = this.form.getRawValue(); this.guardando.set(true); this.service.guardar({ idCategoriaNoticia: v.idCategoriaNoticia!, titulo: v.titulo!, contenido: v.contenido!, imagenUrl: v.imagenUrl || null, fechaPublicacion: v.fechaPublicacion || null, fechaVencimiento: v.fechaVencimiento || null }).pipe(finalize(() => this.guardando.set(false))).subscribe({ next: () => { this.mensaje.set(this.t('news.published')); this.form.reset({ idCategoriaNoticia: 0, titulo: '', contenido: '', imagenUrl: '', fechaPublicacion: '', fechaVencimiento: '' }); this.recargar(); }, error: e => this.error.set(e?.error ?? this.t('news.publishError')) }); }
  baja(id: number): void { this.service.baja(id).subscribe({ next: () => { this.mensaje.set(this.t('news.deactivated')); this.recargar(); }, error: e => this.error.set(e?.error ?? this.t('news.deactivateError')) }); }
}

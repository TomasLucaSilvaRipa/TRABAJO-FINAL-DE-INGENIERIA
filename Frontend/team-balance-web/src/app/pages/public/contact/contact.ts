import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocalizationService } from '../../../services/localization.service';
import { ContactService } from '../../../services/contact.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-contact',
  imports: [ReactiveFormsModule],
  templateUrl: './contact.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContactComponent {
  readonly localization = inject(LocalizationService);
  private readonly formBuilder = inject(FormBuilder); private readonly contactService = inject(ContactService);
  readonly enviado = signal(false);
  readonly error = signal('');
  readonly enviando = signal(false);
  readonly form = this.formBuilder.group({ nombre: ['', Validators.required], organizacion: [''], email: ['', [Validators.required, Validators.email]], mensaje: ['', Validators.required] });
  enviar(): void {
    if (this.enviando()) return;
    this.error.set('');
    this.enviado.set(false);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set('contact.validationError');
      return;
    }
    const datos = this.form.getRawValue();
    this.enviando.set(true);
    this.contactService.enviar({ nombre: datos.nombre ?? '', organizacion: datos.organizacion ?? '', email: datos.email ?? '', mensaje: datos.mensaje ?? '' })
      .pipe(finalize(() => this.enviando.set(false)))
      .subscribe({
        next: () => {
          this.form.reset();
          this.enviado.set(true);
        },
        error: respuesta => this.error.set(respuesta.status === 400 ? 'contact.validationError' : 'contact.sendError'),
      });
  }
}

import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocalizationService } from '../../../services/localization.service';
import { ContactService } from '../../../services/contact.service';

@Component({
  selector: 'app-contact',
  imports: [ReactiveFormsModule],
  templateUrl: './contact.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContactComponent {
  readonly localization = inject(LocalizationService);
  private readonly formBuilder = inject(FormBuilder); private readonly contactService = inject(ContactService);
  readonly mensaje = signal(''); readonly error = signal(''); readonly enviando = signal(false);
  readonly form = this.formBuilder.group({ nombre: ['', Validators.required], organizacion: [''], email: ['', [Validators.required, Validators.email]], mensaje: ['', Validators.required] });
  enviar(): void { if (this.form.invalid) { this.form.markAllAsTouched(); this.error.set('Completá nombre, email y mensaje para enviarnos tu consulta.'); return; } this.error.set(''); this.mensaje.set(''); this.enviando.set(true); const datos = this.form.getRawValue(); this.contactService.enviar({ nombre: datos.nombre ?? '', organizacion: datos.organizacion ?? '', email: datos.email ?? '', mensaje: datos.mensaje ?? '' }).subscribe({ next: respuesta => { this.mensaje.set(respuesta.mensaje); this.form.reset(); this.enviando.set(false); }, error: respuesta => { this.error.set(respuesta.error || 'No se pudo enviar la consulta. Intentá nuevamente.'); this.enviando.set(false); } }); }
}

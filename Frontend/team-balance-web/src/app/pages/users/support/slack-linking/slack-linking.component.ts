import { LocalizationService } from '../../../../services/localization.service';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { UsuariosService, UsuarioGestion } from '../../../../services/usuarios.service';

@Component({
  selector: 'app-slack-linking',
  imports: [ReactiveFormsModule],
  templateUrl: './slack-linking.component.html',
  styleUrl: './slack-linking.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SlackLinkingComponent {
  readonly localization = inject(LocalizationService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly usuariosService = inject(UsuariosService);
  readonly usuarios = signal<UsuarioGestion[]>([]);
  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly error = signal('');
  readonly mensaje = signal('');
  readonly form = this.formBuilder.group({
    idUsuario: [null as number | null, Validators.required],
    slackUserId: ['', [Validators.required, Validators.pattern(/^[UW][A-Z0-9]{8,}$/)]],
  });

  constructor() {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.error.set('');
    this.usuariosService.consultarUsuariosSinVincularSlack().subscribe({
      next: (usuarios) => {
        this.usuarios.set(usuarios);
        this.cargando.set(false);
      },
      error: (respuesta: { error?: string }) => {
        this.error.set(this.mensajeError(respuesta, 'review.slack.loadError'));
        this.cargando.set(false);
      },
    });
  }

  vincular(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.guardando()) {
      return;
    }

    const valores = this.form.getRawValue();
    const idUsuario = valores.idUsuario;
    const slackUserId = valores.slackUserId?.trim().toUpperCase() ?? '';
    const usuario = this.usuarios().find(item => item.id === idUsuario);
    if (!usuario) return;

    const confirmacion = this.localization.traducir('review.slack.confirmLink')
      .replace('{account}', () => [usuario.nombre, usuario.apellido, usuario.email].join(' '))
      .replace('{memberId}', () => slackUserId);
    if (!window.confirm(confirmacion)) return;

    this.guardando.set(true);
    this.error.set('');
    this.mensaje.set('');
    this.usuariosService.vincularUsuarioSlack(usuario.id, slackUserId).subscribe({
      next: () => {
        this.guardando.set(false);
        this.mensaje.set('review.slack.linked');
        this.form.reset({ idUsuario: null, slackUserId: '' });
        this.cargar();
      },
      error: (respuesta: { error?: string }) => {
        this.guardando.set(false);
        this.error.set(this.mensajeError(respuesta, 'review.slack.linkError'));
      },
    });
  }

  private mensajeError(respuesta: { error?: string }, mensajePredeterminado: string): string {
    return typeof respuesta?.error === 'string' ? respuesta.error : mensajePredeterminado;
  }
}

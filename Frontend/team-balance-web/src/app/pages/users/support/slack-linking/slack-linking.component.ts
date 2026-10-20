import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { UsuariosService, UsuarioGestion } from '../../../../services/usuarios.service';

@Component({
  selector: 'app-slack-linking',
  imports: [ReactiveFormsModule],
  templateUrl: './slack-linking.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SlackLinkingComponent {
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
        this.error.set(this.mensajeError(respuesta, 'No pudimos cargar los usuarios disponibles.'));
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
    if (idUsuario === null) {
      return;
    }

    this.guardando.set(true);
    this.error.set('');
    this.mensaje.set('');
    this.usuariosService.vincularUsuarioSlack(idUsuario, slackUserId).subscribe({
      next: () => {
        this.guardando.set(false);
        this.mensaje.set('Usuario vinculado correctamente. Ya puede usar /balance desde su cuenta de Slack.');
        this.form.reset({ idUsuario: null, slackUserId: '' });
        this.cargar();
      },
      error: (respuesta: { error?: string }) => {
        this.guardando.set(false);
        this.error.set(this.mensajeError(respuesta, 'No pudimos vincular el usuario con Slack.'));
      },
    });
  }

  private mensajeError(respuesta: { error?: string }, mensajePredeterminado: string): string {
    return typeof respuesta?.error === 'string' ? respuesta.error : mensajePredeterminado;
  }
}

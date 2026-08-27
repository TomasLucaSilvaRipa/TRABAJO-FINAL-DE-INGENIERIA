import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthPageLayoutComponent } from '../../../shared/layout/auth-page-layout/auth-page-layout.component';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-verify-account',
  imports: [AuthPageLayoutComponent, RouterLink],
  templateUrl: './verify-account.component.html',
  styleUrl: '../../../shared/components/auth/auth-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VerifyAccountComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);

  readonly validating = signal(true);
  readonly validada = signal(false);
  readonly message = signal('');

  constructor()
  {
    this.route.queryParamMap.subscribe((params) => {
      const token = params.get('token');

      if (!token)
      {
        this.validating.set(false);
        this.message.set('No recibimos un enlace de validación válido.');
        return;
      }

      this.authService.validarCuenta(token).subscribe({
        next: (respuesta) => {
          this.validating.set(false);
          this.validada.set(true);
          this.message.set(respuesta.mensaje);
        },
        error: (error) => {
          this.validating.set(false);
          this.message.set(
            typeof error.error === 'string'
              ? error.error
              : 'No fue posible confirmar el correo. Solicitá un nuevo enlace e intentá nuevamente.',
          );
        },
      });
    });
  }
}

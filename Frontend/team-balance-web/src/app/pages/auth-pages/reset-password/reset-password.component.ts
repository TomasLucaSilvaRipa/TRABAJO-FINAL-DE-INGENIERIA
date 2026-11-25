import { LocalizationService } from '../../../services/localization.service';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../services/auth.service';
import { AuthPageLayoutComponent } from '../../../shared/layout/auth-page-layout/auth-page-layout.component';

@Component({
  selector: 'app-reset-password',
  imports: [AuthPageLayoutComponent, ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password.component.html',
  styleUrl: '../../../shared/components/auth/auth-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResetPasswordComponent {
  readonly localization = inject(LocalizationService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);

  readonly submitting = signal(false);
  readonly requestError = signal('');
  readonly responseMessage = signal('');
  readonly token = this.route.snapshot.queryParamMap.get('token') ?? '';

  readonly resetPasswordForm = this.formBuilder.nonNullable.group({
    password: ['', [Validators.required, Validators.minLength(8)]],
    repetirPassword: ['', Validators.required],
  });

  onSubmit(): void {
    this.requestError.set('');
    this.responseMessage.set('');
    this.resetPasswordForm.markAllAsTouched();

    const form = this.resetPasswordForm.getRawValue();

    if (!this.token)
    {
      this.requestError.set('review.auth.resetInvalid');
      return;
    }

    if (this.resetPasswordForm.invalid || form.password !== form.repetirPassword)
    {
      this.requestError.set('review.auth.resetRules');
      return;
    }

    this.submitting.set(true);

    this.authService.restablecerPassword(this.token, form.password)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => this.responseMessage.set('review.auth.resetDone'),
        error: () => this.requestError.set('review.auth.resetFailed'),
      });
  }
}

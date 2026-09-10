import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocalizationService } from '../../../../services/localization.service';

@Component({
  selector: 'app-register-hours',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './register-hours.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterHoursComponent {
  readonly localization = inject(LocalizationService);
  readonly form = new FormBuilder().nonNullable.group({
    tarea: ['', Validators.required],
    fecha: [new Date().toISOString().slice(0, 10), Validators.required],
    horas: [0, [Validators.required, Validators.min(0.25), Validators.max(24)]],
    descripcion: ['', Validators.maxLength(500)],
  });

  enviado = false;

  registrar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.enviado = true;
  }
}

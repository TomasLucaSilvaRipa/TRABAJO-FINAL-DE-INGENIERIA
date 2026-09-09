import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-availability',
  imports: [CommonModule],
  templateUrl: './availability.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AvailabilityComponent {
  readonly dias = [
    { nombre: 'Lunes', disponible: true }, { nombre: 'Martes', disponible: true },
    { nombre: 'Miércoles', disponible: true }, { nombre: 'Jueves', disponible: true },
    { nombre: 'Viernes', disponible: true },
  ];
}

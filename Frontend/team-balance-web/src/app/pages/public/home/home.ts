import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  templateUrl: './home.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomeComponent {
  readonly stars = [1, 2, 3, 4, 5];
  readonly reviewNoticeOpen = signal(false);

  readonly reviews = [
    {
      rating: 5,
      title: 'La planificación dejó de depender de planillas',
      description: 'Podemos ver la carga real del equipo antes de asignar un proyecto. Eso nos permitió anticipar desvíos y conversar con los clientes con mucha más claridad.',
      name: 'Mariana López',
      role: 'Project Manager · Agencia creativa',
      date: '12 de agosto de 2026',
    },
    {
      rating: 5,
      title: 'Más contexto para decidir a quién asignar',
      description: 'La combinación de disponibilidad, skills y seniority nos dio una mirada más objetiva al armar los equipos de cada proyecto.',
      name: 'Franco Gómez',
      role: 'Director de Operaciones · Consultora digital',
      date: '28 de julio de 2026',
    },
    {
      rating: 4,
      title: 'Una forma más ordenada de acompañar al equipo',
      description: 'Los dashboards y alertas nos ayudan a detectar sobrecarga antes de que se convierta en un problema para las personas o los plazos.',
      name: 'Lucía Fernández',
      role: 'People Operations · Agencia de servicios',
      date: '06 de julio de 2026',
    },
  ];

  openReviewNotice(): void {
    this.reviewNoticeOpen.set(true);
  }

  closeReviewNotice(): void {
    this.reviewNoticeOpen.set(false);
  }
}

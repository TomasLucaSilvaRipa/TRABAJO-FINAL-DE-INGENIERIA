import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LocalizationService, type LanguageCode } from '../../../services/localization.service';
import { CustomerReview, ReviewsService } from '../../../services/reviews.service';

interface HomeReview extends CustomerReview {}
interface HomeContent {
  heroEyebrow: string; heroTitle: string; heroAccent: string; heroLead: string; discoverPlans: string; howItWorks: string; proof: string[];
  previewAvailable: string; previewIncrease: string; previewWorkload: string; previewPeople: string; previewAlert: string; previewProduct: string; previewDevelopment: string; previewDesign: string;
  overviewEyebrow: string; overviewTitle: string; overviewLead: string; cards: { title: string; text: string }[];
  stats: { value: string; label: string }[]; reviewsEyebrow: string; reviewsTitle: string; reviewsLead: string; stars: string;
  reviewPromptTitle: string; reviewPromptText: string; leaveReview: string; ctaEyebrow: string; ctaTitle: string; ctaLead: string;
  reviewDialogEyebrow: string; reviewDialogTitle: string; reviewDialogText: string; choosePlan: string; close: string;
}

const HOME_CONTENT: Record<LanguageCode, HomeContent> = {
  es: {
    heroEyebrow: 'Gestión inteligente de equipos', heroTitle: 'Asigná con datos.', heroAccent: 'Trabajá en equilibrio.', heroLead: 'TeamBalance ayuda a agencias y consultoras a planificar proyectos según la disponibilidad, los skills, el seniority y la carga real de cada persona.', discoverPlans: 'Conocer planes', howItWorks: 'Cómo funciona', proof: ['Menos asignaciones “a ojo”', 'Más visibilidad operativa', 'Decisiones con contexto'],
    previewAvailable: 'Capacidad disponible', previewIncrease: '12% más que la semana anterior', previewWorkload: 'Carga del equipo', previewPeople: '3 personas', previewAlert: 'liberan capacidad esta semana.', previewProduct: 'Producto', previewDevelopment: 'Desarrollo', previewDesign: 'Diseño',
    overviewEyebrow: 'Más que gestión de proyectos', overviewTitle: 'La capacidad de tu equipo deja de ser una intuición.', overviewLead: 'La plataforma centraliza las variables que impactan en la planificación para que el líder pueda anticipar desvíos antes de que se conviertan en horas extra, retrasos o pérdida de margen.',
    cards: [{ title: 'Capacidad visible', text: 'Visualizá disponibilidad, licencias y ocupación para evitar sobrecarga o subutilización.' }, { title: 'Mejor asignación', text: 'Contrastá skill, seniority, carga y plazo para encontrar el perfil más conveniente para cada tarea.' }, { title: 'Equipos sostenibles', text: 'Detectá riesgos de ocupación y tomá decisiones que cuiden el ritmo y la rentabilidad.' }],
    stats: [{ value: 'Skills + seniority', label: 'para evaluar cada asignación' }, { value: 'Riesgo proyectado', label: 'para anticipar desvíos de plazos' }, { value: 'Carga consolidada', label: 'para decidir con la misma información' }],
    reviewsEyebrow: 'Opiniones', reviewsTitle: 'La experiencia de quienes ya trabajan con TeamBalance.', reviewsLead: 'Conocé cómo agencias y consultoras usan la plataforma para organizar mejor la operación de sus equipos.', stars: 'de 5 estrellas', reviewPromptTitle: '¿Ya usás TeamBalance?', reviewPromptText: 'Compartí tu experiencia para ayudar a otras agencias a conocer la plataforma.', leaveReview: 'Dejar mi opinión', ctaEyebrow: 'Un mejor punto de partida', ctaTitle: 'Hacé que cada proyecto tenga el equipo que necesita.', ctaLead: 'Elegí la modalidad de suscripción que acompañe a tu agencia.', reviewDialogEyebrow: 'Opiniones de clientes', reviewDialogTitle: 'Tu opinión se publica desde tu cuenta.', reviewDialogText: 'Para dejar una opinión necesitás estar registrado y contar con una suscripción activa de TeamBalance.', choosePlan: 'Elegir un plan', close: 'Cerrar',
  },
  en: {
    heroEyebrow: 'Intelligent team management', heroTitle: 'Assign using data.', heroAccent: 'Work in balance.', heroLead: 'TeamBalance helps agencies and consulting firms plan projects around each person’s availability, skills, seniority and actual workload.', discoverPlans: 'Explore plans', howItWorks: 'How it works', proof: ['Fewer “gut-feel” assignments', 'More operational visibility', 'Context-based decisions'],
    previewAvailable: 'Available capacity', previewIncrease: '12% more than last week', previewWorkload: 'Team workload', previewPeople: '3 people', previewAlert: 'free up capacity this week.', previewProduct: 'Product', previewDevelopment: 'Development', previewDesign: 'Design',
    overviewEyebrow: 'More than project management', overviewTitle: 'Your team’s capacity stops being a guess.', overviewLead: 'The platform centralizes the variables that affect planning so leaders can anticipate deviations before they become overtime, delays or lost margin.',
    cards: [{ title: 'Visible capacity', text: 'View availability, leave and utilization to avoid overloading or underusing people.' }, { title: 'Better assignment', text: 'Compare skills, seniority, workload and deadlines to find the best profile for each task.' }, { title: 'Sustainable teams', text: 'Detect workload risks and make decisions that protect pace and profitability.' }],
    stats: [{ value: 'Skills + seniority', label: 'to assess every assignment' }, { value: 'Projected risk', label: 'to anticipate deadline deviations' }, { value: 'Consolidated workload', label: 'to decide using the same information' }],
    reviewsEyebrow: 'Reviews', reviewsTitle: 'The experience of teams already working with TeamBalance.', reviewsLead: 'Learn how agencies and consulting firms use the platform to better organize their team operations.', stars: 'out of 5 stars', reviewPromptTitle: 'Already using TeamBalance?', reviewPromptText: 'Share your experience to help other agencies learn about the platform.', leaveReview: 'Leave a review', ctaEyebrow: 'A better starting point', ctaTitle: 'Give every project the team it needs.', ctaLead: 'Choose the subscription option that supports your agency.', reviewDialogEyebrow: 'Customer reviews', reviewDialogTitle: 'Your review is published from your account.', reviewDialogText: 'To leave a review, you must be registered and have an active TeamBalance subscription.', choosePlan: 'Choose a plan', close: 'Close',
  },
};

const REVIEWS: Record<LanguageCode, HomeReview[]> = {
  es: [
    { rating: 5, title: 'La planificación dejó de depender de planillas', description: 'Podemos ver la carga real del equipo antes de asignar un proyecto. Eso nos permitió anticipar desvíos y conversar con los clientes con mucha más claridad.', name: 'Mariana López', role: 'Project Manager · Agencia creativa', date: '12 de agosto de 2026' },
    { rating: 5, title: 'Más contexto para decidir a quién asignar', description: 'La combinación de disponibilidad, skills y seniority nos dio una mirada más objetiva al armar los equipos de cada proyecto.', name: 'Franco Gómez', role: 'Director de Operaciones · Consultora digital', date: '28 de julio de 2026' },
    { rating: 4, title: 'Una forma más ordenada de acompañar al equipo', description: 'Los dashboards y alertas nos ayudan a detectar sobrecarga antes de que se convierta en un problema para las personas o los plazos.', name: 'Lucía Fernández', role: 'People Operations · Agencia de servicios', date: '06 de julio de 2026' },
  ],
  en: [
    { rating: 5, title: 'Planning no longer depends on spreadsheets', description: 'We can see the team’s actual workload before assigning a project. That helps us anticipate deviations and speak with clients much more clearly.', name: 'Mariana López', role: 'Project Manager · Creative agency', date: 'August 12, 2026' },
    { rating: 5, title: 'More context to decide who to assign', description: 'Combining availability, skills and seniority gives us a more objective view when building teams for each project.', name: 'Franco Gómez', role: 'Operations Director · Digital consultancy', date: 'July 28, 2026' },
    { rating: 4, title: 'A more organized way to support the team', description: 'Dashboards and alerts help us identify overload before it becomes a problem for people or deadlines.', name: 'Lucía Fernández', role: 'People Operations · Service agency', date: 'July 6, 2026' },
  ],
};

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  templateUrl: './home.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomeComponent {
  private readonly localization = inject(LocalizationService);
  private readonly reviewsService = inject(ReviewsService);
  readonly stars = [1, 2, 3, 4, 5];
  readonly reviewNoticeOpen = signal(false);
  readonly content = computed(() => HOME_CONTENT[this.localization.language()]);
  readonly reviews = computed(() => [...this.reviewsService.reviews(), ...REVIEWS[this.localization.language()]]);

  openReviewNotice(): void {
    this.reviewNoticeOpen.set(true);
  }

  closeReviewNotice(): void {
    this.reviewNoticeOpen.set(false);
  }
}

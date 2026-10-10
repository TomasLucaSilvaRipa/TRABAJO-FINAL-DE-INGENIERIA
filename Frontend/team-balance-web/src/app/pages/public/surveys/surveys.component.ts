import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LocalizationService } from '../../../services/localization.service';
import { Encuesta, RespuestaEncuesta, RespuestaPreguntaEncuesta, SurveysService } from '../../../services/surveys.service';

@Component({
  selector: 'app-public-surveys',
  imports: [FormsModule, RouterLink],
  templateUrl: './surveys.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicSurveysComponent {
  readonly localization = inject(LocalizationService);
  private readonly surveysService = inject(SurveysService);
  readonly encuestas = signal<Encuesta[]>([]);
  readonly encuestaSeleccionada = signal<Encuesta | null>(null);
  readonly respuestas = signal<Record<number, number>>({});
  readonly cargando = signal(true);
  readonly enviando = signal(false);
  readonly mensaje = signal('');
  readonly error = signal('');

  constructor() {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.surveysService.publicas().subscribe({
      next: encuestas => {
        this.encuestas.set(encuestas);
        this.cargando.set(false);
        if (encuestas.length > 0) { this.seleccionar(encuestas[0]); }
      },
      error: () => {
        this.error.set(this.t('surveys.publicLoadError'));
        this.cargando.set(false);
      },
    });
  }

  seleccionar(encuesta: Encuesta): void {
    this.error.set('');
    this.mensaje.set('');
    this.surveysService.publica(encuesta).subscribe({
      next: detalle => {
        this.encuestaSeleccionada.set(detalle);
        this.respuestas.set({});
      },
      error: respuesta => this.error.set(respuesta?.error ?? this.t('surveys.publicLoadError')),
    });
  }

  seleccionarOpcion(idPreguntaEncuesta: number, idOpcionEncuesta: number): void {
    this.respuestas.update(respuestasActuales => ({ ...respuestasActuales, [idPreguntaEncuesta]: idOpcionEncuesta }));
  }

  enviar(): void {
    const encuesta = this.encuestaSeleccionada();
    if (!encuesta) { return; }
    if (Object.keys(this.respuestas()).length !== encuesta.preguntas.length) {
      this.error.set(this.t('surveys.completeAll'));
      return;
    }
    const respuestas: RespuestaPreguntaEncuesta[] = encuesta.preguntas.map(pregunta => ({ idPreguntaEncuesta: pregunta.id, idOpcionEncuesta: this.respuestas()[pregunta.id] }));
    const respuesta: RespuestaEncuesta = { idEncuesta: encuesta.id, identificadorParticipante: this.surveysService.identificadorParticipante(), respuestas };
    this.enviando.set(true);
    this.error.set('');
    this.surveysService.responder(respuesta).subscribe({
      next: () => {
        this.enviando.set(false);
        this.mensaje.set(this.t('surveys.sent'));
        this.encuestaSeleccionada.set(null);
      },
      error: respuestaError => {
        this.enviando.set(false);
        this.error.set(respuestaError?.error ?? this.t('surveys.sendError'));
      },
    });
  }

  texto(tituloEs: string, tituloEn: string): string {
    return this.localization.language() === 'en' ? tituloEn : tituloEs;
  }

  t(clave: string): string {
    return this.localization.traducir(clave);
  }
}

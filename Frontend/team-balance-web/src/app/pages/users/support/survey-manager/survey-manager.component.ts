import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Encuesta, OpcionEncuesta, PreguntaEncuesta, ResultadoEncuesta, SurveysService } from '../../../../services/surveys.service';
import { LocalizationService } from '../../../../services/localization.service';
import { SurveyResultsChartComponent } from './survey-results-chart.component';

@Component({
  selector: 'app-survey-manager',
  imports: [DatePipe, ReactiveFormsModule, SurveyResultsChartComponent],
  templateUrl: './survey-manager.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SurveyManagerComponent {
  readonly localization = inject(LocalizationService);
  private readonly surveysService = inject(SurveysService);
  readonly encuestas = signal<Encuesta[]>([]);
  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly mensaje = signal('');
  readonly error = signal('');
  readonly resultados = signal<ResultadoEncuesta | null>(null);
  readonly formulario = new FormGroup({
    id: new FormControl(0, { nonNullable: true }),
    tituloEs: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(180)] }),
    tituloEn: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(180)] }),
    descripcionEs: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(600)] }),
    descripcionEn: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(600)] }),
    fechaInicio: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    fechaVencimiento: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    preguntas: new FormArray<FormGroup>([]),
  });

  constructor() {
    this.nueva();
    this.cargar();
  }

  get preguntas(): FormArray<FormGroup> {
    return this.formulario.controls.preguntas;
  }

  cargar(): void {
    this.cargando.set(true);
    this.surveysService.gestion().subscribe({
      next: encuestas => {
        this.encuestas.set(encuestas);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set(this.t('surveyManager.loadError'));
        this.cargando.set(false);
      },
    });
  }

  nueva(): void {
    this.formulario.reset({ id: 0, tituloEs: '', tituloEn: '', descripcionEs: '', descripcionEn: '', fechaInicio: this.fechaLocal(new Date()), fechaVencimiento: this.fechaLocal(new Date(Date.now() + 30 * 24 * 60 * 60 * 1000)) });
    this.preguntas.clear();
    this.agregarPregunta();
    this.mensaje.set('');
    this.error.set('');
    this.resultados.set(null);
  }

  editar(encuesta: Encuesta): void {
    this.surveysService.detalleGestion(encuesta).subscribe({
      next: detalle => {
        this.formulario.patchValue({ id: detalle.id, tituloEs: detalle.tituloEs, tituloEn: detalle.tituloEn, descripcionEs: detalle.descripcionEs, descripcionEn: detalle.descripcionEn, fechaInicio: this.fechaLocal(new Date(detalle.fechaInicio)), fechaVencimiento: this.fechaLocal(new Date(detalle.fechaVencimiento)) });
        this.preguntas.clear();
        detalle.preguntas.forEach(pregunta => this.preguntas.push(this.crearPregunta(pregunta)));
        this.mensaje.set('');
        this.error.set('');
        this.resultados.set(null);
      },
      error: respuesta => this.error.set(respuesta?.error ?? this.t('surveyManager.loadError')),
    });
  }

  agregarPregunta(): void {
    const orden = this.preguntas.length + 1;
    this.preguntas.push(this.crearPregunta({ id: 0, idEncuesta: 0, enunciadoEs: '', enunciadoEn: '', orden, opciones: [this.opcionVacia(1), this.opcionVacia(2)] }));
  }

  quitarPregunta(indicePregunta: number): void {
    if (this.preguntas.length === 1) { return; }
    this.preguntas.removeAt(indicePregunta);
    this.ordenarPreguntas();
  }

  agregarOpcion(indicePregunta: number): void {
    const opciones = this.opciones(indicePregunta);
    if (opciones.length >= 6) { return; }
    opciones.push(this.crearOpcion(this.opcionVacia(opciones.length + 1)));
  }

  quitarOpcion(indicePregunta: number, indiceOpcion: number): void {
    const opciones = this.opciones(indicePregunta);
    if (opciones.length <= 2) { return; }
    opciones.removeAt(indiceOpcion);
    opciones.controls.forEach((opcion, indice) => opcion.controls['orden'].setValue(indice + 1));
  }

  guardar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      this.error.set(this.t('surveyManager.invalid'));
      return;
    }
    const datos = this.formulario.getRawValue();
    const encuesta: Encuesta = {
      id: datos.id,
      tituloEs: datos.tituloEs,
      tituloEn: datos.tituloEn,
      descripcionEs: datos.descripcionEs,
      descripcionEn: datos.descripcionEn,
      fechaInicio: datos.fechaInicio,
      fechaVencimiento: datos.fechaVencimiento,
      activo: true,
      cantidadRespuestas: 0,
      preguntas: datos.preguntas as PreguntaEncuesta[],
    };
    this.guardando.set(true);
    this.error.set('');
    this.surveysService.guardar(encuesta).subscribe({
      next: () => {
        this.guardando.set(false);
        this.mensaje.set(this.t('surveyManager.saved'));
        this.nueva();
        this.cargar();
      },
      error: respuesta => {
        this.guardando.set(false);
        this.error.set(respuesta?.error ?? this.t('surveyManager.saveError'));
      },
    });
  }

  baja(encuesta: Encuesta): void {
    if (!confirm(this.t('surveyManager.confirmDeactivate'))) { return; }
    this.surveysService.baja(encuesta).subscribe({
      next: () => {
        this.mensaje.set(this.t('surveyManager.deactivated'));
        this.cargar();
      },
      error: respuesta => this.error.set(respuesta?.error ?? this.t('surveyManager.deactivateError')),
    });
  }

  verResultados(encuesta: Encuesta): void {
    this.surveysService.resultados(encuesta).subscribe({
      next: resultados => {
        this.resultados.set(resultados);
        this.error.set('');
      },
      error: respuesta => this.error.set(respuesta?.error ?? this.t('surveyManager.resultsError')),
    });
  }

  opciones(indicePregunta: number): FormArray<FormGroup> {
    return this.preguntas.at(indicePregunta).controls['opciones'] as FormArray<FormGroup>;
  }

  texto(tituloEs: string, tituloEn: string): string {
    return this.localization.language() === 'en' ? tituloEn : tituloEs;
  }

  t(clave: string): string {
    return this.localization.traducir(clave);
  }

  private crearPregunta(pregunta: PreguntaEncuesta): FormGroup {
    const opciones = new FormArray<FormGroup>([]);
    pregunta.opciones.forEach(opcion => opciones.push(this.crearOpcion(opcion)));
    return new FormGroup({
      id: new FormControl(pregunta.id, { nonNullable: true }),
      idEncuesta: new FormControl(pregunta.idEncuesta, { nonNullable: true }),
      enunciadoEs: new FormControl(pregunta.enunciadoEs, { nonNullable: true, validators: [Validators.required, Validators.maxLength(300)] }),
      enunciadoEn: new FormControl(pregunta.enunciadoEn, { nonNullable: true, validators: [Validators.required, Validators.maxLength(300)] }),
      orden: new FormControl(pregunta.orden, { nonNullable: true }),
      opciones,
    });
  }

  private crearOpcion(opcion: OpcionEncuesta): FormGroup {
    return new FormGroup({
      id: new FormControl(opcion.id, { nonNullable: true }),
      idPreguntaEncuesta: new FormControl(opcion.idPreguntaEncuesta, { nonNullable: true }),
      textoEs: new FormControl(opcion.textoEs, { nonNullable: true, validators: [Validators.required, Validators.maxLength(200)] }),
      textoEn: new FormControl(opcion.textoEn, { nonNullable: true, validators: [Validators.required, Validators.maxLength(200)] }),
      orden: new FormControl(opcion.orden, { nonNullable: true }),
    });
  }

  private opcionVacia(orden: number): OpcionEncuesta {
    return { id: 0, idPreguntaEncuesta: 0, textoEs: '', textoEn: '', orden };
  }

  private ordenarPreguntas(): void {
    this.preguntas.controls.forEach((pregunta, indice) => pregunta.controls['orden'].setValue(indice + 1));
  }

  private fechaLocal(fecha: Date): string {
    const desfase = fecha.getTimezoneOffset() * 60 * 1000;
    return new Date(fecha.getTime() - desfase).toISOString().slice(0, 16);
  }
}

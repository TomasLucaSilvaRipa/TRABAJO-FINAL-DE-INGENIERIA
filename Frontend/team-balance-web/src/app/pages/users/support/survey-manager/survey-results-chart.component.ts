import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, Input, OnChanges, OnDestroy, SimpleChanges, ViewChild } from '@angular/core';
import { ArcElement, Chart, DoughnutController, Legend, Tooltip } from 'chart.js';
import { ResultadoPreguntaEncuesta } from '../../../../services/surveys.service';

Chart.register(ArcElement, DoughnutController, Legend, Tooltip);

@Component({
  selector: 'app-survey-results-chart',
  templateUrl: './survey-results-chart.component.html',
  styleUrl: './survey-results-chart.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SurveyResultsChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @Input({ required: true }) pregunta!: ResultadoPreguntaEncuesta;
  @Input({ required: true }) idioma!: 'es' | 'en';
  @Input({ required: true }) etiquetaRespuestas!: string;
  @ViewChild('grafico', { static: true }) private graficoElemento!: ElementRef<HTMLCanvasElement>;

  private readonly colores = ['#22d3ee', '#a3e635', '#38bdf8', '#facc15', '#818cf8', '#fb7185'];
  private grafico?: Chart<'doughnut', number[], string>;

  ngAfterViewInit(): void {
    this.crearGrafico();
  }

  ngOnChanges(cambios: SimpleChanges): void {
    if (this.grafico && (cambios['pregunta'] || cambios['idioma'])) {
      this.crearGrafico();
    }
  }

  ngOnDestroy(): void {
    this.grafico?.destroy();
  }

  etiquetaOpcion(indice: number): string {
    return this.idioma === 'en' ? this.pregunta.opciones[indice].textoEn : this.pregunta.opciones[indice].textoEs;
  }

  colorOpcion(indice: number): string {
    return this.colores[indice % this.colores.length];
  }

  cantidadRespuestas(): number {
    return this.pregunta.opciones.reduce((acumulado, opcion) => acumulado + opcion.cantidadRespuestas, 0);
  }

  private crearGrafico(): void {
    this.grafico?.destroy();
    this.grafico = new Chart(this.graficoElemento.nativeElement, {
      type: 'doughnut',
      data: {
        labels: this.pregunta.opciones.map((opcion, indice) => this.idioma === 'en' ? opcion.textoEn : opcion.textoEs),
        datasets: [{ data: this.pregunta.opciones.map(opcion => opcion.cantidadRespuestas), backgroundColor: this.pregunta.opciones.map((opcion, indice) => this.colorOpcion(indice)), borderColor: '#082f49', borderWidth: 3, hoverOffset: 8 }],
      },
      options: {
        animation: { animateRotate: true, animateScale: true },
        cutout: '68%',
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: '#082f49',
            bodyColor: '#f8fafc',
            borderColor: '#22d3ee',
            borderWidth: 1,
            displayColors: true,
            padding: 12,
            callbacks: {
              label: contexto => {
                const cantidad = Number(contexto.raw);
                const total = this.cantidadRespuestas();
                const porcentaje = total === 0 ? 0 : Math.round((cantidad / total) * 100);
                return `${contexto.label}: ${porcentaje}% · ${cantidad}`;
              },
            },
          },
        },
      },
    });
  }
}

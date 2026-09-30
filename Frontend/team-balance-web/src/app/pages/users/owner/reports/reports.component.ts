import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProjectsService, Proyecto } from '../../../../services/projects.service';
import { ConfiguracionReporteEjecutivo, ReporteEjecutivo, ReporteEjecutivoService } from '../../../../services/reporte-ejecutivo.service';
import { DetalleRiesgoProyecto, FiltroRiesgoRetraso, RiesgoRetrasoService } from '../../../../services/riesgo-retraso.service';

@Component({
  selector: 'app-reports',
  imports: [DatePipe, FormsModule],
  templateUrl: './reports.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Reports {
  private readonly projectsService = inject(ProjectsService);
  private readonly riesgoService = inject(RiesgoRetrasoService);
  private readonly reporteService = inject(ReporteEjecutivoService);
  protected readonly proyectos = signal<Proyecto[]>([]);
  protected readonly riesgos = signal<DetalleRiesgoProyecto[]>([]);
  protected readonly cargando = signal(false);
  protected readonly error = signal('');
  protected readonly reporte = signal<ReporteEjecutivo | null>(null);
  protected readonly generandoReporte = signal(false);
  protected filtro: FiltroRiesgoRetraso = {};
  protected configuracionReporte: ConfiguracionReporteEjecutivo = {
    titulo: 'Reporte ejecutivo de gestión',
    filtro: { alcanceGeneral: true },
    incluirOcupacionOperativa: true,
    incluirRiesgoRetraso: true,
    incluirDesvioHoras: true,
    incluirEficienciaPerfiles: true,
    incluirConvenienciaOperativa: true,
    incluirTodasLasSecciones: true,
  };

  constructor() {
    this.projectsService.consultar().subscribe({ next: proyectos => this.proyectos.set(proyectos.filter(proyecto => proyecto.activo)), error: () => this.error.set('No se pudieron cargar los proyectos para aplicar filtros.') });
  }

  protected consultarRiesgos(): void {
    this.cargando.set(true);
    this.error.set('');
    this.riesgoService.consultar(this.filtro).subscribe({
      next: resultado => {
        this.riesgos.set(resultado.proyectos);
        this.cargando.set(false);
      },
      error: error => {
        this.error.set(error?.error || 'No se pudo calcular el riesgo de retraso.');
        this.cargando.set(false);
      },
    });
  }

  protected generarReporte(): void {
    this.generandoReporte.set(true);
    this.error.set('');
    this.configuracionReporte.filtro.idProyecto = this.filtro.idProyecto;
    this.reporteService.previsualizar(this.configuracionReporte).subscribe({
      next: reporte => {
        this.reporte.set(reporte);
        this.generandoReporte.set(false);
      },
      error: error => {
        this.error.set(error?.error || 'No se pudo generar el reporte ejecutivo.');
        this.generandoReporte.set(false);
      },
    });
  }

  protected imprimirReporte(): void {
    window.print();
  }
}

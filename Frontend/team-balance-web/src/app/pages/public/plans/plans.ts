import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LocalizationService } from '../../../services/localization.service';
import { PlanComercial, PlanComercialService } from '../../../services/plan-comercial.service';
import { PlanComparisonComponent } from '../../../shared/components/plans/plan-comparison/plan-comparison.component';
import { ConsultaPlan, PlanConsultasService } from '../../../services/plan-consultas.service';
@Component({
  selector: 'app-plans',
  imports: [RouterLink, PlanComparisonComponent, ReactiveFormsModule],
  templateUrl: './plans.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlansComponent {
  readonly localization = inject(LocalizationService);
  private readonly planService = inject(PlanComercialService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly consultasService = inject(PlanConsultasService);
  readonly plans = signal<PlanComercial[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly comparisonVisible = signal(false);
  readonly selectedPlanIds = signal<number[]>([]);
  readonly comparisonError = signal('');
  readonly planConsultado = signal<PlanComercial | null>(null);
  readonly consultas = signal<ConsultaPlan[]>([]);
  readonly cargandoConsultas = signal(false);
  readonly guardandoConsulta = signal(false);
  readonly consultaError = signal('');
  readonly consultaForm = this.formBuilder.nonNullable.group({ nombre: ['', [Validators.required, Validators.minLength(2)]], email: ['', [Validators.required, Validators.email]], consulta: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(1000)]] });

  constructor() {
    this.loadPlans();
  }

  loadPlans(): void {
    this.loading.set(true);
    this.error.set(false);
    this.planService.consultarPlanesActivos().subscribe({
      next: (plans) => { this.plans.set(plans); this.selectedPlanIds.update((ids) => ids.filter((id) => plans.some((plan) => plan.id === id))); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
  }

  toggleComparison(): void {
    if (this.selectedPlanIds().length < 2) { this.comparisonError.set(this.localization.traducir('plans.comparison.minimum')); return; }
    this.comparisonError.set(''); this.comparisonVisible.update((visible) => !visible);
  }

  togglePlanSelection(idPlan: number): void { const seleccionados = this.selectedPlanIds(); if (seleccionados.includes(idPlan)) { this.selectedPlanIds.set(seleccionados.filter((id) => id !== idPlan)); this.comparisonVisible.set(false); this.comparisonError.set(''); return; } if (seleccionados.length >= 4) { this.comparisonError.set(this.localization.traducir('plans.comparison.maximum')); return; } this.selectedPlanIds.set([...seleccionados, idPlan]); this.comparisonError.set(''); }
  planSeleccionado(idPlan: number): boolean { return this.selectedPlanIds().includes(idPlan); }
  planesSeleccionados(): PlanComercial[] { return this.plans().filter((plan) => this.selectedPlanIds().includes(plan.id)); }
  abrirConsultas(plan: PlanComercial): void { this.planConsultado.set(plan); this.consultaError.set(''); this.consultaForm.reset({ nombre: '', email: '', consulta: '' }); this.cargarConsultas(plan.id); }
  cerrarConsultas(): void { this.planConsultado.set(null); this.consultas.set([]); this.consultaError.set(''); }

  guardarConsulta(): void {
    const plan = this.planConsultado(); this.consultaForm.markAllAsTouched();
    if (!plan || this.guardandoConsulta()) { return; }
    if (this.consultaForm.invalid) { this.consultaError.set(this.localization.traducir('plans.questions.completeRequired')); return; }
    const valores = this.consultaForm.getRawValue();
    const consulta = { id: 0, idPlanComercial: plan.id, nombre: valores.nombre, email: valores.email, consulta: valores.consulta, fechaAlta: new Date().toISOString(), activo: true } as ConsultaPlan;
    this.guardandoConsulta.set(true);
    this.consultaError.set('');
    this.consultasService.registrar(consulta).subscribe({ next: (resultado) => {
      this.consultas.update((consultas) => [resultado, ...consultas]);
      this.consultaForm.reset({ nombre: '', email: '', consulta: '' });
      this.guardandoConsulta.set(false); }, error: (error) => {
        this.consultaError.set(typeof error.error === 'string' ? error.error : this.localization.traducir('plans.questions.saveError'));
        this.guardandoConsulta.set(false);
      }
    });
  }

  private cargarConsultas(idPlan: number): void {
    this.cargandoConsultas.set(true);
    this.consultasService.consultar(idPlan).subscribe({ next: (consultas) => {
      this.consultas.set(consultas);
      this.cargandoConsultas.set(false); }, error: () => {
        this.consultaError.set(this.localization.traducir('plans.questions.loadError'));
        this.cargandoConsultas.set(false);
      }
    });
  }

  features(plan: PlanComercial): string[] {
    return this.planService.obtenerFuncionalidades(plan).slice(0, 8);
  }

  featureLabel(feature: string): string {
    return this.localization.traducir(`plans.feature.${feature}`, undefined, feature);
  }

  description(plan: PlanComercial): string {
    return this.localization.traducir(`plans.description.${plan.nombre.toLowerCase()}`, undefined, plan.descripcion ?? '');
  }
}

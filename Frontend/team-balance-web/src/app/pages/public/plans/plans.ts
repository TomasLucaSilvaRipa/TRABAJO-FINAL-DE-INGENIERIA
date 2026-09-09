import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LocalizationService } from '../../../services/localization.service';
import { PlanComercial, PlanComercialService } from '../../../services/plan-comercial.service';
import { PlanComparisonComponent } from '../../../shared/components/plans/plan-comparison/plan-comparison.component';
@Component({
  selector: 'app-plans',
  imports: [RouterLink, PlanComparisonComponent],
  templateUrl: './plans.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlansComponent {
  readonly localization = inject(LocalizationService);
  private readonly planService = inject(PlanComercialService);
  readonly plans = signal<PlanComercial[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly comparisonVisible = signal(false);

  constructor() {
    this.loadPlans();
  }

  loadPlans(): void {
    this.loading.set(true);
    this.error.set(false);
    this.planService.consultarPlanesActivos().subscribe({
      next: (plans) => { this.plans.set(plans); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
  }

  toggleComparison(): void {
    this.comparisonVisible.update((visible) => !visible);
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

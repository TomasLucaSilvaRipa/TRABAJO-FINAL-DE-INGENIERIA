import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { LocalizationService } from '../../../../services/localization.service';
import { PlanComercial, PlanComercialService } from '../../../../services/plan-comercial.service';

@Component({
  selector: 'app-plan-comparison',
  templateUrl: './plan-comparison.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlanComparisonComponent {
  readonly plans = input.required<PlanComercial[]>();
  readonly localization = inject(LocalizationService);
  private readonly planService = inject(PlanComercialService);

  readonly features = computed(() => Array.from(new Set(this.plans().flatMap((plan) => this.planService.obtenerFuncionalidades(plan)))));

  includes(plan: PlanComercial, feature: string): boolean {
    return this.planService.obtenerFuncionalidades(plan).includes(feature);
  }

  featureLabel(feature: string): string {
    return this.localization.traducir(`plans.feature.${feature}`, undefined, feature);
  }
}

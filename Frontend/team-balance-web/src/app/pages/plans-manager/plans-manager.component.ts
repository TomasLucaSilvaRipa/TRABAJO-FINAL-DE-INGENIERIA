import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocalizationService } from '../../services/localization.service';
import { PlanComercial, PlanComercialService } from '../../services/plan-comercial.service';
import { PlanComparisonComponent } from '../../shared/components/plans/plan-comparison/plan-comparison.component';

@Component({
  selector: 'app-plans-manager',
  imports: [ReactiveFormsModule, PlanComparisonComponent],
  templateUrl: './plans-manager.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlansManager {
  readonly localization = inject(LocalizationService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly planService = inject(PlanComercialService);
  readonly plans = signal<PlanComercial[]>([]);
  readonly selectedId = signal<number | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly comparison = signal(false);
  readonly message = signal('');
  readonly error = signal('');

  readonly form = this.formBuilder.nonNullable.group({
    nombre: ['', Validators.required],
    descripcion: [''],
    precioVigente: [0, [Validators.required, Validators.min(0.01)]],
    moneda: ['USD', Validators.required],
    periodicidad: ['Mensual', Validators.required],
    duracionMeses: [1, [Validators.required, Validators.min(1)]],
    alcanceFuncional: ['', Validators.required],
    condicionesRenovacion: ['Renovación mensual.'],
    activo: [true],
    fechaVigenciaDesde: [new Date().toISOString().slice(0, 10), Validators.required],
    fechaVigenciaHasta: [''],
  });

  constructor() {
    this.loadPlans();
  }

  loadPlans(): void {
    this.loading.set(true);
    this.planService.consultarPlanes().subscribe({
      next: (plans) => { this.plans.set(plans); this.loading.set(false); },
      error: (error) => { this.error.set(this.errorMessage(error)); this.loading.set(false); },
    });
  }

  edit(plan: PlanComercial): void {
    this.selectedId.set(plan.id);
    this.message.set('');
    this.error.set('');
    this.form.setValue({
      nombre: plan.nombre,
      descripcion: plan.descripcion ?? '',
      precioVigente: plan.precioVigente,
      moneda: plan.moneda,
      periodicidad: plan.periodicidad,
      duracionMeses: plan.duracionMeses,
      alcanceFuncional: plan.alcanceFuncional ?? '',
      condicionesRenovacion: plan.condicionesRenovacion ?? '',
      activo: plan.activo,
      fechaVigenciaDesde: plan.fechaVigenciaDesde.slice(0, 10),
      fechaVigenciaHasta: plan.fechaVigenciaHasta?.slice(0, 10) ?? '',
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  newPlan(): void {
    this.selectedId.set(null);
    this.message.set('');
    this.error.set('');
    this.form.reset({ moneda: 'USD', periodicidad: 'Mensual', duracionMeses: 1, activo: true, fechaVigenciaDesde: new Date().toISOString().slice(0, 10), condicionesRenovacion: 'Renovación mensual.' });
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.saving()) { return; }

    const value = this.form.getRawValue();
    const plan: PlanComercial = {
      id: this.selectedId() ?? 0,
      nombre: value.nombre,
      descripcion: value.descripcion || null,
      periodicidad: value.periodicidad,
      precioVigente: value.precioVigente,
      moneda: value.moneda,
      duracionMeses: value.duracionMeses,
      alcanceFuncional: value.alcanceFuncional,
      condicionesRenovacion: value.condicionesRenovacion || null,
      activo: value.activo,
      fechaVigenciaDesde: new Date(`${value.fechaVigenciaDesde}T00:00:00`).toISOString(),
      fechaVigenciaHasta: value.fechaVigenciaHasta ? new Date(`${value.fechaVigenciaHasta}T00:00:00`).toISOString() : null,
    };

    this.saving.set(true);
    this.message.set('');
    this.error.set('');
    const operation = plan.id ? this.planService.modificarPlan(plan) : this.planService.registrarPlan(plan);
    operation.subscribe({
      next: () => { this.saving.set(false); this.message.set(this.localization.traducir('planManager.saved')); this.newPlan(); this.message.set(this.localization.traducir('planManager.saved')); this.loadPlans(); },
      error: (error) => { this.saving.set(false); this.error.set(this.errorMessage(error)); },
    });
  }

  changeStatus(plan: PlanComercial): void {
    this.planService.cambiarEstado(plan, !plan.activo).subscribe({
      next: () => this.loadPlans(),
      error: (error) => this.error.set(this.errorMessage(error)),
    });
  }

  private errorMessage(error: { error?: string }): string {
    return typeof error?.error === 'string' ? error.error : this.localization.traducir('planManager.error');
  }
}

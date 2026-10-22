import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { LocalizationService } from '../../../../services/localization.service';
import { PlatformRevenueService, PlatformRevenueSummary } from '../../../../services/platform-revenue.service';

@Component({
  selector: 'app-platform-revenue',
  imports: [DatePipe, DecimalPipe],
  templateUrl: './platform-revenue.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlatformRevenueComponent {
  readonly localization = inject(LocalizationService);
  private readonly revenueService = inject(PlatformRevenueService);
  readonly summary = signal<PlatformRevenueSummary | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.revenueService.consultarResumen().subscribe({
      next: summary => { this.summary.set(summary); this.loading.set(false); },
      error: error => { this.error.set(typeof error?.error === 'string' ? error.error : this.localization.traducir('platformRevenue.loadError')); this.loading.set(false); },
    });
  }
}

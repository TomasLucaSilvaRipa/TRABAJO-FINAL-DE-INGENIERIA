import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { LocalizationService } from '../../../services/localization.service';
import { OperationalEmailService, TaskEmailAction } from '../../../services/operational-email.service';

@Component({ selector: 'app-task-email-action', standalone: true, imports: [RouterLink], templateUrl: './task-email-action.component.html', styleUrl: './task-email-action.component.css', changeDetection: ChangeDetectionStrategy.OnPush })
export class TaskEmailActionComponent {
  private readonly route = inject(ActivatedRoute); private readonly service = inject(OperationalEmailService);
  readonly localization = inject(LocalizationService); readonly loading = signal(true); readonly processing = signal(false); readonly completed = signal(false); readonly error = signal<string | null>(null); readonly action = signal<TaskEmailAction | null>(null);
  private readonly token = this.route.snapshot.queryParamMap.get('token')?.trim() ?? '';
  constructor() { void this.load(); }
  async confirm(): Promise<void> {
    if (!this.token || this.processing()) return;
    this.processing.set(true); this.error.set(null);
    try { this.action.set(await firstValueFrom(this.service.executeAction(this.token))); this.completed.set(true); }
    catch (error: unknown) { this.error.set(this.message(error)); }
    finally { this.processing.set(false); }
  }
  private async load(): Promise<void> {
    if (!this.token) { this.error.set(this.localization.traducir('private.taskAction.invalid')); this.loading.set(false); return; }
    try { this.action.set(await firstValueFrom(this.service.getAction(this.token))); }
    catch (error: unknown) { this.error.set(this.message(error)); }
    finally { this.loading.set(false); }
  }
  private message(error: unknown): string { const value=error as {error?:string|{message?:string};message?:string}; return typeof value.error==='string'?value.error:value.error?.message||value.message||this.localization.traducir('private.taskAction.invalid'); }
}

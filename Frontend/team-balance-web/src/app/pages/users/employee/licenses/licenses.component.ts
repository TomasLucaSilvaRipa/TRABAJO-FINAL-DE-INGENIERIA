import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({ selector: 'app-licenses', standalone: true, imports: [RouterModule], templateUrl: './licenses.component.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class LicensesComponent {}

import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-proyect-management',
  imports: [RouterModule],
  templateUrl: './proyect-management.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProyectManagement {}

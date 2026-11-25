import { inject } from '@angular/core';
import { LocalizationService } from '../../../services/localization.service';
import { Component } from '@angular/core';
import { GridShapeComponent } from '../../../shared/components/common/grid-shape/grid-shape.component';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-not-found',
  imports: [
    GridShapeComponent,
    RouterModule,
  ],
  templateUrl: './not-found.component.html',
  styles: ``
})
export class NotFoundComponent {
  readonly localization = inject(LocalizationService);

  currentYear: number = new Date().getFullYear();
}

import { Component, inject } from '@angular/core';
import { LocalizationService } from '../../../services/localization.service';

@Component({
  selector: 'app-auth-page-layout',
  imports: [],
  templateUrl: './auth-page-layout.component.html',
  styleUrl: './auth-page-layout.component.css',
})
export class AuthPageLayoutComponent {
  readonly localization = inject(LocalizationService);
}

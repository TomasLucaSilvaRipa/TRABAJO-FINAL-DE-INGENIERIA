import { Component, input, signal,inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { LanguageSelector } from '../../../components/language-selector/language-selector.component';
import { TranslatePipe } from '../../../../pipes/translate.pipe';
import { LocalizationService } from '../../../../services/localization.service';

@Component({
  selector: 'app-public-header',
  imports: [
    RouterLink,
    RouterLinkActive,
    LanguageSelector,
    TranslatePipe,
  ],
  templateUrl: './public-header.component.html',
  // styleUrl: './public-header.component.css'
})
export class PublicHeader {
  readonly contrastMode = input(false);
  protected readonly menuOpen = signal(false);
  readonly localization = inject(LocalizationService);

  protected toggleMenu(): void {
    this.menuOpen.update((isOpen) => !isOpen);
  }

  protected closeMenu(): void {
    this.menuOpen.set(false);
  }
}

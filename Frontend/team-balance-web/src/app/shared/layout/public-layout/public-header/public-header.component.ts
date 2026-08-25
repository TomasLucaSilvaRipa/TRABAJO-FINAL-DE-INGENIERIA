import { Component, input, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-public-header',
  imports: [
    RouterLink,
    RouterLinkActive
  ],
  templateUrl: './public-header.component.html',
  // styleUrl: './public-header.component.css'
})
export class PublicHeader {
  readonly contrastMode = input(false);
  protected readonly menuOpen = signal(false);

  protected toggleMenu(): void {
    this.menuOpen.update((isOpen) => !isOpen);
  }

  protected closeMenu(): void {
    this.menuOpen.set(false);
  }
}

import { Component, signal, ViewEncapsulation } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PublicHeader } from '../public-layout/public-header/public-header.component';
import { PublicFooterComponent } from '../public-layout/public-footer/public-footer.component';

@Component({
  selector: 'app-public-layout',
  imports: [
    RouterOutlet,
    PublicHeader,
    PublicFooterComponent
  ],
  templateUrl: './public-layout.component.html',
  styleUrl: './public-layout.component.css',
  encapsulation: ViewEncapsulation.None,
})
export class PublicLayoutComponent {
  publicheader = new PublicHeader();
  protected readonly contrastMode = signal(false);

  protected toggleContrast(): void {
    this.contrastMode.update((enabled) => !enabled);
  }
}

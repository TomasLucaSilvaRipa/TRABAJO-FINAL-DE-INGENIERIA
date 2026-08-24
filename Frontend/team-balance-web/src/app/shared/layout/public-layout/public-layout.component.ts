import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PublicHeader } from '../public-layout/public-header/public-header.component';
import { PublicFooterComponent } from '../public-layout/public-footer/public-footer.component';
import { RouterLink,RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-public-layout',
  imports: [
    RouterOutlet,
    PublicHeader,
    PublicFooterComponent,
    RouterLink,
    RouterLinkActive
  ],
  templateUrl: './public-layout.component.html',
})
export class PublicLayoutComponent {

}

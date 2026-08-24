import { Component } from '@angular/core';
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

}

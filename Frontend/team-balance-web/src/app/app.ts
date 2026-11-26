import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SessionInactivityService } from './services/session-inactivity.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  constructor() {
    inject(SessionInactivityService);
  }
}

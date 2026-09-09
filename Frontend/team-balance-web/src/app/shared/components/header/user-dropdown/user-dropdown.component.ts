import { Component, inject } from '@angular/core';
import { DropdownComponent } from '../../ui/dropdown/dropdown.component';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { DropdownItemTwoComponent } from '../../ui/dropdown/dropdown-item/dropdown-item.component-two';
import { AuthService } from '../../../../services/auth.service';
import { LocalizationService } from '../../../../services/localization.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-user-dropdown',
  templateUrl: './user-dropdown.component.html',
  imports:[CommonModule,RouterModule,DropdownComponent,DropdownItemTwoComponent]
})
export class UserDropdownComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  readonly localization = inject(LocalizationService);
  readonly user = this.authService.usuarioActual();
  isOpen = false;

  toggleDropdown() {
    this.isOpen = !this.isOpen;
  }

  closeDropdown() {
    this.isOpen = false;
  }

  signOut(): void {
    this.authService.cerrarSesion().pipe(finalize(() => {
      this.authService.logOut();
      this.closeDropdown();
      void this.router.navigate(['/signin']);
    })).subscribe({ error: () => undefined });
  }
}

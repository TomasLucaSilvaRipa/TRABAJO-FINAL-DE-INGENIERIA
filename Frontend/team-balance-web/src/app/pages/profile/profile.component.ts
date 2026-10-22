
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { LocalizationService } from '../../services/localization.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './profile.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileComponent {
  private readonly authService = inject(AuthService);
  readonly localization = inject(LocalizationService);
  readonly cargandoFoto = signal(false);
  readonly errorFoto = signal<string | null>(null);
  readonly exitoFoto = signal<string | null>(null);

  get usuario() {
    return this.authService.usuarioActual();
  }

  async seleccionarFoto(evento: Event): Promise<void> {
    const input = evento.target as HTMLInputElement;
    const archivo = input.files?.[0];
    input.value = '';
    this.errorFoto.set(null);
    this.exitoFoto.set(null);

    if (!archivo) { return; }
    if (!['image/png', 'image/jpeg', 'image/webp'].includes(archivo.type)) {
      this.errorFoto.set(this.localization.traducir('profile.photoFormatError'));
      return;
    }

    try {
      this.cargandoFoto.set(true);
      const fotoPerfil = await this.optimizarFoto(archivo);
      await firstValueFrom(this.authService.actualizarFotoPerfil(fotoPerfil));
      this.exitoFoto.set(this.localization.traducir('profile.photoSaved'));
    } catch (error) {
      this.errorFoto.set(error instanceof Error ? error.message : this.localization.traducir('profile.photoSaveError'));
    } finally {
      this.cargandoFoto.set(false);
    }
  }

  private optimizarFoto(archivo: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const imagen = new Image();
      const url = URL.createObjectURL(archivo);
      imagen.onload = () => {
        URL.revokeObjectURL(url);
        const escala = Math.min(1, 512 / Math.max(imagen.naturalWidth, imagen.naturalHeight));
        const lienzo = document.createElement('canvas');
        lienzo.width = Math.max(1, Math.round(imagen.naturalWidth * escala));
        lienzo.height = Math.max(1, Math.round(imagen.naturalHeight * escala));
        const contexto = lienzo.getContext('2d');
        if (!contexto) { reject(new Error(this.localization.traducir('profile.photoSaveError'))); return; }
        contexto.drawImage(imagen, 0, 0, lienzo.width, lienzo.height);
        const fotoPerfil = lienzo.toDataURL('image/webp', 0.78);
        const tamanioBytes = Math.floor((fotoPerfil.length - fotoPerfil.indexOf(',') - 1) * 3 / 4);
        if (tamanioBytes > 512 * 1024) { reject(new Error(this.localization.traducir('profile.photoSizeError'))); return; }
        resolve(fotoPerfil);
      };
      imagen.onerror = () => { URL.revokeObjectURL(url); reject(new Error(this.localization.traducir('profile.photoFormatError'))); };
      imagen.src = url;
    });
  }
}

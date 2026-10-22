import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IdiomaPlataforma, LocalizationService } from '../../../../services/localization.service';
import { IdiomasService } from '../../../../services/idiomas.service';

@Component({
  selector: 'app-language-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './language-management.component.html',
})
export class LanguageManagementComponent {
  readonly localization = inject(LocalizationService);
  private readonly idiomasService = inject(IdiomasService);
  readonly idiomas = signal<IdiomaPlataforma[]>([]);
  readonly idiomaSeleccionadoId = signal<number | null>(null);
  readonly busqueda = signal('');
  readonly codigoNuevo = signal('');
  readonly nombreNuevo = signal('');
  readonly creando = signal(false);
  readonly guardando = signal(false);
  readonly mensaje = signal('');
  readonly error = signal('');
  readonly idiomaSeleccionado = computed(() => this.idiomas().find((idioma) => idioma.id === this.idiomaSeleccionadoId()) ?? null);
  readonly claves = computed(() => {
    const termino = this.busqueda().trim().toLocaleLowerCase();
    return Object.entries(this.localization.catalogoEnIngles())
      .filter(([clave, texto]) => !termino || clave.toLocaleLowerCase().includes(termino) || texto.toLocaleLowerCase().includes(termino))
      .sort(([primera], [segunda]) => primera.localeCompare(segunda));
  });

  constructor() { this.cargar(); }

  cargar(): void {
    this.idiomasService.consultarGestion().subscribe({
      next: (idiomas) => {
        this.idiomas.set(idiomas);
        if (!this.idiomaSeleccionadoId() && idiomas[0]) this.idiomaSeleccionadoId.set(idiomas[0].id);
        this.inicializarIdiomasBase(idiomas);
      },
      error: () => this.error.set(this.localization.traducir('languages.loadError')),
    });
  }

  crear(): void {
    this.error.set(''); this.mensaje.set('');
    const codigo = this.codigoNuevo().trim(); const nombre = this.nombreNuevo().trim();
    if (!codigo || !nombre) { this.error.set(this.localization.traducir('common.required')); return; }
    this.creando.set(true);
    this.idiomasService.registrar({ codigo, nombre, activo: true, traducciones: {} }).subscribe({
      next: (idioma) => { this.idiomas.update((actuales) => [...actuales, idioma]); this.idiomaSeleccionadoId.set(idioma.id); this.codigoNuevo.set(''); this.nombreNuevo.set(''); this.creando.set(false); this.mensaje.set(this.localization.traducir('languages.created')); this.localization.cargarIdiomas(); },
      error: (respuesta) => { this.creando.set(false); this.error.set(typeof respuesta.error === 'string' ? respuesta.error : this.localization.traducir('languages.saveError')); },
    });
  }

  actualizarTraduccion(clave: string, valor: string): void {
    const id = this.idiomaSeleccionadoId();
    if (!id) return;
    this.idiomas.update((idiomas) => idiomas.map((idioma) => idioma.id === id ? { ...idioma, traducciones: { ...idioma.traducciones, [clave]: valor } } : idioma));
  }

  cambiarActivo(activo: boolean): void {
    const id = this.idiomaSeleccionadoId();
    if (!id) return;
    this.idiomas.update((idiomas) => idiomas.map((idioma) => idioma.id === id ? { ...idioma, activo } : idioma));
  }

  guardar(): void {
    const idioma = this.idiomaSeleccionado(); if (!idioma) return;
    this.error.set(''); this.mensaje.set(''); this.guardando.set(true);
    this.idiomasService.guardar(idioma).subscribe({
      next: () => { this.guardando.set(false); this.mensaje.set(this.localization.traducir('languages.saved')); this.localization.cargarIdiomas(); },
      error: (respuesta) => { this.guardando.set(false); this.error.set(typeof respuesta.error === 'string' ? respuesta.error : this.localization.traducir('languages.saveError')); },
    });
  }

  cantidadTraducidas(idioma: IdiomaPlataforma): number { return Object.values(idioma.traducciones).filter((texto) => !!texto?.trim()).length; }

  private inicializarIdiomasBase(idiomas: IdiomaPlataforma[]): void {
    for (const codigo of ['es', 'en'] as const) {
      const idioma = idiomas.find((item) => item.codigo === codigo);
      if (!idioma || Object.keys(idioma.traducciones).length) continue;
      this.idiomasService.guardar({ ...idioma, traducciones: this.localization.catalogoBase(codigo) }).subscribe({
        next: () => this.localization.cargarIdiomas(),
      });
    }
  }
}

import { Component, computed, inject, input, signal } from '@angular/core';
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
  protected readonly busqueda = signal('');
  readonly localization = inject(LocalizationService);
  private readonly paginasBuscables = [
    { ruta: '/', titulo: 'publicSearch.home.title', descripcion: 'publicSearch.home.description', palabras: 'inicio home principal teambalance' },
    { ruta: '/plans', titulo: 'publicSearch.plans.title', descripcion: 'publicSearch.plans.description', palabras: 'planes precios contratar compra suscripcion subscription pricing' },
    { ruta: '/registrar-agencia', titulo: 'publicSearch.registration.title', descripcion: 'publicSearch.registration.description', palabras: 'registro registrar agencia crear contratar alta sign up' },
    { ruta: '/signin', titulo: 'publicSearch.signin.title', descripcion: 'publicSearch.signin.description', palabras: 'iniciar sesion ingresar acceso login sign in' },
    { ruta: '/recuperar-contrasena', titulo: 'publicSearch.recovery.title', descripcion: 'publicSearch.recovery.description', palabras: 'recuperar contraseña password clave acceso' },
    { ruta: '/novedades', titulo: 'publicSearch.news.title', descripcion: 'publicSearch.news.description', palabras: 'novedades noticias newsletter actualizaciones news updates' },
    { ruta: '/ayuda', titulo: 'publicSearch.help.title', descripcion: 'publicSearch.help.description', palabras: 'ayuda faq preguntas frecuentes soporte help' },
    { ruta: '/encuestas', titulo: 'publicSearch.surveys.title', descripcion: 'publicSearch.surveys.description', palabras: 'encuesta encuestas opinion mejorar experiencia necesidades survey feedback' },
    { ruta: '/about-us', titulo: 'publicSearch.about.title', descripcion: 'publicSearch.about.description', palabras: 'nosotros equipo empresa about company' },
    { ruta: '/contact', titulo: 'publicSearch.contact.title', descripcion: 'publicSearch.contact.description', palabras: 'contacto comunicar ventas contact' },
    { ruta: '/terms-and-conditions', titulo: 'publicSearch.terms.title', descripcion: 'publicSearch.terms.description', palabras: 'terminos condiciones legal contract terms' },
    { ruta: '/privacy-policy', titulo: 'publicSearch.privacy.title', descripcion: 'publicSearch.privacy.description', palabras: 'privacidad datos politica privacy' },
    { ruta: '/security-policy', titulo: 'publicSearch.security.title', descripcion: 'publicSearch.security.description', palabras: 'seguridad politica security' }
  ];
  protected readonly resultadosBusqueda = computed(() => {
    this.localization.language();
    const termino = this.normalizar(this.busqueda());
    if (!termino) { return []; }
    return this.paginasBuscables.filter((pagina) => this.normalizar(`${this.localization.traducir(pagina.titulo)} ${this.localization.traducir(pagina.descripcion)} ${pagina.palabras}`).includes(termino)).slice(0, 7);
  });

  protected toggleMenu(): void {
    this.menuOpen.update((isOpen) => !isOpen);
  }

  protected closeMenu(): void {
    this.menuOpen.set(false);
  }

  protected actualizarBusqueda(valor: string): void {
    this.busqueda.set(valor);
  }

  protected cerrarBusqueda(): void {
    this.busqueda.set('');
  }

  protected etiquetaBusqueda(clave: string): string {
    return this.localization.traducir(clave);
  }

  private normalizar(texto: string): string {
    return texto.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase().trim();
  }
}

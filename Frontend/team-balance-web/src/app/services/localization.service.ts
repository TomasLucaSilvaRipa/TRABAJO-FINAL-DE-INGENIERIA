import { Injectable, signal } from '@angular/core';

export type LanguageCode = 'es' | 'en';
export type CultureCode = 'es-AR' | 'en-US';

type TranslationDictionary = Record<string, string>;

@Injectable({
  providedIn: 'root',
})
export class LocalizationService {

  private readonly translations: Record<LanguageCode, TranslationDictionary> = {

    es: {
      'common.save': 'Guardar',
      'common.cancel': 'Cancelar',
      'common.search': 'Buscar',
      'common.close': 'Cerrar',

      'signin': 'Iniciar sesión',
      'view-plans': 'Ver planes',
      'login.title': 'Iniciar sesión',
      'login.email': 'Correo electrónico',
      'login.password': 'Contraseña',
      'login.remember': 'Mantener sesión iniciada',
      'login.button': 'Ingresar',
      'login.forgotPassword': '¿Olvidaste tu contraseña?',

      'menu.home': 'Inicio',
      'menu.dashboard': 'Panel',
      'menu.projects': 'Proyectos',
      'menu.employees': 'Empleados',
      'menu.security': 'Seguridad',
      'menu.logout': 'Cerrar sesión',

      'logs.title': 'Bitácora de actividad y accesos',
      'logs.filters': 'Filtros de búsqueda',
      'logs.date': 'Fecha',
      'logs.agency': 'Agencia',
      'logs.user': 'Usuario',
      'logs.module': 'Módulo',
      'logs.action': 'Acción',
      'logs.result': 'Resultado',
      'logs.criticality': 'Criticidad',
    },

    en: {
      'common.save': 'Save',
      'common.cancel': 'Cancel',
      'common.search': 'Search',
      'common.close': 'Close',

      'signin': 'Sign in',
      'view-plans': 'View plans',
      'login.title': 'Sign in',
      'login.email': 'Email',
      'login.password': 'Password',
      'login.remember': 'Keep me signed in',
      'login.button': 'Sign in',
      'login.forgotPassword': 'Forgot your password?',

      'menu.home': 'Home',
      'menu.dashboard': 'Dashboard',
      'menu.projects': 'Projects',
      'menu.employees': 'Employees',
      'menu.security': 'Security',
      'menu.logout': 'Sign out',

      'logs.title': 'Activity and access log',
      'logs.filters': 'Search filters',
      'logs.date': 'Date',
      'logs.agency': 'Agency',
      'logs.user': 'User',
      'logs.module': 'Module',
      'logs.action': 'Action',
      'logs.result': 'Result',
      'logs.criticality': 'Criticality',
    }
  };

  readonly language = signal<LanguageCode>((localStorage.getItem('language') as LanguageCode) || 'es');

  readonly culture = signal<CultureCode>((localStorage.getItem('culture') as CultureCode) || 'es-AR');

  cambiarIdioma(language: LanguageCode): void {
    this.language.set(language);
    const culture: CultureCode = language === 'es' ? 'es-AR' : 'en-US';
    this.culture.set(culture);
    localStorage.setItem('language', language);
    localStorage.setItem('culture', culture);
  }

  traducir(key: string, language: LanguageCode = this.language()): string {
    return this.translations[language][key] ?? key;
  }

  formatearFecha(fecha: Date | string): string {
    return new Intl.DateTimeFormat( this.culture(), { year: 'numeric', month: '2-digit', day: '2-digit',}).format(new Date(fecha));
  }

  formatearFechaHora(fecha: Date | string): string {
    return new Intl.DateTimeFormat(this.culture(), { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit',}).format(new Date(fecha));
  }

  formatearNumero(numero: number): string {

    return new Intl.NumberFormat(
      this.culture()
    ).format(numero);
  }

  formatearMoneda( monto: number, moneda: string = 'USD'): string {
    return new Intl.NumberFormat( this.culture(), { style: 'currency', currency: moneda,}).format(monto);
  }

}

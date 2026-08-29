import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { RECAPTCHA_SITE_KEY } from '../config/recaptcha.config';

interface RecaptchaGlobal {
  ready(callback: () => void): void;
  execute(siteKey: string, options: { action: string }): Promise<string>;
}

declare global {
  interface Window {
    grecaptcha?: RecaptchaGlobal;
  }
}

@Injectable({
  providedIn: 'root',
})
export class RecaptchaService {
  private readonly document = inject(DOCUMENT);
  private cargaScript?: Promise<void>;

  ejecutarLogin(): Promise<string> {
    return this.ejecutar('login');
  }

  private ejecutar(accion: string): Promise<string> {
    if (!RECAPTCHA_SITE_KEY)
    {
      return Promise.reject(new Error('Configurá la Site Key de reCAPTCHA antes de iniciar sesión.'));
    }

    return this.cargarScript().then(() => new Promise<string>((resolve, reject) => {
      const recaptcha = window.grecaptcha;

      if (!recaptcha)
      {
        reject(new Error('No fue posible cargar la verificación de seguridad.'));
        return;
      }

      recaptcha.ready(() => {
        recaptcha.execute(RECAPTCHA_SITE_KEY, { action: accion }).then(resolve).catch(reject);
      });
    }));
  }

  private cargarScript(): Promise<void> {
    if (window.grecaptcha)
    {
      return Promise.resolve();
    }

    if (this.cargaScript)
    {
      return this.cargaScript;
    }

    this.cargaScript = new Promise<void>((resolve, reject) => {
      const script = this.document.createElement('script');

      script.src = `https://www.google.com/recaptcha/api.js?render=${encodeURIComponent(RECAPTCHA_SITE_KEY)}`;
      script.async = true;
      script.defer = true;
      script.onload = () => resolve();
      script.onerror = () => reject(new Error('No fue posible cargar reCAPTCHA.'));

      this.document.head.appendChild(script);
    });

    return this.cargaScript;
  }
}

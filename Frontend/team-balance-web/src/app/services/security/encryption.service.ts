import { Injectable } from '@angular/core';

export interface EncryptionServiceInterface {
  encryptedData: string;
  encryptedKey: string;
  iv: string;
}

@Injectable({
  providedIn: 'root',
})
export class EncryptionService {
  async encrypt( data: object, publicKeyPem: string): Promise<EncryptionServiceInterface> {

    // 1. Generar una clave AES temporal
    const aesKey = await crypto.subtle.generateKey({ name: 'AES-GCM', length: 256 }, true,['encrypt']);

    // 2. Crear IV aleatorio
    const iv = crypto.getRandomValues(new Uint8Array(12));

    // 3. Convertir los datos del login a bytes
    const encodedData = new TextEncoder().encode( JSON.stringify(data));

    // 4. Cifrar email/password con AES
    const encryptedData = await crypto.subtle.encrypt({name: 'AES-GCM', iv}, aesKey, encodedData);

    // 5. Exportar la clave AES
    const rawAesKey = await crypto.subtle.exportKey('raw', aesKey);

    // 6. Importar la clave pública RSA del backend
    const publicKey = await this.importPublicKey(publicKeyPem);

    // 7. Cifrar la clave AES usando RSA
    const encryptedKey = await crypto.subtle.encrypt({name: 'RSA-OAEP'}, publicKey,rawAesKey);

    return { encryptedData: this.toBase64(encryptedData), encryptedKey: this.toBase64(encryptedKey), iv: this.toBase64(iv.buffer)};
  }

  private async importPublicKey( pem: string): Promise<CryptoKey> {

    const cleanPem = pem.replace('-----BEGIN PUBLIC KEY-----', '').replace('-----END PUBLIC KEY-----', '').replace(/\s/g, '');

    const binaryDer = Uint8Array.from(atob(cleanPem), c => c.charCodeAt(0));

    return crypto.subtle.importKey('spki', binaryDer.buffer, { name: 'RSA-OAEP', hash: 'SHA-256'}, false, ['encrypt']);
  }

  private toBase64(buffer: ArrayBuffer): string {
    return btoa(String.fromCharCode(...new Uint8Array(buffer)));
  }
}

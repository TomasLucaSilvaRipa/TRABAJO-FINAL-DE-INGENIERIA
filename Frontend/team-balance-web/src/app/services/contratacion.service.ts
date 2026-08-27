import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ContratacionResponse {
  urlPago: string;
  referencia: string;
}

export interface ContratacionRequest {
  nombreComercialAgencia: string;
  razonSocial: string;
  cuit: string;
  condicionFiscal: string;
  emailFacturacion: string;
  telefonoContacto: string;
  nombreResponsable: string;
  apellidoResponsable: string;
  emailLaboralResponsable: string;
  cargoResponsable: string;
  proveedorPagoSeleccionado: string;
  periodicidad: 'Mensual' | 'Anual';
}

export interface EstadoContratacionResponse {
  referencia: string;
  estado: string;
  puedeRegistrar: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class ContratacionService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl = '/api/contratacion';

  contratar(data: ContratacionRequest): Observable<ContratacionResponse> {
    return this.http.post<ContratacionResponse>(this.apiUrl, data);
  }

  consultarEstado(referencia: string): Observable<EstadoContratacionResponse> {
    return this.http.get<EstadoContratacionResponse>(`${this.apiUrl}/${referencia}/estado`);
  }

  verificarPago(referencia: string, paymentId: string): Observable<EstadoContratacionResponse> {
    return this.http.post<EstadoContratacionResponse>(
      `${this.apiUrl}/${referencia}/verificar-pago`,
      { paymentId },
    );
  }

}

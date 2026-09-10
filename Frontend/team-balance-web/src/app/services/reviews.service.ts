import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { map } from 'rxjs/operators';

export interface CustomerReview {
  rating: number;
  title: string;
  description: string;
  name: string;
  role: string;
  date: string;
}

interface OpinionApi { calificacion: number; titulo: string; comentario: string; nombreUsuario: string; nombreRol: string; fechaAlta: string; }

@Injectable({ providedIn: 'root' })
export class ReviewsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/opiniones';
  private readonly reviewsSignal = signal<CustomerReview[]>([]);
  readonly reviews = this.reviewsSignal.asReadonly();

  constructor() {
    this.cargar();
  }

  cargar(): void {
    this.http.get<OpinionApi[]>(this.apiUrl).pipe(map((opiniones: OpinionApi[]) => opiniones.map((opinion: OpinionApi) => this.mapear(opinion)))).subscribe({ next: (opiniones: CustomerReview[]) => this.reviewsSignal.set(opiniones), error: () => this.reviewsSignal.set([]) });
  }

  guardar(rating: number, title: string, description: string): Observable<CustomerReview> {
    return this.http.post<OpinionApi>(this.apiUrl, { calificacion: rating, titulo: title, comentario: description }).pipe(map((opinion: OpinionApi) => this.mapear(opinion)), tap((opinion: CustomerReview) => {
      const reviews = this.reviewsSignal().filter((review: CustomerReview) => review.name !== opinion.name);
      this.reviewsSignal.set([opinion, ...reviews]);
    }));
  }

  private mapear(opinion: OpinionApi): CustomerReview {
    return { rating: opinion.calificacion, title: opinion.titulo, description: opinion.comentario, name: opinion.nombreUsuario, role: opinion.nombreRol, date: new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: 'long', year: 'numeric' }).format(new Date(opinion.fechaAlta)) };
  }
}

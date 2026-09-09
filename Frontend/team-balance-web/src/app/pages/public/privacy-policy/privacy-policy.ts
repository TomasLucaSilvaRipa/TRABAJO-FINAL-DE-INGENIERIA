import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { LocalizationService, type LanguageCode } from '../../../services/localization.service';

interface PrivacySection {
  title: string;
  paragraphs: string[];
  items?: string[];
}

interface PrivacyContent {
  eyebrow: string;
  title: string;
  meta: string;
  intro: string;
  sections: PrivacySection[];
  notice: string;
}

const PRIVACY_CONTENT: Record<LanguageCode, PrivacyContent> = {
  es: {
    eyebrow: 'Privacidad',
    title: 'Política de privacidad',
    meta: 'Versión 1.0 · Última actualización: agosto de 2026',
    intro: 'Esta política explica qué información puede tratar TeamBalance, para qué se utiliza y qué controles se contemplan para protegerla.',
    sections: [
      { title: '1. Información que puede tratarse', paragraphs: ['Para prestar el servicio, TeamBalance puede procesar datos de cuentas y organizaciones, como nombre, correo electrónico, rol, datos de acceso y preferencias operativas. También puede procesar la información que cada agencia registra para gestionar sus proyectos, tareas, disponibilidad, skills, seniority, carga de trabajo y clientes.', 'La plataforma está orientada a información operativa. Las organizaciones usuarias deben evitar cargar datos personales sensibles o información que no sea necesaria para el propósito de planificación y gestión.'] },
      { title: '2. Finalidades del tratamiento', paragraphs: [], items: ['Crear y administrar cuentas, roles, sesiones y suscripciones.', 'Calcular disponibilidad, carga, proyecciones y recomendaciones dentro de la agencia.', 'Brindar soporte, responder consultas y mantener la continuidad operativa del servicio.', 'Prevenir accesos no autorizados, investigar incidentes y conservar trazabilidad de eventos relevantes.', 'Mejorar la plataforma a partir de información agregada o desidentificada, cuando corresponda.'] },
      { title: '3. Acceso y separación entre agencias', paragraphs: ['TeamBalance fue concebido con una lógica de aislamiento por agencia. Cada operación debe asociarse a la organización autenticada y los permisos del rol correspondiente, con el objetivo de impedir el acceso de una agencia a información de otra.', 'Dentro de una misma agencia, la persona administradora debe definir quién puede acceder a datos de proyectos, personas, costos u otros indicadores operativos.'] },
      { title: '4. Conservación de la información', paragraphs: ['La información se conserva durante el tiempo necesario para operar la cuenta, brindar soporte, mantener trazabilidad y cumplir las obligaciones aplicables. Los plazos concretos pueden variar según el tipo de dato, el estado de la suscripción, las necesidades de respaldo y la normativa vigente.', 'La arquitectura funcional prevista contempla registros de auditoría, copias de respaldo y mecanismos de archivo para información operativa. Estos controles buscan permitir la recuperación ante incidentes y evitar una conservación mayor a la necesaria.'] },
      { title: '5. Derechos de las personas titulares', paragraphs: ['Las personas titulares pueden solicitar información sobre sus datos y, según corresponda, pedir acceso, actualización, rectificación o supresión. Cuando los datos son cargados por una agencia cliente, la solicitud debe canalizarse primero a través de la organización que administra esa cuenta.', 'TeamBalance deberá habilitar un canal de contacto para estos pedidos y tratarlos de acuerdo con la legislación aplicable en materia de protección de datos personales.'] },
      { title: '6. Cambios en la política', paragraphs: ['Esta política puede actualizarse cuando cambien las funcionalidades, los procesos de tratamiento o las obligaciones aplicables. La versión vigente se mantendrá disponible en este sitio, con su fecha de actualización.'] },
    ],
    notice: 'TeamBalance mantiene esta política disponible para que las organizaciones usuarias comprendan cómo se trata la información dentro del servicio.',
  },
  en: {
    eyebrow: 'Privacy',
    title: 'Privacy policy',
    meta: 'Version 1.0 · Last updated: August 2026',
    intro: 'This policy explains what information TeamBalance may process, why it is used and what controls are considered to protect it.',
    sections: [
      { title: '1. Information that may be processed', paragraphs: ['To provide the service, TeamBalance may process account and organization data such as name, email address, role, access data and operational preferences. It may also process information that each agency records to manage its projects, tasks, availability, skills, seniority, workload and clients.', 'The platform is intended for operational information. Customer organizations should avoid entering sensitive personal data or information that is not necessary for planning and management purposes.'] },
      { title: '2. Processing purposes', paragraphs: [], items: ['Create and manage accounts, roles, sessions and subscriptions.', 'Calculate availability, workload, projections and recommendations within the agency.', 'Provide support, answer inquiries and maintain the operational continuity of the service.', 'Prevent unauthorized access, investigate incidents and preserve traceability of relevant events.', 'Improve the platform using aggregated or de-identified information, where appropriate.'] },
      { title: '3. Access and separation between agencies', paragraphs: ['TeamBalance is designed with an agency-isolation approach. Each operation must be associated with the authenticated organization and the applicable role permissions in order to prevent one agency from accessing another’s information.', 'Within the same agency, the administrator must determine who can access data about projects, people, costs or other operational indicators.'] },
      { title: '4. Information retention', paragraphs: ['Information is retained for the time needed to operate the account, provide support, preserve traceability and comply with applicable obligations. Specific periods may vary according to the type of data, subscription status, backup needs and current regulations.', 'The planned functional architecture includes audit records, backups and archiving mechanisms for operational information. These controls are intended to enable recovery after incidents and avoid retaining information longer than necessary.'] },
      { title: '5. Rights of data subjects', paragraphs: ['Data subjects may request information about their data and, where applicable, request access, updating, correction or deletion. When data is entered by a customer agency, the request should first be directed through the organization that manages that account.', 'TeamBalance must provide a contact channel for these requests and handle them in accordance with applicable personal-data protection law.'] },
      { title: '6. Changes to this policy', paragraphs: ['This policy may be updated when features, processing practices or applicable obligations change. The current version will remain available on this site together with its update date.'] },
    ],
    notice: 'TeamBalance keeps this policy available so customer organizations can understand how information is processed within the service.',
  },
};

@Component({
  selector: 'app-privacy-policy',
  imports: [],
  templateUrl: './privacy-policy.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PrivacyPolicyComponent {
  private readonly localization = inject(LocalizationService);
  readonly content = computed(() => PRIVACY_CONTENT[this.localization.language()]);
}

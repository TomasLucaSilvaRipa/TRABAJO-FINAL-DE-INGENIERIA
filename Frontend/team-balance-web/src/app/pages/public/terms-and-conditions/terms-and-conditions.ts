import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { LocalizationService, type LanguageCode } from '../../../services/localization.service';

interface LegalSection {
  title: string;
  paragraphs: string[];
  items?: string[];
}

interface LegalContent {
  eyebrow: string;
  title: string;
  meta: string;
  intro: string;
  sections: LegalSection[];
  notice: string;
}

const TERMS_CONTENT: Record<LanguageCode, LegalContent> = {
  es: {
    eyebrow: 'Marco de uso',
    title: 'Términos y condiciones',
    meta: 'Versión 1.0 · Última actualización: agosto de 2026',
    intro: 'Estos términos describen el uso previsto de TeamBalance como plataforma de gestión operativa para agencias y consultoras que trabajan por proyectos.',
    sections: [
      { title: '1. Objeto del servicio', paragraphs: ['TeamBalance es una plataforma de software como servicio orientada a centralizar información de proyectos, tareas, disponibilidad, skills, seniority y carga operativa. Su finalidad es asistir la planificación y la asignación de recursos dentro de una organización.', 'Las recomendaciones, proyecciones y alertas que pueda mostrar la plataforma son herramientas de apoyo a la decisión. La validación final de las asignaciones, los plazos y las decisiones laborales corresponde siempre a la organización usuaria.'] },
      { title: '2. Acceso, cuentas y roles', paragraphs: ['El acceso se realiza mediante cuentas personales asociadas a una agencia u organización. Cada persona usuaria debe mantener la confidencialidad de sus credenciales y utilizar la plataforma de acuerdo con el rol que le haya sido asignado.', 'Los perfiles de Dueño, Project Manager y Empleado pueden visualizar y operar información distinta según sus permisos. La persona responsable de la agencia debe administrar las altas, bajas y niveles de acceso de su equipo con criterio de necesidad y mínima exposición.'] },
      { title: '3. Uso responsable y datos cargados', paragraphs: ['La organización usuaria es responsable por la exactitud, legitimidad y actualización de los datos que incorpora a TeamBalance, incluyendo información de personas, proyectos, tareas, disponibilidad y clientes.'], items: ['No deben cargarse datos cuyo tratamiento no esté autorizado por la organización o por la normativa aplicable.', 'No debe utilizarse la plataforma para afectar derechos de terceros, vulnerar confidencialidad o realizar actividades ilícitas.', 'Las decisiones de gestión de personas no deben sustentarse únicamente en una recomendación automatizada.'] },
      { title: '4. Planes, contratación y suscripción', paragraphs: ['Los planes publicados describen la periodicidad, el alcance funcional y el valor de referencia de la suscripción. La contratación se formalizará mediante el flujo específico de TeamBalance y las condiciones comerciales vigentes al momento de la solicitud.', 'La activación, renovación, suspensión o baja de una suscripción estará vinculada al estado de la contratación y a la información registrada para la agencia. Las condiciones de facturación, medios de pago y vigencia se mostrarán de forma clara durante la contratación.'] },
      { title: '5. Disponibilidad y evolución del producto', paragraphs: ['TeamBalance se diseña para brindar acceso continuo y confiable a la información operativa. Sin perjuicio de ello, pueden existir tareas de mantenimiento, actualizaciones, incidentes o dependencias de infraestructura que afecten temporalmente la disponibilidad.', 'La plataforma puede evolucionar sus funcionalidades, interfaces y políticas para mejorar la seguridad, el rendimiento o la experiencia de uso. Cuando un cambio sea relevante, se comunicará por los canales disponibles en la cuenta o en esta sección pública.'] },
      { title: '6. Propiedad intelectual y confidencialidad', paragraphs: ['La marca TeamBalance, su interfaz, documentación, componentes y lógica de producto pertenecen a sus titulares o se utilizan con la autorización correspondiente. La contratación otorga una autorización de uso limitada al período contratado, sin transferir derechos de propiedad intelectual.', 'La información operativa ingresada por una agencia continúa bajo la responsabilidad de esa organización. TeamBalance debe tratarla únicamente en la medida necesaria para prestar, mantener, dar soporte y proteger el servicio.'] },
      { title: '7. Suspensión y finalización', paragraphs: ['El acceso puede limitarse de manera preventiva cuando exista un riesgo de seguridad, un uso contrario a estos términos, una afectación a terceros o una necesidad operativa fundada. Cuando resulte razonable, la medida será comunicada a la persona administradora de la cuenta.', 'Al finalizar la relación de servicio, se aplicarán los criterios de conservación, respaldo y eliminación que correspondan según la política de privacidad, los requerimientos técnicos y las obligaciones aplicables.'] },
    ],
    notice: 'La versión vigente de estos términos se mantiene disponible en esta página. Las condiciones particulares de cada contratación complementan este marco general de uso.',
  },
  en: {
    eyebrow: 'Terms of use',
    title: 'Terms and conditions',
    meta: 'Version 1.0 · Last updated: August 2026',
    intro: 'These terms describe the intended use of TeamBalance as an operational management platform for project-based agencies and consulting firms.',
    sections: [
      { title: '1. Purpose of the service', paragraphs: ['TeamBalance is a software-as-a-service platform designed to centralize information about projects, tasks, availability, skills, seniority and operational workload. Its purpose is to support resource planning and assignment within an organization.', 'Any recommendations, projections and alerts shown by the platform are decision-support tools. Final validation of assignments, deadlines and employment decisions always remains the responsibility of the customer organization.'] },
      { title: '2. Access, accounts and roles', paragraphs: ['Access is provided through personal accounts associated with an agency or organization. Each user must keep their credentials confidential and use the platform according to their assigned role.', 'Owner, Project Manager and Employee profiles may view and operate different information according to their permissions. The agency administrator must manage the creation, removal and access levels of their team based on necessity and minimum exposure.'] },
      { title: '3. Responsible use and uploaded data', paragraphs: ['The customer organization is responsible for the accuracy, legitimacy and currency of the data entered into TeamBalance, including information about people, projects, tasks, availability and clients.'], items: ['Data whose processing is not authorized by the organization or applicable regulations must not be uploaded.', 'The platform must not be used to infringe third-party rights, breach confidentiality or perform unlawful activities.', 'People-management decisions must not rely solely on an automated recommendation.'] },
      { title: '4. Plans, purchase and subscription', paragraphs: ['Published plans describe the billing period, functional scope and reference value of the subscription. Purchase is formalized through the specific TeamBalance flow and the commercial conditions in force at the time of the request.', 'Activation, renewal, suspension or cancellation of a subscription is linked to its contract status and the information recorded for the agency. Billing conditions, payment methods and validity will be clearly shown during checkout.'] },
      { title: '5. Availability and product evolution', paragraphs: ['TeamBalance is designed to provide continuous and reliable access to operational information. However, maintenance, updates, incidents or infrastructure dependencies may temporarily affect availability.', 'The platform may evolve its features, interfaces and policies to improve security, performance or user experience. Relevant changes will be communicated through available account channels or in this public section.'] },
      { title: '6. Intellectual property and confidentiality', paragraphs: ['The TeamBalance brand, interface, documentation, components and product logic belong to their respective owners or are used with proper authorization. A subscription grants a limited right to use the service for the subscribed period; it does not transfer intellectual-property rights.', 'Operational information entered by an agency remains under that organization’s responsibility. TeamBalance may process it only as necessary to provide, maintain, support and protect the service.'] },
      { title: '7. Suspension and termination', paragraphs: ['Access may be preventively limited if there is a security risk, use contrary to these terms, harm to third parties or a substantiated operational need. Where reasonable, the measure will be communicated to the account administrator.', 'When the service relationship ends, the applicable retention, backup and deletion criteria will be applied according to the privacy policy, technical requirements and applicable obligations.'] },
    ],
    notice: 'The current version of these terms remains available on this page. The specific conditions of each purchase supplement this general framework of use.',
  },
};

@Component({
  selector: 'app-terms-and-conditions',
  imports: [],
  templateUrl: './terms-and-conditions.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TermsAndConditions {
  private readonly localization = inject(LocalizationService);
  readonly content = computed(() => TERMS_CONTENT[this.localization.language()]);
}

import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { LocalizationService, type LanguageCode } from '../../../services/localization.service';

interface SecuritySection { title: string; paragraphs: string[]; }
interface SecurityContent { eyebrow: string; title: string; lead: string; sections: SecuritySection[]; notice: string; }

const SECURITY_CONTENT: Record<LanguageCode, SecurityContent> = {
  es: {
    eyebrow: 'Seguridad', title: 'La protección de la información es parte del producto.', lead: 'TeamBalance está diseñado para administrar información operativa de agencias con controles de acceso, trazabilidad y continuidad.',
    sections: [
      { title: '1. Acceso según rol', paragraphs: ['La plataforma contempla autorización basada en roles para diferenciar las acciones y la información disponibles para Dueños, Project Managers y Empleados. Los datos sensibles, como costos e indicadores de rentabilidad, deben permanecer restringidos a los perfiles autorizados.', 'Las cuentas utilizan credenciales individuales y sesiones con vigencia limitada. La arquitectura contempla mecanismos de recuperación de acceso y el registro de intentos de inicio de sesión para facilitar la detección de actividad inusual.'] },
      { title: '2. Protección de datos y comunicaciones', paragraphs: ['El diseño de TeamBalance contempla cifrado de las comunicaciones entre el navegador y el servidor mediante protocolos seguros, junto con mecanismos de protección para datos operativos y personales almacenados. Las contraseñas no deben conservarse ni transmitirse en texto plano.', 'La información de cada agencia se trata de manera separada a través de los identificadores y validaciones aplicables a cada operación, reduciendo el riesgo de acceso cruzado entre organizaciones.'] },
      { title: '3. Auditoría y trazabilidad', paragraphs: ['Las acciones relevantes sobre proyectos, asignaciones, cambios de estado y accesos pueden quedar registradas en una bitácora. Esta trazabilidad facilita el seguimiento operativo, la investigación de incidentes y la revisión de actividades que requieran control.', 'Los eventos de autenticación y seguridad se contemplan como parte de los registros de auditoría, respetando los principios de necesidad, acceso restringido y conservación limitada.'] },
      { title: '4. Continuidad y respaldos', paragraphs: ['La arquitectura prevista incluye monitoreo básico de disponibilidad, copias de respaldo y procedimientos de recuperación ante fallas. Estos mecanismos buscan proteger la continuidad del servicio y permitir la restauración de información ante incidentes técnicos.', 'La seguridad y la disponibilidad requieren una revisión continua. Los controles pueden evolucionar junto con la infraestructura, los riesgos identificados y las necesidades de las organizaciones usuarias.'] },
      { title: '5. Uso compartido de la seguridad', paragraphs: ['La seguridad también depende de las prácticas de cada agencia. Las personas administradoras deben asignar los permisos mínimos necesarios, mantener actualizados los usuarios autorizados y comunicar cualquier acceso sospechoso o incidente que pueda afectar a la cuenta.'] },
    ],
    notice: 'La seguridad se revisa de forma continua para acompañar la evolución de la plataforma, la infraestructura y las necesidades de las organizaciones usuarias.',
  },
  en: {
    eyebrow: 'Security', title: 'Protecting information is part of the product.', lead: 'TeamBalance is designed to manage agency operational information with access controls, traceability and continuity.',
    sections: [
      { title: '1. Role-based access', paragraphs: ['The platform supports role-based authorization to differentiate the actions and information available to Owners, Project Managers and Employees. Sensitive data, such as costs and profitability indicators, must remain restricted to authorized profiles.', 'Accounts use individual credentials and time-limited sessions. The architecture includes access-recovery mechanisms and login-attempt logging to help identify unusual activity.'] },
      { title: '2. Data and communications protection', paragraphs: ['TeamBalance is designed to encrypt communications between the browser and server through secure protocols, together with protection mechanisms for stored operational and personal data. Passwords must not be stored or transmitted as plain text.', 'Each agency’s information is handled separately through identifiers and validations applicable to each operation, reducing the risk of cross-organization access.'] },
      { title: '3. Audit and traceability', paragraphs: ['Relevant actions involving projects, assignments, status changes and access may be recorded in an activity log. This traceability supports operational monitoring, incident investigation and review of activities that require oversight.', 'Authentication and security events are considered part of audit records, following the principles of necessity, restricted access and limited retention.'] },
      { title: '4. Continuity and backups', paragraphs: ['The planned architecture includes basic availability monitoring, backups and recovery procedures for failures. These mechanisms seek to protect service continuity and allow information to be restored after technical incidents.', 'Security and availability require ongoing review. Controls may evolve alongside infrastructure, identified risks and customer-organization needs.'] },
      { title: '5. Shared responsibility for security', paragraphs: ['Security also depends on the practices of each agency. Administrators must assign the minimum permissions needed, keep authorized users up to date and report any suspicious access or incident that could affect the account.'] },
    ],
    notice: 'Security is reviewed continuously to support the evolution of the platform, infrastructure and the needs of customer organizations.',
  },
};

@Component({
  selector: 'app-security-policy',
  imports: [],
  templateUrl: './security-policy.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SecurityPolicyComponent {
  private readonly localization = inject(LocalizationService);
  readonly content = computed(() => SECURITY_CONTENT[this.localization.language()]);
}

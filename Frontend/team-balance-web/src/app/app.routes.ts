import { Routes } from '@angular/router';
import { EcommerceComponent } from './pages/dashboard/ecommerce/ecommerce.component';
import { ProfileComponent } from './pages/profile/profile.component';
import { FormElementsComponent } from './pages/forms/form-elements/form-elements.component';
import { BasicTablesComponent } from './pages/tables/basic-tables/basic-tables.component';
import { BlankComponent } from './pages/blank/blank.component';
import { NotFoundComponent } from './pages/other-page/not-found/not-found.component';
import { AppLayoutComponent } from './shared/layout/app-layout/app-layout.component';
import { InvoicesComponent } from './pages/invoices/invoices.component';
import { LineChartComponent } from './pages/charts/line-chart/line-chart.component';
import { BarChartComponent } from './pages/charts/bar-chart/bar-chart.component';
import { AlertsComponent } from './pages/ui-elements/alerts/alerts.component';
import { AvatarElementComponent } from './pages/ui-elements/avatar-element/avatar-element.component';
import { BadgesComponent } from './pages/ui-elements/badges/badges.component';
import { ButtonsComponent } from './pages/ui-elements/buttons/buttons.component';
import { ImagesComponent } from './pages/ui-elements/images/images.component';
import { VideosComponent } from './pages/ui-elements/videos/videos.component';
import { SignInComponent } from './pages/auth-pages/sign-in/sign-in.component';
import { SignUpComponent } from './pages/auth-pages/sign-up/sign-up.component';
import { CalenderComponent } from './pages/calender/calender.component';
import { PublicLayoutComponent } from './shared/layout/public-layout/public-layout.component';
import { HomeComponent } from './pages/public/home/home';
import { AboutUsComponent } from './pages/public/about us/about-us';
import { ContactComponent } from './pages/public/contact/contact';
import { PrivacyPolicyComponent } from './pages/public/privacy-policy/privacy-policy';
import { SecurityPolicyComponent } from './pages/public/security-policy/security-policy';
import { PlansComponent } from './pages/public/plans/plans';
import { TermsAndConditions } from './pages/public/terms-and-conditions/terms-and-conditions';
import { CheckoutComponent } from './pages/public/checkout/checkout';
import { PaymentResultComponent } from './pages/public/payment-result/payment-result';
import { VerifyAccountComponent } from './pages/auth-pages/verify-account/verify-account.component';
import { ResendValidationComponent } from './pages/auth-pages/resend-validation/resend-validation.component';
import { ForgotPasswordComponent } from './pages/auth-pages/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './pages/auth-pages/reset-password/reset-password.component';
import { ChangePasswordComponent } from './pages/security/change-password/change-password.component';
import { authGuard } from './guards/auth.guard';
import { permissionGuard } from './guards/permission.guard';
import { supportGuard } from './guards/support.guard';
import { PlansManager } from './pages/users/support/plans-manager/plans-manager.component';
import { EmployeeManagement } from './pages/users/owner/employee-management/employee-management.component';
import { ProyectManagement } from './pages/users/owner/proyect-management/proyect-management.component';
import { Reports } from './pages/users/owner/reports/reports.component';
import { AgencyConfiguration } from './pages/users/owner/agency-configuration/agency-configuration.component';
import { Subscription } from './pages/users/owner/subscription-manager/subscription.manager.component';
import { TaskManagerComponent } from './pages/users/pm/task-manager/task-manager.component';
import { BestFit } from './pages/users/pm/task-manager/BestFit/BestFit.component';
import { TeamCalendar } from './pages/users/pm/team-calendar/team-calendar.component';
import { MyTasksComponent } from './pages/users/employee/MyTasks/MyTasks.component';
import { TaskDetailComponent } from './pages/users/employee/TaskDetail/TaskDetail.component';
import { KanbanBoard } from './pages/users/employee/KanbanBoard/KanbanBoard.component';
import { MyWorkload } from './pages/users/employee/MyWorkload/MyWorkload.component';
import { RolManagement } from './pages/users/support/roles-management/roles-management.component';
import { RegisterHoursComponent } from './pages/users/employee/register-hours/register-hours.component';
import { AvailabilityComponent } from './pages/users/employee/availability/availability.component';
import { ImpactSimulationComponent } from './pages/users/pm/impact-simulation/impact-simulation.component';
import { NotificationsComponent } from './pages/global/notifications/notifications.component';
import { HelpComponent } from './pages/global/help/help.component';

export const routes: Routes = [
  {
    path:'',
    component:PublicLayoutComponent,
    children:[
      {
        path: '',
        component: HomeComponent,
        pathMatch: 'full',
        title: 'TeamBalance | Gestión inteligente de equipos',
      },
      {
        path:'contact',
        component: ContactComponent,
        title: 'Contacto | TeamBalance',
      },
      {
        path: 'about-us',
        component: AboutUsComponent,
        title: 'Nosotros | TeamBalance',
      },
      {
        path: 'plans',
        component: PlansComponent,
        title: 'Planes | TeamBalance',
      },
      {
        path: 'checkout',
        component: CheckoutComponent,
        title: 'Contratación | TeamBalance',
      },
      {
        path: 'pago/resultado',
        component: PaymentResultComponent,
        title: 'Estado del pago | TeamBalance',
      },
      {
        path: 'registrar-agencia',
        component: SignUpComponent,
        title: 'Registrar agencia | TeamBalance',
      },
      {
        path: 'privacy-policy',
        component: PrivacyPolicyComponent,
        title: 'Política de privacidad | TeamBalance',
      },
      {
        path: 'security-policy',
        component: SecurityPolicyComponent,
        title: 'Política de seguridad | TeamBalance',
      },
      {
        path: 'terms-and-conditions',
        component: TermsAndConditions,
        title: 'Términos y condiciones | TeamBalance',
      },
      {
        path: 'signin',
        component: SignInComponent,
        title: 'Iniciar sesión | TeamBalance',
      },
      {
        path: 'recuperar-contrasena',
        component: ForgotPasswordComponent,
        title: 'Recuperar contraseña | TeamBalance',
      },
      {
        path: 'restablecer-contrasena',
        component: ResetPasswordComponent,
        title: 'Restablecer contraseña | TeamBalance',
      },
      {
        path: 'validar-cuenta',
        component: VerifyAccountComponent,
        title: 'Validar cuenta | TeamBalance',
      },
      {
        path: 'reenviar-validacion',
        component: ResendValidationComponent,
        title: 'Reenviar validación | TeamBalance',
      },
      {
        path: 'signup',
        redirectTo: 'plans',
        pathMatch: 'full',
      },
    ],
  },
  {
    path:'dashboard',
    component:AppLayoutComponent,
    canActivate: [authGuard],
    children:[
      {
        path: '',
        component: EcommerceComponent,
        pathMatch: 'full',
        title: 'Dashboard | TeamBalance',
      },
      {
        path:'calendar',
        component:CalenderComponent,
        title:'Angular Calender | TeamBalance - Angular Admin Dashboard Template'
      },
      {
        path:'profile',
        component:ProfileComponent,
        canActivate: [permissionGuard],
        data: { permission: 'Perfil' },
        title:'Angular Profile Dashboard | TeamBalance - Angular Admin Dashboard Template'
      },
      {
        path:'seguridad',
        component:ChangePasswordComponent,
        canActivate: [permissionGuard],
        data: { permission: 'SeguridadCuenta' },
        title:'Seguridad de la cuenta | TeamBalance'
      },
      {
        path:'planes',
        component:PlansManager,
        canActivate: [permissionGuard],
        data: { permission: 'GestionarPlanes' },
        title:'Gestión de planes | TeamBalance'
      },
      {
        path: 'bitacora',
        loadComponent: () => import('./pages/users/support/logs/logs.component').then(m => m.Logs),
        canActivate: [permissionGuard, supportGuard],
        data: { permission: 'ConsultarBitacora' },
        title: 'Bitácora | TeamBalance'
      },
      { path: 'empleados', component: EmployeeManagement, canActivate: [permissionGuard], data: { permission: 'GestionarUsuarios' }, title: 'Gestión de empleados | TeamBalance' },
      { path: 'proyectos', component: ProyectManagement, canActivate: [permissionGuard], data: { permission: 'GestionarProyectos' }, title: 'Proyectos | TeamBalance' },
      { path: 'reportes', component: Reports, canActivate: [permissionGuard], data: { permission: 'ConsultarTableroEjecutivo' }, title: 'Reportes | TeamBalance' },
      { path: 'configuracion-agencia', component: AgencyConfiguration, canActivate: [permissionGuard], data: { permission: 'GestionarAgencia' }, title: 'Configuración de agencia | TeamBalance' },
      { path: 'suscripcion', component: Subscription, canActivate: [permissionGuard], data: { permission: 'GestionarSuscripcion' }, title: 'Suscripción | TeamBalance' },
      { path: 'tareas', component: TaskManagerComponent, canActivate: [permissionGuard], data: { permission: 'GestionarTareas' }, title: 'Gestión de tareas | TeamBalance' },
      { path: 'best-fit', component: BestFit, canActivate: [permissionGuard], data: { permission: 'UsarBestFit' }, title: 'Best Fit | TeamBalance' },
      { path: 'calendario-equipo', component: TeamCalendar, canActivate: [permissionGuard], data: { permission: 'VerCalendarioEquipo' }, title: 'Calendario del equipo | TeamBalance' },
      { path: 'simulacion-impacto', component: ImpactSimulationComponent, canActivate: [permissionGuard], data: { permission: 'SimularImpacto' }, title: 'Simulación de impacto | TeamBalance' },
      { path: 'mis-tareas', component: MyTasksComponent, canActivate: [permissionGuard], data: { permission: 'VerDashboard' }, title: 'Mis tareas | TeamBalance' },
      { path: 'detalle-tarea', component: TaskDetailComponent, canActivate: [permissionGuard], data: { permission: 'VerDashboard' }, title: 'Detalle de tarea | TeamBalance' },
      { path: 'kanban', component: KanbanBoard, canActivate: [permissionGuard], data: { permission: 'VerKanban' }, title: 'Tablero Kanban | TeamBalance' },
      { path: 'registrar-horas', component: RegisterHoursComponent, canActivate: [permissionGuard], data: { permission: 'RegistrarHoras' }, title: 'Registrar horas | TeamBalance' },
      { path: 'disponibilidad', component: AvailabilityComponent, canActivate: [permissionGuard], data: { permission: 'GestionarDisponibilidad' }, title: 'Mi disponibilidad | TeamBalance' },
      { path: 'carga-operativa', component: MyWorkload, canActivate: [permissionGuard], data: { permission: 'VerCargaOperativa' }, title: 'Carga operativa | TeamBalance' },
      { path: 'roles', component: RolManagement, canActivate: [permissionGuard], data: { permission: 'GestionarRoles' }, title: 'Gestión de roles | TeamBalance' },
      { path: 'notificaciones', component: NotificationsComponent, canActivate: [permissionGuard], data: { permission: 'ConsultarNotificaciones' }, title: 'Notificaciones | TeamBalance' },
      { path: 'ayuda', component: HelpComponent, canActivate: [permissionGuard], data: { permission: 'ConsultarAyuda' }, title: 'Ayuda | TeamBalance' },
      {
        path:'form-elements',
        component:FormElementsComponent,
        title:'Angular Form Elements Dashboard | TeamBalance - Angular Admin Dashboard Template'
      },
      {
        path:'basic-tables',
        component:BasicTablesComponent,
        title:'Angular Basic Tables Dashboard | TeamBalance - Angular Admin Dashboard Template'
      },
      {
        path:'blank',
        component:BlankComponent,
        title:'Angular Blank Dashboard | TeamBalance - Angular Admin Dashboard Template'
      },
      // support tickets
      {
        path:'invoice',
        component:InvoicesComponent,
        title:'Angular Invoice Details Dashboard | TeamBalance - Angular Admin Dashboard Template'
      },
      {
        path:'line-chart',
        component:LineChartComponent,
        title:'Angular Line Chart Dashboard | TeamBalance - Angular Admin Dashboard Template'
      },
      {
        path:'bar-chart',
        component:BarChartComponent,
        title:'Angular Bar Chart Dashboard | TeamBalance - Angular Admin Dashboard Template'
      },
      {
        path:'alerts',
        component:AlertsComponent,
        title:'Angular Alerts Dashboard | TeamBalance - Angular Admin Dashboard Template'
      },
      {
        path:'avatars',
        component:AvatarElementComponent,
        title:'Angular Avatars Dashboard | TeamBalance - Angular Admin Dashboard Template'
      },
      {
        path:'badge',
        component:BadgesComponent,
        title:'Angular Badges Dashboard | TeamBalance - Angular Admin Dashboard Template'
      },
      {
        path:'buttons',
        component:ButtonsComponent,
        title:'Angular Buttons Dashboard | TeamBalance - Angular Admin Dashboard Template'
      },
      {
        path:'images',
        component:ImagesComponent,
        title:'Angular Images Dashboard | TeamBalance - Angular Admin Dashboard Template'
      },
      {
        path:'videos',
        component:VideosComponent,
        title:'Angular Videos Dashboard | TeamBalance - Angular Admin Dashboard Template'
      },

    ]
  },
  { path: 'logs', redirectTo: 'dashboard/bitacora', pathMatch: 'full' },
  // error pages
  {
    path:'**',
    component:NotFoundComponent,
    title:'Angular NotFound Dashboard | TeamBalance - Angular Admin Dashboard Template'
  },
];

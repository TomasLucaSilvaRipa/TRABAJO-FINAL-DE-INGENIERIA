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
        path: 'signup',
        redirectTo: 'plans',
        pathMatch: 'full',
      },
    ],
  },
  {
    path:'dashboard',
    component:AppLayoutComponent,
    children:[
      {
        path: '',
        component: EcommerceComponent,
        pathMatch: 'full',
        title:
          'Angular Ecommerce Dashboard | TeamBalance - Admin Dashboard Template',
      },
      {
        path:'calendar',
        component:CalenderComponent,
        title:'Angular Calender | TeamBalance - Angular Admin Dashboard Template'
      },
      {
        path:'profile',
        component:ProfileComponent,
        title:'Angular Profile Dashboard | TeamBalance - Angular Admin Dashboard Template'
      },
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
  // error pages
  {
    path:'**',
    component:NotFoundComponent,
    title:'Angular NotFound Dashboard | TeamBalance - Angular Admin Dashboard Template'
  },
];

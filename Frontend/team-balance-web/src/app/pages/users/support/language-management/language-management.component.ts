import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IdiomaPlataforma, LocalizationService } from '../../../../services/localization.service';
import { IdiomasService } from '../../../../services/idiomas.service';

interface SeccionTraduccionesDefinicion { id: string; etiqueta: string; prefijos?: string[]; claves?: string[]; }
interface AreaTraduccionesDefinicion { id: string; etiqueta: string; secciones: SeccionTraduccionesDefinicion[]; }

const areasTraducciones: AreaTraduccionesDefinicion[] = [
  { id: 'publicas', etiqueta: 'languages.group.public', secciones: [
    { id: 'inicio', etiqueta: 'languages.section.publicHome', prefijos: ['about', 'contact', 'publicSearch', 'public.home', 'public.accessibility', 'public.contrast', 'public.title.home'] },
    { id: 'ayuda', etiqueta: 'languages.section.publicHelp', prefijos: ['faq', 'faqCategory'] },
    { id: 'novedades', etiqueta: 'languages.section.publicNews', prefijos: ['news', 'public.news', 'public.title.news', 'private.news'] },
    { id: 'encuestas', etiqueta: 'languages.section.publicSurveys', prefijos: ['surveys'] },
    { id: 'acceso', etiqueta: 'languages.section.publicAccess', prefijos: ['login', 'signup', 'auth', 'security'], claves: ['signin'] },
    { id: 'planes', etiqueta: 'languages.section.publicPlans', prefijos: ['plans', 'checkout'], claves: ['view-plans'] },
    { id: 'terminos', etiqueta: 'menu.terms', prefijos: ['public.terms', 'public.title.terms'] },
    { id: 'privacidad', etiqueta: 'menu.privacy', prefijos: ['public.privacy', 'public.title.privacy'] },
    { id: 'seguridad-publica', etiqueta: 'menu.securityPolicy', prefijos: ['public.security', 'public.title.security'] },
    { id: 'pie', etiqueta: 'languages.section.publicFooter', prefijos: ['footer'] },
  ] },
  { id: 'privadas', etiqueta: 'languages.group.private', secciones: [
    { id: 'panel', etiqueta: 'languages.section.privateDashboard', prefijos: ['dashboard', 'dashboardSearch', 'private.dashboard'] },
    { id: 'proyectos', etiqueta: 'languages.section.privateProjects', prefijos: ['projects', 'private.projects'] },
    { id: 'kanban', etiqueta: 'languages.section.privateKanban', prefijos: ['kanban', 'private.projectKanban', 'private.employeeKanban'] },
    { id: 'tareas', etiqueta: 'languages.section.privateTasks', prefijos: ['tasks'] },
    { id: 'mis-tareas', etiqueta: 'languages.section.privateMyTasks', prefijos: ['myTasks'] },
    { id: 'horas', etiqueta: 'languages.section.privateHours', prefijos: ['hours', 'private.hours'] },
    { id: 'empleados', etiqueta: 'languages.section.privateEmployees', prefijos: ['employees', 'private.employees'] },
    { id: 'disponibilidad', etiqueta: 'languages.section.privateAvailability', prefijos: ['availability', 'private.availability', 'private.workload'] },
    { id: 'agencia', etiqueta: 'languages.section.privateAgency', prefijos: ['agency'] },
    { id: 'suscripcion', etiqueta: 'languages.section.privateSubscription', prefijos: ['subscription', 'private.subscription'] },
    { id: 'roles', etiqueta: 'languages.section.privateRoles', prefijos: ['roles'] },
    { id: 'impacto', etiqueta: 'static.simulateImpact', prefijos: ['private.impactSimulation'] },
    { id: 'skills', etiqueta: 'dashboardSearch.skills.title', prefijos: ['private.skillsCoverage'] },
    { id: 'reportes', etiqueta: 'menu.reports', prefijos: ['private.reports'] },
    { id: 'seguridad', etiqueta: 'security.title', prefijos: ['private.security'] },
    { id: 'slack', etiqueta: 'static.slackLinks', prefijos: ['private.slack'] },
  ] },
  { id: 'gestion', etiqueta: 'languages.group.management', secciones: [
    { id: 'consultas', etiqueta: 'languages.section.managementHelpdesk', prefijos: ['helpdesk'] },
    { id: 'gestion-faq', etiqueta: 'languages.section.managementFaq', prefijos: ['faqManager'] },
    { id: 'gestion-encuestas', etiqueta: 'languages.section.managementSurveys', prefijos: ['surveyManager'] },
    { id: 'operadores', etiqueta: 'languages.section.managementOperators', prefijos: ['operators'] },
    { id: 'gestion-planes', etiqueta: 'languages.section.managementPlans', prefijos: ['planManager', 'platformRevenue'] },
    { id: 'respaldos', etiqueta: 'languages.section.managementBackups', prefijos: ['backup'] },
    { id: 'actividad', etiqueta: 'languages.section.managementLogs', prefijos: ['logs'] },
    { id: 'idiomas', etiqueta: 'languages.section.managementLanguages', prefijos: ['languages'] },
  ] },
  { id: 'navegacion', etiqueta: 'languages.group.navigation', secciones: [
    { id: 'menu-vertical', etiqueta: 'languages.section.navigationSidebar', prefijos: ['menu'] },
    { id: 'cabecera', etiqueta: 'languages.section.navigationHeader', prefijos: ['profile', 'notifications'] },
  ] },
  { id: 'compartidas', etiqueta: 'languages.group.shared', secciones: [
    { id: 'comunes', etiqueta: 'languages.section.sharedCommon', prefijos: ['common'] },
    { id: 'interfaz-general', etiqueta: 'languages.section.sharedInterface', prefijos: ['static', 'legacy'] },
  ] },
];

@Component({
  selector: 'app-language-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './language-management.component.html',
})
export class LanguageManagementComponent {
  readonly localization = inject(LocalizationService);
  private readonly idiomasService = inject(IdiomasService);
  readonly idiomas = signal<IdiomaPlataforma[]>([]);
  readonly idiomaSeleccionadoId = signal<number | null>(null);
  readonly mostrarInactivos = signal(false);
  readonly busqueda = signal('');
  readonly codigoFormulario = signal('');
  readonly nombreFormulario = signal('');
  readonly procesandoDatos = signal(false);
  readonly guardando = signal(false);
  readonly eliminando = signal(false);
  readonly reactivando = signal(false);
  readonly idiomaPendienteBaja = signal<IdiomaPlataforma | null>(null);
  readonly mensaje = signal('');
  readonly error = signal('');
  readonly areasAbiertas = signal<Set<string>>(new Set(['publicas']));
  readonly seccionesAbiertas = signal<Set<string>>(new Set());
  readonly idiomaSeleccionado = computed(() => this.idiomas().find((idioma) => idioma.id === this.idiomaSeleccionadoId()) ?? null);
  readonly modoAlta = computed(() => this.idiomaSeleccionadoId() === null);
  readonly puedeEditarDatos = computed(() => {
    const idioma = this.idiomaSeleccionado();
    return idioma === null || !this.esIdiomaBase(idioma);
  });
  readonly puedeEditarCodigo = computed(() => {
    const idioma = this.idiomaSeleccionado();
    return idioma === null || (!idioma.activo && !this.esIdiomaBase(idioma));
  });
  readonly idiomasVisibles = computed(() => this.idiomas()
    .filter((idioma) => idioma.activo || this.mostrarInactivos())
    .sort((primero, segundo) => Number(segundo.activo) - Number(primero.activo) || primero.nombre.localeCompare(segundo.nombre)));
  readonly catalogoCompleto = computed(() => Object.entries(this.localization.catalogoEnIngles()).sort(([primera], [segunda]) => primera.localeCompare(segunda)));
  readonly claves = computed(() => {
    const termino = this.busqueda().trim().toLocaleLowerCase();
    const traducciones = this.idiomaSeleccionado()?.traducciones ?? {};
    return this.catalogoCompleto().filter(([clave, texto]) => !termino || clave.toLocaleLowerCase().includes(termino) || texto.toLocaleLowerCase().includes(termino) || (traducciones[clave] ?? '').toLocaleLowerCase().includes(termino));
  });
  readonly areasAgrupadas = computed(() => areasTraducciones.map((area) => ({
    ...area,
    secciones: area.secciones.map((seccion) => ({
      ...seccion,
      entradas: this.claves().filter(([clave]) => seccion.claves?.includes(clave) || seccion.prefijos?.some((prefijo) => clave === prefijo || clave.startsWith(`${prefijo}.`))),
    })).filter((seccion) => seccion.entradas.length > 0),
  })).map((area) => ({ ...area, cantidad: area.secciones.reduce((total, seccion) => total + seccion.entradas.length, 0) })).filter((area) => area.cantidad > 0));

  constructor() { this.cargar(); }

  cargar(): void {
    this.idiomasService.consultarGestion().subscribe({
      next: (idiomas) => {
        this.idiomas.set(idiomas);
        const seleccionado = idiomas.find((idioma) => idioma.id === this.idiomaSeleccionadoId());
        if (seleccionado) this.seleccionarIdioma(seleccionado.id);
        else if (this.idiomaSeleccionadoId() !== null) this.seleccionarIdioma(this.idiomasVisibles()[0]?.id ?? null);
        else if (idiomas[0] && this.codigoFormulario() === '' && this.nombreFormulario() === '') this.seleccionarIdioma(this.idiomasVisibles()[0]?.id ?? null);
        this.inicializarIdiomasBase(idiomas);
      },
      error: () => this.error.set(this.localization.traducir('languages.loadError')),
    });
  }

  iniciarAlta(): void {
    this.idiomaSeleccionadoId.set(null);
    this.codigoFormulario.set('');
    this.nombreFormulario.set('');
    this.busqueda.set('');
    this.error.set('');
    this.mensaje.set('');
  }

  seleccionarIdioma(id: number | null): void {
    this.idiomaSeleccionadoId.set(id);
    const idioma = this.idiomas().find((item) => item.id === id);
    this.codigoFormulario.set(idioma?.codigo ?? '');
    this.nombreFormulario.set(idioma?.nombre ?? '');
  }

  cambiarVisibilidadInactivos(mostrar: boolean): void {
    this.mostrarInactivos.set(mostrar);
    const seleccionado = this.idiomaSeleccionado();
    if (!mostrar && seleccionado && !seleccionado.activo) this.seleccionarIdioma(this.idiomasVisibles()[0]?.id ?? null);
  }

  alternarArea(id: string): void { this.alternarConjunto(this.areasAbiertas, id); }
  alternarSeccion(id: string): void { this.alternarConjunto(this.seccionesAbiertas, id); }
  areaAbierta(id: string): boolean { return !!this.busqueda().trim() || this.areasAbiertas().has(id); }
  seccionAbierta(id: string): boolean { return !!this.busqueda().trim() || this.seccionesAbiertas().has(id); }

  guardarDatosIdioma(): void {
    if (this.modoAlta()) this.crear();
    else this.modificarDatos();
  }

  crear(): void {
    this.error.set(''); this.mensaje.set('');
    const codigo = this.codigoFormulario().trim(); const nombre = this.nombreFormulario().trim();
    if (!codigo || !nombre) { this.error.set(this.localization.traducir('common.required')); return; }
    this.procesandoDatos.set(true);
    this.idiomasService.registrar({ codigo, nombre, activo: true, traducciones: {} }).subscribe({
      next: (idioma) => { this.idiomas.update((actuales) => [...actuales, idioma]); this.seleccionarIdioma(idioma.id); this.procesandoDatos.set(false); this.mensaje.set(this.localization.traducir('languages.created')); this.localization.cargarIdiomas(); },
      error: (respuesta) => { this.procesandoDatos.set(false); this.error.set(typeof respuesta.error === 'string' ? respuesta.error : this.localization.traducir('languages.saveError')); },
    });
  }

  modificarDatos(): void {
    const idioma = this.idiomaSeleccionado();
    if (!idioma) return;
    const codigo = this.codigoFormulario().trim();
    const nombre = this.nombreFormulario().trim();
    if (!codigo || !nombre) { this.error.set(this.localization.traducir('common.required')); return; }
    const idiomaModificado: IdiomaPlataforma = { ...idioma, codigo, nombre };
    this.error.set(''); this.mensaje.set(''); this.procesandoDatos.set(true);
    this.idiomasService.guardar(idiomaModificado).subscribe({
      next: () => {
        this.idiomas.update((idiomas) => idiomas.map((item) => item.id === idioma.id ? idiomaModificado : item));
        this.procesandoDatos.set(false);
        this.seleccionarIdioma(idioma.id);
        this.mensaje.set(this.localization.traducir('languages.modified'));
        this.localization.cargarIdiomas();
      },
      error: (respuesta) => { this.procesandoDatos.set(false); this.error.set(typeof respuesta.error === 'string' ? respuesta.error : this.localization.traducir('languages.saveError')); },
    });
  }

  actualizarTraduccion(clave: string, valor: string): void {
    const id = this.idiomaSeleccionadoId();
    if (!id) return;
    this.idiomas.update((idiomas) => idiomas.map((idioma) => idioma.id === id ? { ...idioma, traducciones: { ...idioma.traducciones, [clave]: valor } } : idioma));
  }

  guardar(): void {
    const idioma = this.idiomaSeleccionado(); if (!idioma) return;
    this.error.set(''); this.mensaje.set(''); this.guardando.set(true);
    this.idiomasService.guardar(idioma).subscribe({
      next: () => { this.guardando.set(false); this.mensaje.set(this.localization.traducir('languages.saved')); this.localization.cargarIdiomas(); },
      error: (respuesta) => { this.guardando.set(false); this.error.set(typeof respuesta.error === 'string' ? respuesta.error : this.localization.traducir('languages.saveError')); },
    });
  }

  solicitarBaja(idioma: IdiomaPlataforma): void {
    if (!idioma.activo || this.esIdiomaBase(idioma)) return;
    this.error.set('');
    this.mensaje.set('');
    this.idiomaPendienteBaja.set(idioma);
  }

  cancelarBaja(): void {
    if (this.eliminando()) return;
    this.idiomaPendienteBaja.set(null);
  }

  confirmarBaja(): void {
    const idioma = this.idiomaPendienteBaja();
    if (!idioma || this.eliminando()) return;
    this.eliminando.set(true);
    this.idiomasService.darDeBaja(idioma).subscribe({
      next: () => {
        this.idiomas.update((idiomas) => idiomas.map((item) => item.id === idioma.id ? { ...item, activo: false } : item));
        this.mostrarInactivos.set(true);
        this.eliminando.set(false);
        this.idiomaPendienteBaja.set(null);
        this.seleccionarIdioma(idioma.id);
        this.mensaje.set(this.localization.traducir('languages.deactivated'));
        this.localization.cargarIdiomas();
      },
      error: (respuesta) => {
        this.eliminando.set(false);
        this.error.set(typeof respuesta.error === 'string' ? respuesta.error : this.localization.traducir('languages.deactivateError'));
      },
    });
  }

  reactivar(idioma: IdiomaPlataforma): void {
    if (idioma.activo || this.reactivando()) return;
    const idiomaReactivado: IdiomaPlataforma = { ...idioma, activo: true };
    this.error.set(''); this.mensaje.set(''); this.reactivando.set(true);
    this.idiomasService.guardar(idiomaReactivado).subscribe({
      next: () => {
        this.idiomas.update((idiomas) => idiomas.map((item) => item.id === idioma.id ? idiomaReactivado : item));
        this.reactivando.set(false);
        this.seleccionarIdioma(idioma.id);
        this.mensaje.set(this.localization.traducir('languages.reactivated'));
        this.localization.cargarIdiomas();
      },
      error: (respuesta) => { this.reactivando.set(false); this.error.set(typeof respuesta.error === 'string' ? respuesta.error : this.localization.traducir('languages.reactivateError')); },
    });
  }

  esIdiomaBase(idioma: IdiomaPlataforma): boolean { return idioma.codigo === 'es' || idioma.codigo === 'en' || idioma.codigo === 'fr'; }

  cantidadTraducidas(idioma: IdiomaPlataforma): number { return Object.values(idioma.traducciones).filter((texto) => !!texto?.trim()).length; }

  private alternarConjunto(conjunto: { (): Set<string>; set(valor: Set<string>): void }, id: string): void {
    const actualizado = new Set(conjunto());
    if (actualizado.has(id)) actualizado.delete(id); else actualizado.add(id);
    conjunto.set(actualizado);
  }

  private inicializarIdiomasBase(idiomas: IdiomaPlataforma[]): void {
    for (const codigo of ['es', 'en'] as const) {
      const idioma = idiomas.find((item) => item.codigo === codigo);
      if (!idioma || Object.keys(idioma.traducciones).length) continue;
      this.idiomasService.guardar({ ...idioma, traducciones: this.localization.catalogoBase(codigo) }).subscribe({
        next: () => this.localization.cargarIdiomas(),
      });
    }
  }
}

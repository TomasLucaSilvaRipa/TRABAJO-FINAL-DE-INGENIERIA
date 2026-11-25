import { Component, computed, HostListener, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Cliente, FiltroProyecto, ProjectsService, Proyecto, ProyectoOpciones } from '../../../../services/projects.service';
import { LocalizationService } from '../../../../services/localization.service';
import { finalize, forkJoin } from 'rxjs';
import { AnaliticaOperativaService } from '../../../../services/analitica-operativa.service';
import { AuthService } from '../../../../services/auth.service';
import { TasksService } from '../../../../services/tasks.service';

@Component({
  selector: 'app-proyect-management',
  imports: [ReactiveFormsModule],
  templateUrl: './proyect-management.component.html',
  styleUrl: './proyect-management.component.css',
})
export class ProyectManagement {
  private readonly service = inject(ProjectsService);
  private readonly fb = inject(FormBuilder);
  readonly localization = inject(LocalizationService);
  readonly proyectos = signal<Proyecto[]>([]);
  private readonly auth = inject(AuthService);
  private readonly analitica = inject(AnaliticaOperativaService);
  private readonly tareas = inject(TasksService);
  private readonly proyectosConActividad = signal<Set<number> | null>(null);
  readonly revisandoActividad = signal(false);
  readonly errorActividad = signal(false);
  readonly puedeRevisarActividad = computed(() =>
    this.auth.tienePermiso('ConsultarTableroEjecutivo') &&
    (this.auth.usuarioActual()?.roles.some(rol => rol.tipoUsuario === 'Dueno' || rol.tipoUsuario === 'PM') ?? false)
  );
  readonly planificadosConActividad = computed(() => {
    const idsConActividad = this.proyectosConActividad();
    return idsConActividad === null
      ? null
      : this.proyectos().filter(proyecto =>
          proyecto.activo && proyecto.estado === 'Planificado' && idsConActividad.has(proyecto.id)
        );
  });
  readonly opciones = signal<ProyectoOpciones>({ clientes: [], responsables: [] });
  readonly mostrarFormulario = signal(false);
  readonly mostrarCliente = signal(false);
  readonly clienteEditando = signal<Cliente | null>(null);
  readonly proyectoSeleccionado = signal<Proyecto | null>(null);
  readonly menuClienteAbierto = signal<number | null>(null);
  readonly error = signal('');
  readonly errorProyecto = signal('');
  readonly errorCliente = signal('');
  readonly guardandoProyecto = signal(false);
  readonly guardandoCliente = signal(false);
  readonly clienteActualizando = signal<number | null>(null);
  private proyectoEditando: Proyecto | null = null;
  readonly form = this.fb.nonNullable.group({ id: [0], idCliente: [0, [Validators.required, Validators.min(1)]], idPMResponsable: [0, [Validators.required, Validators.min(1)]], nombre: ['', [Validators.required, Validators.pattern(/\S/), Validators.maxLength(200)]], descripcion: [''], fechaInicio: [''], deadline: [''], horasEstimadasTotales: [0, Validators.min(0)], estado: ['Planificado'] });
  readonly clienteForm = this.fb.nonNullable.group({ nombre: ['', [Validators.required, Validators.pattern(/\S/), Validators.maxLength(150)]], razonSocial: ['', Validators.maxLength(200)], email: ['', [Validators.email, Validators.maxLength(200)]], telefono: ['', Validators.maxLength(50)] });
  readonly filtroForm = this.fb.group({ idCliente: [null as number | null], idPMResponsable: [null as number | null], estado: [''], fechaDesde: [''], fechaHasta: [''] });

  constructor() { this.cargar(); }

  revisarActividadProyectos(): void {
    if (!this.puedeRevisarActividad() || this.revisandoActividad()) { return; }
    this.revisandoActividad.set(true);
    this.errorActividad.set(false);
    this.proyectosConActividad.set(null);

    forkJoin({
      horas: this.analitica.consultarDesvioHoras(),
      tareas: this.tareas.consultarTableroProyectos({}),
    }).pipe(finalize(() => this.revisandoActividad.set(false))).subscribe({
      next: ({ horas, tareas }) => {
        const ids = new Set<number>();
        for (const proyecto of horas.proyectos) {
          if (proyecto.horasReales > 0) { ids.add(proyecto.idProyecto); }
        }
        for (const tarea of tareas) {
          if (tarea.activo && tarea.porcentajeAvance > 0) { ids.add(tarea.idProyecto); }
        }
        this.proyectosConActividad.set(ids);
      },
      error: () => this.errorActividad.set(true),
    });
  }

  cargar(): void {
    this.service.consultar().subscribe({ next: (proyectos: Proyecto[]) => this.proyectos.set(proyectos), error: () => this.error.set('projectForm.loadProjects') });
    this.service.opciones().subscribe({ next: (opciones: ProyectoOpciones) => this.opciones.set(opciones), error: () => this.error.set('projectForm.loadOptions') });
  }
  nuevo(): void {
    this.proyectoEditando = null;
    this.form.reset({ fechaInicio: this.hoy() });
    this.error.set('');
    this.errorProyecto.set('');
    this.mostrarFormulario.set(true);
  }

  editar(p: Proyecto): void {
    this.proyectoEditando = p;
    this.form.reset({
      ...p, descripcion: p.descripcion ?? '', fechaInicio: p.fechaInicio?.slice(0, 10) ?? '', deadline: p.deadline?.slice(0, 10) ?? '',
    });
    this.errorProyecto.set('');
    this.mostrarFormulario.set(true);
  }

  fechaMinimaInicio(): string | null { return this.proyectoEditando ? null : this.hoy(); }
  fechaMinimaLimite(): string | null { return this.form.controls.fechaInicio.value || this.fechaMinimaInicio(); }

  guardar(): void {
    if (this.guardandoProyecto()) return;
    this.errorProyecto.set('');
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const datos = this.form.getRawValue();
    const inicioMinimo = this.fechaMinimaInicio();
    if (inicioMinimo && datos.fechaInicio && datos.fechaInicio < inicioMinimo) {
      this.errorProyecto.set('projectForm.startInPast');
      return;
    }
    const limiteMinimo = this.fechaMinimaLimite();
    const original = this.proyectoEditando;
    const fechasSinCambios = original && datos.fechaInicio === (original.fechaInicio?.slice(0, 10) ?? '')
      && datos.deadline === (original.deadline?.slice(0, 10) ?? '');
    if (datos.deadline && limiteMinimo && datos.deadline < limiteMinimo && !fechasSinCambios) {
      this.errorProyecto.set(datos.fechaInicio ? 'projectForm.deadlineBeforeStart' : 'projectForm.deadlineInPast');
      return;
    }

    const fechaInicio = datos.fechaInicio === original?.fechaInicio?.slice(0, 10) ? original?.fechaInicio : datos.fechaInicio || null;
    const deadline = datos.deadline === original?.deadline?.slice(0, 10) ? original?.deadline : datos.deadline || null;
    this.guardandoProyecto.set(true);
    this.service.guardar({ ...datos, nombre: datos.nombre.trim(), fechaInicio, deadline, horasEstimadasTotales: datos.horasEstimadasTotales ?? 0 })
      .pipe(finalize(() => this.guardandoProyecto.set(false))).subscribe({
        next: () => { this.mostrarFormulario.set(false); this.cargar(); },
        error: respuesta => this.errorProyecto.set(this.claveError(respuesta, 'projectForm.saveProject')),
      });
  }

  abrirCliente(cliente: Cliente | null = null): void {
    this.clienteEditando.set(cliente);
    this.errorCliente.set('');
    this.clienteForm.reset({ nombre: cliente?.nombre ?? '', razonSocial: cliente?.razonSocial ?? '', email: cliente?.email ?? '', telefono: cliente?.telefono ?? '' });
    this.mostrarCliente.set(true);
  }

  guardarCliente(): void {
    if (this.guardandoCliente()) return;
    this.errorCliente.set('');
    this.clienteForm.markAllAsTouched();
    if (this.clienteForm.invalid) return;
    const valores = this.clienteForm.getRawValue();
    const datos = { nombre: valores.nombre.trim(), razonSocial: valores.razonSocial.trim(), email: valores.email.trim(), telefono: valores.telefono.trim() };
    const cliente = this.clienteEditando();
    this.guardandoCliente.set(true);
    const solicitud = cliente ? this.service.modificarCliente({ id: cliente.id, activo: cliente.activo, ...datos }) : this.service.crearCliente(datos);
    solicitud.pipe(finalize(() => this.guardandoCliente.set(false))).subscribe({
      next: c => {
        this.mostrarCliente.set(false);
        this.service.opciones().subscribe({
          next: opciones => { this.opciones.set(opciones); if (!cliente) this.form.controls.idCliente.setValue(c.id); },
          error: () => this.error.set('projectForm.loadOptions'),
        });
      },
      error: respuesta => this.errorCliente.set(this.claveError(respuesta, 'projectForm.saveClient')),
    });
  }

  cambiarEstadoCliente(cliente: Cliente): void {
    if (this.clienteActualizando() !== null) return;
    this.error.set('');
    this.clienteActualizando.set(cliente.id);
    this.service.cambiarEstadoCliente({ id: cliente.id, activo: !cliente.activo })
      .pipe(finalize(() => this.clienteActualizando.set(null))).subscribe({
        next: ok => { if (ok) this.cargar(); else this.error.set('projectForm.clientNotFound'); },
        error: respuesta => this.error.set(this.claveError(respuesta, 'projectForm.clientStatus')),
      });
  }
  aplicarFiltro(): void {
    this.error.set('');
    const valores = this.filtroForm.getRawValue();
    if (valores.fechaDesde && valores.fechaHasta && valores.fechaHasta < valores.fechaDesde) {
      this.error.set('projectForm.filterDates');
      return;
    }
    const filtro: FiltroProyecto = { idCliente: valores.idCliente, idPMResponsable: valores.idPMResponsable, estado: valores.estado || null, fechaDesde: valores.fechaDesde || null, fechaHasta: valores.fechaHasta || null };
    this.service.filtrar(filtro).subscribe({ next: (proyectos: Proyecto[]) => this.proyectos.set(proyectos), error: respuesta => this.error.set(this.claveError(respuesta, 'projectForm.filterProjects')) });
  }

  limpiarFiltro(): void { this.error.set(''); this.filtroForm.reset({ idCliente: null, idPMResponsable: null, estado: '', fechaDesde: '', fechaHasta: '' }); this.cargar(); }

  verDetalle(proyecto: Proyecto): void {
    this.service.consultarDetalle(proyecto).subscribe({ next: (detalle: Proyecto) => this.proyectoSeleccionado.set(detalle), error: respuesta => this.error.set(this.claveError(respuesta, 'projectForm.loadDetail')) });
  }

  cerrarProyecto(proyecto: Proyecto): void {
    this.service.consultarDetalle(proyecto).subscribe({
      next: (detalle: Proyecto) => {
        const tareasPendientes = (detalle.tareas ?? []).filter(tarea => tarea.activo && tarea.estado !== 'Finalizada');
        const mensaje = this.localization.traducir(tareasPendientes.length ? 'projectForm.confirmClosePending' : 'projectForm.confirmClose').replace('{count}', String(tareasPendientes.length));
        if (!window.confirm(mensaje)) { return; }
        this.service.cerrar(detalle).subscribe({ next: () => { this.proyectoSeleccionado.set(null); this.cargar(); }, error: respuesta => this.error.set(this.claveError(respuesta, 'projectForm.closeProject')) });
      },
      error: respuesta => this.error.set(this.claveError(respuesta, 'projectForm.checkTasks')),
    });
  }

  cambiar(p: Proyecto): void { this.service.cambiarEstado({ ...p, activo: !p.activo }).subscribe({ next: () => this.cargar(), error: respuesta => this.error.set(this.claveError(respuesta, 'projectForm.projectStatus')) }); }

  private hoy(): string {
    const fecha = new Date();
    return `${fecha.getFullYear()}-${String(fecha.getMonth() + 1).padStart(2, '0')}-${String(fecha.getDate()).padStart(2, '0')}`;
  }

  private claveError(respuesta: { status?: number; error?: unknown }, alternativa: string): string {
    if (respuesta.status === 401) return 'projectForm.invalidSession';
    if (respuesta.status === 403) return 'projectForm.notAllowed';
    if (respuesta.status === 0) return 'projectForm.connection';
    if (typeof respuesta.error === 'string') {
      const texto = respuesta.error;
      if (/no se encontr[oó] el cliente/i.test(texto)) return 'projectForm.clientNotFound';
      if (/PM activo|responsable seleccionado/i.test(texto)) return 'projectForm.invalidManager';
      if (/s[oó]lo puede realizarla el due[ñn]o|no ten[eé]s permiso|no ten[eé]s acceso/i.test(texto)) return 'projectForm.notAllowed';
    }
    if (respuesta.error && typeof respuesta.error === 'object') {
      const campos = Object.keys((respuesta.error as { errors?: Record<string, unknown> }).errors ?? {});
      if (campos.some(campo => /(^|\.)(fechaInicio|deadline)$/i.test(campo))) return 'projectForm.invalidDates';
      if (campos.some(campo => /(^|\.)horasEstimadasTotales$/i.test(campo))) return 'projectForm.invalidHours';
      if (campos.some(campo => /(^|\.)email$/i.test(campo))) return 'projectForm.invalidEmail';
    }
    return alternativa;
  }

  alternarMenuCliente(idCliente: number, event: MouseEvent): void {
    event.stopPropagation();
    this.menuClienteAbierto.update((actual) => actual === idCliente ? null : idCliente);
  }

  cerrarMenuCliente(): void { this.menuClienteAbierto.set(null); }

  @HostListener('document:click')
  alHacerClickFuera(): void { this.cerrarMenuCliente(); }
}

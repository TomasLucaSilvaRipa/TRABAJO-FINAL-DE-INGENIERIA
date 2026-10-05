import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Cliente, FiltroProyecto, ProjectsService, Proyecto, ProyectoOpciones } from '../../../../services/projects.service';
import { LocalizationService } from '../../../../services/localization.service';

@Component({
  selector: 'app-proyect-management',
  imports: [ReactiveFormsModule],
  templateUrl: './proyect-management.component.html',
})
export class ProyectManagement {
  private readonly service = inject(ProjectsService);
  private readonly fb = inject(FormBuilder);
  readonly localization = inject(LocalizationService);
  readonly proyectos = signal<Proyecto[]>([]);
  readonly opciones = signal<ProyectoOpciones>({ clientes: [], responsables: [] });
  readonly mostrarFormulario = signal(false);
  readonly mostrarCliente = signal(false);
  readonly clienteEditando = signal<Cliente | null>(null);
  readonly proyectoSeleccionado = signal<Proyecto | null>(null);
  readonly error = signal('');
  readonly form = this.fb.group({ id: [0], idCliente: [0, Validators.min(1)], idPMResponsable: [0, Validators.min(1)], nombre: ['', Validators.required], descripcion: [''], fechaInicio: [''], deadline: [''], horasEstimadasTotales: [0, Validators.min(0)], estado: ['Planificado'] });
  readonly clienteForm = this.fb.group({ nombre: ['', Validators.required], razonSocial: [''], email: [''], telefono: [''] });
  readonly filtroForm = this.fb.group({ idCliente: [null as number | null], idPMResponsable: [null as number | null], estado: [''], fechaDesde: [''], fechaHasta: [''] });

  constructor() { this.cargar(); }

  cargar(): void {
    this.service.consultar().subscribe({ next: (proyectos: Proyecto[]) => this.proyectos.set(proyectos), error: () => this.error.set('No se pudieron cargar los proyectos.') });
    this.service.opciones().subscribe({ next: (opciones: ProyectoOpciones) => this.opciones.set(opciones), error: () => this.error.set('No se pudieron cargar las opciones.') });
  }
  nuevo(): void { this.form.reset({ id: 0, idCliente: 0, idPMResponsable: 0, nombre: '', descripcion: '', fechaInicio: '', deadline: '', horasEstimadasTotales: 0, estado: 'Planificado' }); this.error.set(''); this.mostrarFormulario.set(true); }
  editar(p: Proyecto): void { this.form.patchValue(p); this.mostrarFormulario.set(true); }
  guardar(): void { if (this.form.invalid) { this.error.set('Completá los campos obligatorios.'); return; } this.service.guardar(this.form.getRawValue() as unknown as Partial<Proyecto>).subscribe({ next: () => { this.mostrarFormulario.set(false); this.cargar(); }, error: () => this.error.set('No se pudo guardar el proyecto.') }); }
  abrirCliente(cliente: Cliente | null = null): void { this.clienteEditando.set(cliente); this.clienteForm.reset(cliente ?? { nombre: '', razonSocial: '', email: '', telefono: '' }); this.mostrarCliente.set(true); }
  guardarCliente(): void { if (this.clienteForm.invalid) { return; } const datos = this.clienteForm.getRawValue() as unknown as Partial<Cliente>; const cliente = this.clienteEditando(); const solicitud = cliente ? this.service.modificarCliente({ ...cliente, ...datos }) : this.service.crearCliente(datos); solicitud.subscribe({ next: (c) => { this.mostrarCliente.set(false); this.clienteForm.reset(); this.service.opciones().subscribe((x) => { this.opciones.set(x); this.form.controls.idCliente.setValue(c.id); }); }, error: () => this.error.set('No se pudo guardar el cliente.') }); }
  cambiarEstadoCliente(cliente: Cliente): void { this.service.cambiarEstadoCliente({ ...cliente, activo: !cliente.activo }).subscribe({ next: () => this.cargar(), error: () => this.error.set('No se pudo actualizar el cliente.') }); }
  aplicarFiltro(): void {
    const valores = this.filtroForm.getRawValue();
    const filtro: FiltroProyecto = { idCliente: valores.idCliente, idPMResponsable: valores.idPMResponsable, estado: valores.estado || null, fechaDesde: valores.fechaDesde || null, fechaHasta: valores.fechaHasta || null };
    this.service.filtrar(filtro).subscribe({ next: (proyectos: Proyecto[]) => this.proyectos.set(proyectos), error: () => this.error.set('No se pudo aplicar el filtro de proyectos.') });
  }

  limpiarFiltro(): void { this.filtroForm.reset({ idCliente: null, idPMResponsable: null, estado: '', fechaDesde: '', fechaHasta: '' }); this.cargar(); }

  verDetalle(proyecto: Proyecto): void {
    this.service.consultarDetalle(proyecto).subscribe({ next: (detalle: Proyecto) => this.proyectoSeleccionado.set(detalle), error: () => this.error.set('No se pudo consultar el detalle del proyecto.') });
  }

  cerrarProyecto(proyecto: Proyecto): void {
    this.service.consultarDetalle(proyecto).subscribe({
      next: (detalle: Proyecto) => {
        const tareasPendientes = (detalle.tareas ?? []).filter(tarea => tarea.activo && tarea.estado !== 'Finalizada');
        const mensaje = tareasPendientes.length ? `El proyecto tiene ${tareasPendientes.length} tarea(s) abierta(s) o bloqueada(s). Se conservarán para el historial. ¿Confirmás el cierre?` : 'No hay tareas pendientes. ¿Confirmás el cierre del proyecto?';
        if (!window.confirm(mensaje)) { return; }
        this.service.cerrar(detalle).subscribe({ next: () => { this.proyectoSeleccionado.set(null); this.cargar(); }, error: () => this.error.set('No se pudo cerrar el proyecto.') });
      },
      error: () => this.error.set('No se pudo verificar el estado de las tareas del proyecto.')
    });
  }

  cambiar(p: Proyecto): void { this.service.cambiarEstado({ ...p, activo: !p.activo }).subscribe({ next: () => this.cargar(), error: () => this.error.set('No se pudo actualizar el estado del proyecto.') }); }
}

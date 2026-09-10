import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Cliente, ProjectsService, Proyecto, ProyectoOpciones } from '../../../../services/projects.service';

@Component({
  selector: 'app-proyect-management',
  imports: [ReactiveFormsModule],
  templateUrl: './proyect-management.component.html',
})
export class ProyectManagement {
  private readonly service = inject(ProjectsService); private readonly fb = inject(FormBuilder);
  readonly proyectos = signal<Proyecto[]>([]); readonly opciones = signal<ProyectoOpciones>({ clientes: [], responsables: [] }); readonly mostrarFormulario = signal(false); readonly mostrarCliente = signal(false); readonly clienteEditando = signal<Cliente | null>(null); readonly error = signal('');
  readonly form = this.fb.group({ id: [0], idCliente: [0, Validators.min(1)], idPMResponsable: [0, Validators.min(1)], nombre: ['', Validators.required], descripcion: [''], fechaInicio: [''], deadline: [''], horasEstimadasTotales: [0, Validators.min(0)], estado: ['Planificado'] });
  readonly clienteForm = this.fb.group({ nombre: ['', Validators.required], razonSocial: [''], email: [''], telefono: [''] });
  constructor() { this.cargar(); }
  cargar(): void { this.service.consultar().subscribe((x: Proyecto[]) => this.proyectos.set(x)); this.service.opciones().subscribe((x: ProyectoOpciones) => this.opciones.set(x)); }
  nuevo(): void { this.form.reset({ id: 0, idCliente: 0, idPMResponsable: 0, nombre: '', descripcion: '', fechaInicio: '', deadline: '', horasEstimadasTotales: 0, estado: 'Planificado' }); this.error.set(''); this.mostrarFormulario.set(true); }
  editar(p: Proyecto): void { this.form.patchValue(p); this.mostrarFormulario.set(true); }
  guardar(): void { if (this.form.invalid) { this.error.set('Completá los campos obligatorios.'); return; } this.service.guardar(this.form.getRawValue() as unknown as Partial<Proyecto>).subscribe({ next: () => { this.mostrarFormulario.set(false); this.cargar(); }, error: () => this.error.set('No se pudo guardar el proyecto.') }); }
  abrirCliente(cliente: Cliente | null = null): void { this.clienteEditando.set(cliente); this.clienteForm.reset(cliente ?? { nombre: '', razonSocial: '', email: '', telefono: '' }); this.mostrarCliente.set(true); }
  guardarCliente(): void { if (this.clienteForm.invalid) { return; } const datos = this.clienteForm.getRawValue() as unknown as Partial<Cliente>; const cliente = this.clienteEditando(); const solicitud = cliente ? this.service.modificarCliente({ ...cliente, ...datos }) : this.service.crearCliente(datos); solicitud.subscribe({ next: (c) => { this.mostrarCliente.set(false); this.clienteForm.reset(); this.service.opciones().subscribe((x) => { this.opciones.set(x); this.form.controls.idCliente.setValue(c.id); }); }, error: () => this.error.set('No se pudo guardar el cliente.') }); }
  cambiarEstadoCliente(cliente: Cliente): void { this.service.cambiarEstadoCliente(cliente.id, !cliente.activo).subscribe({ next: () => this.cargar(), error: () => this.error.set('No se pudo actualizar el cliente.') }); }
  cambiar(p: Proyecto): void { this.service.cambiarEstado(p.id, !p.activo).subscribe(() => this.cargar()); }
}

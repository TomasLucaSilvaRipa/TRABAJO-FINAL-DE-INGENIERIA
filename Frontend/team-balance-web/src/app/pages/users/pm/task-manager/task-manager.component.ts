import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Skill } from '../../../../services/resources.service';
import { FiltroTarea, Tarea, TareaOpciones, TasksService } from '../../../../services/tasks.service';
import { LocalizationService } from '../../../../services/localization.service';
import { BestFitService, RecomendacionBestFit } from '../../../../services/best-fit.service';
import { PlantillaTarea, TaskTemplatesService } from '../../../../services/task-templates.service';

interface ChecklistItem {
    texto: string;
    completada: boolean;
}

@Component({ selector: 'app-task-manager.component', imports: [ReactiveFormsModule], templateUrl: './task-manager.component.html' })
export class TaskManagerComponent {
  private readonly service = inject(TasksService);
    private readonly templatesService = inject(TaskTemplatesService);
    private readonly bestFitService = inject(BestFitService);
    private readonly fb = inject(FormBuilder);

    readonly localization = inject(LocalizationService);

    readonly tareas = signal<Tarea[]>([]);
    readonly plantillas = signal<PlantillaTarea[]>([]);
    readonly opciones = signal<TareaOpciones>({ proyectos: [], estados: [], empleados: [], skills: [] });
    readonly mostrar = signal(false);
    readonly mostrarSkill = signal(false);
    readonly recomendaciones = signal<RecomendacionBestFit[]>([]);
    readonly buscandoRecurso = signal(false);
    readonly error = signal('');
    readonly checklist = signal<ChecklistItem[]>([]);
    readonly nuevoChecklist = signal('');

    readonly form = this.fb.group({
        id: [0],
        idPlantillaTarea: [0],
        idProyecto: [0, Validators.min(1)],
        idEstadoTarea: [0, Validators.min(1)],
        idEmpleadoAsignado: [null as number | null],
        idTareaPredecesora: [null as number | null],
        idSkillRequerido: [0, Validators.min(1)],
        titulo: ['', Validators.required],
        descripcion: [''],
        prioridad: ['Media'],
        complejidad: ['Media'],
        fechaInicio: [''],
        deadline: [''],
        seniorityRequerido: [''],
        porcentajeAvance: [0],
        horasEstimadas: [0],
        checklistJson: [''],
        archivosAdjuntosJson: ['']
    });

    readonly skillForm = this.fb.group({
        nombre: ['', Validators.required],
        categoria: ['']
    });
    readonly filtroForm = this.fb.group({ idProyecto: [null as number | null], estado: [''], prioridad: [''], idEmpleadoAsignado: [null as number | null], idSkillRequerido: [null as number | null], deadlineDesde: [''], deadlineHasta: [''] });

    constructor()
    {
        this.cargar();
    }

    cargar(): void
    {
        this.service.consultar().subscribe((tareas: Tarea[]) => this.tareas.set(tareas));
        this.service.opciones().subscribe((opciones: TareaOpciones) => this.opciones.set(opciones));
        this.templatesService.consultar().subscribe({ next: (plantillas: PlantillaTarea[]) => this.plantillas.set(plantillas) });
    }

    nuevo(): void
    {
        const estadoPendiente = this.opciones().estados.find(estado => estado.nombre.toLowerCase() === 'pendiente')?.id ?? 0;
        this.form.reset({ id: 0, idPlantillaTarea: 0, idProyecto: 0, idEstadoTarea: estadoPendiente, idEmpleadoAsignado: null, idTareaPredecesora: null, idSkillRequerido: 0, titulo: '', descripcion: '', prioridad: 'Media', complejidad: 'Media', fechaInicio: '', deadline: '', seniorityRequerido: '', porcentajeAvance: 0, horasEstimadas: 0, checklistJson: '', archivosAdjuntosJson: '' });
        this.checklist.set([]);
        this.nuevoChecklist.set('');
        this.error.set('');
        this.recomendaciones.set([]);
        this.mostrar.set(true);
    }

    editar(tarea: Tarea): void
    {
        this.form.patchValue({ ...tarea, idPlantillaTarea: 0, archivosAdjuntosJson: this.obtenerUrlsAdjuntas(tarea.archivosAdjuntosJson).join('\n') });
        this.checklist.set(this.obtenerChecklist(tarea.checklistJson));
        this.nuevoChecklist.set('');
        this.recomendaciones.set([]);
        this.mostrar.set(true);
    }

    aplicarPlantilla(): void
    {
        const idPlantilla = this.form.controls.idPlantillaTarea.value;
        const plantilla = this.plantillas().find((item: PlantillaTarea) => item.id === idPlantilla);
        if (!plantilla)
        {
            return;
        }

        this.form.patchValue({
            titulo: plantilla.tituloSugerido || '',
            descripcion: plantilla.descripcionBase || '',
            idSkillRequerido: plantilla.idSkillRequerido || 0,
            prioridad: plantilla.prioridadSugerida || 'Media',
            complejidad: plantilla.complejidad || 'Media',
            seniorityRequerido: plantilla.seniorityRecomendado || '',
            horasEstimadas: plantilla.horasEstimadas || 0,
            archivosAdjuntosJson: this.obtenerUrlsAdjuntas(plantilla.archivosAdjuntosJson).join('\n')
        });
        this.checklist.set(this.obtenerChecklist(plantilla.checklistBaseJson));
        this.actualizarAvanceChecklist();
    }

    guardar(sugerirRecurso: boolean = false): void
    {
        if (this.form.invalid)
        {
            this.error.set('Completá título, proyecto, skill y estado.');
            return;
        }

        const datos = this.form.getRawValue();
        const urls = this.obtenerUrlsAdjuntas(datos.archivosAdjuntosJson);

        if (urls.length > 5 || urls.some(url => !this.esUrlImagen(url)))
        {
            this.error.set('Indicá hasta 5 URLs válidas de imágenes externas.');
            return;
        }

        const tarea = { ...datos, checklistJson: JSON.stringify(this.checklist()), archivosAdjuntosJson: urls.length === 0 ? null : JSON.stringify(urls) } as unknown as Partial<Tarea>;

        this.service.guardar(tarea).subscribe({
            next: (resultado: Tarea) =>{
                if (sugerirRecurso)
                {
                    this.form.patchValue({ id: resultado.id });
                    this.sugerirRecurso();
                }
                else
                {
                    this.mostrar.set(false);
                }
                this.cargar();
            },error: respuesta => this.error.set(this.obtenerMensajeError(respuesta, 'No se pudo guardar la tarea.'))
        });
    }

    aplicarFiltro(): void
    {
        const valores = this.filtroForm.getRawValue();
        const filtro: FiltroTarea = { idProyecto: valores.idProyecto, estado: valores.estado || null, prioridad: valores.prioridad || null, idEmpleadoAsignado: valores.idEmpleadoAsignado, idSkillRequerido: valores.idSkillRequerido, deadlineDesde: valores.deadlineDesde || null, deadlineHasta: valores.deadlineHasta || null };
        this.service.filtrar(filtro).subscribe({ next: (tareas: Tarea[]) => this.tareas.set(tareas), error: () => this.error.set('No se pudo aplicar el filtro de tareas.') });
    }

    limpiarFiltro(): void
    {
        this.filtroForm.reset({ idProyecto: null, estado: '', prioridad: '', idEmpleadoAsignado: null, idSkillRequerido: null, deadlineDesde: '', deadlineHasta: '' });
        this.cargar();
    }

    tareasPredecesoras(): Tarea[]
    {
        const idProyecto = this.form.controls.idProyecto.value;
        const idTarea = this.form.controls.id.value;
        return this.tareas().filter(tarea => tarea.activo && tarea.idProyecto === idProyecto && tarea.id !== idTarea);
    }

    sugerirRecurso(): void
    {
        const idTarea = this.form.controls.id.value;
        if (!idTarea)
        {
            this.guardar(true);
            return;
        }
        this.buscandoRecurso.set(true);
        this.error.set('');
        this.bestFitService.sugerir({ idTarea }).subscribe({ next: (recomendaciones: RecomendacionBestFit[]) => { this.recomendaciones.set(recomendaciones); this.buscandoRecurso.set(false); }, error: () => { this.error.set('No se pudieron generar sugerencias para esta tarea.'); this.buscandoRecurso.set(false); } });
    }

    seleccionarRecurso(recomendacion: RecomendacionBestFit): void
    {
        this.form.controls.idEmpleadoAsignado.setValue(recomendacion.idEmpleadoSugerido);
        this.recomendaciones.set([]);
    }

    agregarChecklist(): void
    {
        const texto = this.nuevoChecklist().trim();
        if (!texto)
        {
            return;
        }

        this.checklist.update(items => [...items, { texto, completada: false }]);
        this.nuevoChecklist.set('');
        this.actualizarAvanceChecklist();
    }

    alternarChecklist(indice: number): void
    {
        this.checklist.update(items => items.map((item, posicion) => posicion === indice ? { ...item, completada: !item.completada } : item));
        this.actualizarAvanceChecklist();
    }

    eliminarChecklist(indice: number): void
    {
        this.checklist.update(items => items.filter((_, posicion) => posicion !== indice));
        this.actualizarAvanceChecklist();
    }

    abrirSkills(): void
    {
        this.error.set('');
        this.mostrarSkill.set(true);
    }

    crearSkill(): void
    {
        if (this.skillForm.invalid)
        {
            return;
        }

        const datos = this.skillForm.getRawValue();

        this.service.crearSkill({ nombre: datos.nombre ?? '', categoria: datos.categoria ?? '' }).subscribe({
            next: (skill: Skill) =>{
                this.skillForm.reset();
                this.mostrarSkill.set(false);
                this.service.opciones().subscribe((opciones: TareaOpciones) =>{
                    this.opciones.set(opciones);
                    this.form.controls.idSkillRequerido.setValue(skill.id);
                });
            },error: () => this.error.set('No se pudo crear la skill.')
        });
    }

    cambiarEstadoSkill(skill: Skill): void
    {
      this.service.cambiarEstadoSkill({ ...skill, activo: !skill.activo }).subscribe({
        next: () => this.service.opciones().subscribe((opciones: TareaOpciones) => this.opciones.set(opciones)),
        error: () => this.error.set('No se pudo actualizar la skill.')
      });
    }

    cambiar(tarea: Tarea): void
    {
      this.service.cambiarEstado({ ...tarea, activo: !tarea.activo }).subscribe(() => this.cargar());
    }

    private obtenerUrlsAdjuntas(archivosAdjuntosJson?: string | null): string[]
    {
        if (!archivosAdjuntosJson)
        {
            return [];
        }

        try
        {
            const archivos = JSON.parse(archivosAdjuntosJson);
            return Array.isArray(archivos) ? archivos.filter((archivo: unknown): archivo is string => typeof archivo === 'string') : archivosAdjuntosJson.split(/\r?\n/).map(url => url.trim()).filter(Boolean);
        }
        catch
        {
            return archivosAdjuntosJson.split(/\r?\n/).map(url => url.trim()).filter(Boolean);
        }
    }

    private obtenerChecklist(checklistJson?: string | null): ChecklistItem[]
    {
        if (!checklistJson)
        {
            return [];
        }

        try
        {
            const checklist = JSON.parse(checklistJson);
            return Array.isArray(checklist) ? checklist.filter(item => typeof item?.texto === 'string').map(item => ({ texto: item.texto, completada: Boolean(item.completada) })) : [];
        }
        catch
        {
            return [];
        }
    }

    private actualizarAvanceChecklist(): void
    {
        const checklist = this.checklist();
        if (!checklist.length)
        {
            return;
        }

        const porcentaje = Math.round((checklist.filter(item => item.completada).length / checklist.length) * 100);
        this.form.controls.porcentajeAvance.setValue(porcentaje);
    }

    private esUrlImagen(url: string): boolean
    {
        try
        {
            const direccion = new URL(url);
            return direccion.protocol === 'https:' || direccion.protocol === 'http:';
        }
        catch
        {
            return false;
        }
    }

    private obtenerMensajeError(respuesta: any, predeterminado: string): string
    {
        if (typeof respuesta?.error === 'string')
        {
            return respuesta.error;
        }

        const errores = respuesta?.error?.errors;
        if (errores && typeof errores === 'object')
        {
            const mensajes = Object.values(errores).flat().filter((mensaje): mensaje is string => typeof mensaje === 'string');
            if (mensajes.length)
            {
                return mensajes.join(' ');
            }
        }

        return predeterminado;
    }
}

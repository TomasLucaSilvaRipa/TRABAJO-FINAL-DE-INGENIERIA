import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AreaSkill, Skill } from '../../../../services/resources.service';
import { AsignacionTarea, FiltroTarea, Tarea, TareaOpciones, TasksService } from '../../../../services/tasks.service';
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
    readonly recomendacionesConservadas = signal<RecomendacionBestFit[]>([]);
    readonly mensajeSugerencias = signal('');
    readonly buscandoRecurso = signal(false);
    readonly error = signal('');
    readonly checklist = signal<ChecklistItem[]>([]);
    readonly nuevoChecklist = signal('');
    readonly asignaciones = signal<AsignacionTarea[]>([]);
    readonly areasSkills = signal<AreaSkill[]>([]);
    readonly skillEnEdicion = signal<Skill | null>(null);

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
        seniorityRequerido: ['Semisenior', Validators.required],
        porcentajeAvance: [0],
        horasEstimadas: [0],
        checklistJson: [''],
        archivosAdjuntosJson: ['']
    });

    readonly skillForm = this.fb.group({
        id: [0],
        nombre: ['', Validators.required],
        idAreaSkill: [0, Validators.min(1)]
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
        this.service.consultarAreasSkills().subscribe({ next: areas => this.areasSkills.set(areas), error: () => this.areasSkills.set([]) });
        this.templatesService.consultar().subscribe({ next: (plantillas: PlantillaTarea[]) => this.plantillas.set(plantillas) });
    }

    nuevo(): void
    {
        const estadoPendiente = this.opciones().estados.find(estado => estado.nombre.toLowerCase() === 'pendiente')?.id ?? 0;
        this.form.reset({ id: 0, idPlantillaTarea: 0, idProyecto: 0, idEstadoTarea: estadoPendiente, idEmpleadoAsignado: null, idTareaPredecesora: null, idSkillRequerido: 0, titulo: '', descripcion: '', prioridad: 'Media', complejidad: 'Media', fechaInicio: '', deadline: '', seniorityRequerido: 'Semisenior', porcentajeAvance: 0, horasEstimadas: 0, checklistJson: '', archivosAdjuntosJson: '' });
        this.checklist.set([]);
        this.nuevoChecklist.set('');
        this.error.set('');
        this.recomendaciones.set([]);
        this.recomendacionesConservadas.set([]);
        this.mensajeSugerencias.set('');
        this.asignaciones.set([]);
        this.mostrar.set(true);
    }

    editar(tarea: Tarea): void
    {
        this.form.patchValue({
            ...tarea,
            idPlantillaTarea: 0,
            seniorityRequerido: tarea.seniorityRequerido || this.senioritySegunComplejidad(tarea.complejidad),
            fechaInicio: this.formatearFechaParaControl(tarea.fechaInicio),
            deadline: this.formatearFechaParaControl(tarea.deadline),
            archivosAdjuntosJson: this.obtenerUrlsAdjuntas(tarea.archivosAdjuntosJson).join('\n')
        });
        this.checklist.set(this.obtenerChecklist(tarea.checklistJson));
        this.nuevoChecklist.set('');
        this.recomendaciones.set([]);
        this.mensajeSugerencias.set('');
        this.asignaciones.set([]);
        this.service.consultarAsignaciones(tarea.id).subscribe({ next: asignaciones => this.asignaciones.set(asignaciones.filter(item => item.activa !== false)) });
        this.cargarRecomendacionesConservadas(tarea.id);
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
            seniorityRequerido: plantilla.seniorityRecomendado || this.senioritySegunComplejidad(plantilla.complejidad),
            horasEstimadas: plantilla.horasEstimadas || 0,
            archivosAdjuntosJson: this.obtenerUrlsAdjuntas(plantilla.archivosAdjuntosJson).join('\n')
        });
        this.checklist.set(this.obtenerChecklist(plantilla.checklistBaseJson));
        this.actualizarAvanceChecklist();
    }

    guardar(sugerirRecurso: boolean = false): void
    {
        if (!this.validarCamposObligatorios())
        {
            return;
        }

        if (sugerirRecurso && !this.validarRequisitosBestFit())
        {
            return;
        }

        const datos = this.form.getRawValue();
        const urls = this.obtenerUrlsAdjuntas(datos.archivosAdjuntosJson);

        if (urls.length > 5 || urls.some(url => !this.esUrlImagen(url)))
        {
            this.error.set('Indicá hasta 5 URLs válidas de imágenes externas.');
            return;
        }

        const asignaciones = this.prepararAsignaciones(datos.idEmpleadoAsignado, Number(datos.horasEstimadas ?? 0));
        if (!this.validarAsignaciones(datos.idEmpleadoAsignado, asignaciones, Number(datos.horasEstimadas ?? 0)))
        {
            return;
        }

        const tarea = {
            ...datos,
            fechaInicio: datos.fechaInicio || null,
            deadline: datos.deadline || null,
            checklistJson: JSON.stringify(this.checklist()),
            archivosAdjuntosJson: urls.length === 0 ? null : JSON.stringify(urls),
            asignaciones
        } as unknown as Partial<Tarea>;

        this.service.guardar(tarea).subscribe({
            next: (resultado: Tarea) =>{
                if (sugerirRecurso)
                {
                    this.form.patchValue({ id: resultado.id });
                    this.sugerirRecurso(true);
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

    sugerirRecurso(usarTareaGuardada: boolean = false): void
    {
        // La recomendación se calcula sobre lo persistido. Si el PM acaba de
        // agregar o quitar participantes, primero se guarda la distribución
        // automática para que Best Fit evalúe la composición actual.
        if (!usarTareaGuardada)
        {
            this.guardar(true);
            return;
        }

        const idTarea = this.form.controls.id.value;
        if (!idTarea)
        {
            return;
        }
        if (!this.validarRequisitosBestFit())
        {
            return;
        }
        this.buscandoRecurso.set(true);
        this.error.set('');
        this.recomendaciones.set([]);
        this.mensajeSugerencias.set('');
        this.bestFitService.sugerir({ idTarea }).subscribe({
            next: (recomendaciones: RecomendacionBestFit[]) => {
                this.recomendaciones.set(recomendaciones);
                if (recomendaciones.length === 0)
                {
                    this.mensajeSugerencias.set('No hay otros empleados compatibles disponibles para esta tarea. Revisá la skill requerida o la disponibilidad del equipo.');
                }
                this.buscandoRecurso.set(false);
            },
            error: respuesta => { this.error.set(this.obtenerMensajeError(respuesta, 'No se pudieron generar sugerencias para esta tarea.')); this.buscandoRecurso.set(false); }
        });
    }

    conservarRecurso(recomendacion: RecomendacionBestFit): void
    {
        this.error.set('');
        this.bestFitService.guardarRecomendacion(recomendacion).subscribe({
            next: () => {
                this.recomendaciones.update((items: RecomendacionBestFit[]) => items.map((item: RecomendacionBestFit) => item.idEmpleadoSugerido === recomendacion.idEmpleadoSugerido ? { ...item, seleccionadaPorPM: true, estado: 'Recomendada' } : item));
                this.cargarRecomendacionesConservadas(recomendacion.idTarea);
            },
            error: respuesta => this.error.set(this.obtenerMensajeError(respuesta, 'No se pudo conservar la recomendación.'))
        });
    }

    empleadosParaAsignar(): TareaOpciones['empleados']
    {
        const recomendaciones = this.recomendacionesConservadas();
        return [...this.opciones().empleados].sort((primero, segundo) => {
            const recomendacionPrimero = recomendaciones.find((item: RecomendacionBestFit) => item.idEmpleadoSugerido === primero.id);
            const recomendacionSegundo = recomendaciones.find((item: RecomendacionBestFit) => item.idEmpleadoSugerido === segundo.id);
            if (recomendacionPrimero && recomendacionSegundo) { return Number(recomendacionSegundo.puntajeCompatibilidad) - Number(recomendacionPrimero.puntajeCompatibilidad); }
            if (recomendacionPrimero) { return -1; }
            if (recomendacionSegundo) { return 1; }
            return `${primero.nombre} ${primero.apellido}`.localeCompare(`${segundo.nombre} ${segundo.apellido}`);
        });
    }

    textoEmpleadoParaAsignar(idEmpleado: number, nombre: string, apellido: string): string
    {
        const recomendacion = this.recomendacionesConservadas().find((item: RecomendacionBestFit) => item.idEmpleadoSugerido === idEmpleado);
        return recomendacion ? `${nombre} ${apellido} · Best Fit ${recomendacion.puntajeCompatibilidad}/100` : `${nombre} ${apellido}`;
    }

    agregarEmpleadoAsignado(idEmpleado: number): void
    {
        if (!idEmpleado || this.asignaciones().some(item => item.idEmpleado === idEmpleado)) { return; }
        this.asignaciones.update(items => [...items, { idEmpleado, horasAsignadas: 0, motivo: null, activa: true }]);
    }

    quitarEmpleadoAsignado(idEmpleado: number): void
    {
        if (this.form.controls.idEmpleadoAsignado.value === idEmpleado)
        {
            this.error.set('Elegí otro responsable principal o dejá la tarea sin asignar antes de quitarlo.');
            return;
        }
        this.asignaciones.update(items => items.filter(item => item.idEmpleado !== idEmpleado));
    }

    nombreEmpleado(idEmpleado: number): string
    {
        const empleado = this.opciones().empleados.find(item => item.id === idEmpleado);
        return empleado ? `${empleado.nombre} ${empleado.apellido}` : `Empleado #${idEmpleado}`;
    }

    private prepararAsignaciones(idResponsable: number | null, _horasEstimadas: number): AsignacionTarea[]
    {
        const asignaciones = this.asignaciones().map(item => ({ ...item, horasAsignadas: 0 }));
        if (idResponsable && !asignaciones.some(item => item.idEmpleado === idResponsable))
        {
            asignaciones.push({ idEmpleado: idResponsable, horasAsignadas: 0, motivo: null, activa: true });
        }
        return asignaciones;
    }

    private validarAsignaciones(idResponsable: number | null, asignaciones: AsignacionTarea[], horasEstimadas: number): boolean
    {
        if (asignaciones.length === 0) { return true; }
        if (!idResponsable) { this.error.set('Seleccioná el responsable principal de los empleados asignados.'); return false; }
        if (asignaciones.some(item => !item.idEmpleado)) { this.error.set('Uno de los empleados asignados no es válido.'); return false; }
        if (asignaciones.some((item, indice) => asignaciones.findIndex(otro => otro.idEmpleado === item.idEmpleado) !== indice)) { this.error.set('No podés asignar dos veces al mismo empleado en una tarea.'); return false; }
        if (horasEstimadas <= 0) { this.error.set('Indicá horas estimadas mayores a cero para distribuir el trabajo entre los empleados.'); return false; }
        return true;
    }

    esRecomendacionConservada(idEmpleado: number): boolean
    {
        return this.recomendacionesConservadas().some((item: RecomendacionBestFit) => item.idEmpleadoSugerido === idEmpleado);
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
        this.skillEnEdicion.set(null);
        this.skillForm.reset({ id: 0, nombre: '', idAreaSkill: 0 });
        this.mostrarSkill.set(true);
    }

    crearSkill(): void
    {
        if (this.skillForm.invalid)
        {
            return;
        }

        const datos = this.skillForm.getRawValue();
        const skill: Partial<Skill> = { id: datos.id ?? 0, nombre: datos.nombre ?? '', idAreaSkill: datos.idAreaSkill ?? null };
        const operacion = skill.id ? this.service.modificarSkill(skill) : this.service.crearSkill(skill);

        operacion.subscribe({
            next: (skill: Skill) =>{
                this.skillForm.reset({ id: 0, nombre: '', idAreaSkill: 0 });
                this.skillEnEdicion.set(null);
                this.service.opciones().subscribe((opciones: TareaOpciones) =>{
                    this.opciones.set(opciones);
                    this.form.controls.idSkillRequerido.setValue(skill.id);
                });
            },error: (respuesta) => this.error.set(this.obtenerMensajeError(respuesta, 'No se pudo guardar la skill.'))
        });
    }

    editarSkill(skill: Skill): void
    {
        this.skillEnEdicion.set(skill);
        this.skillForm.reset({ id: skill.id, nombre: skill.nombre, idAreaSkill: skill.idAreaSkill ?? 0 });
    }

    cambiarEstadoSkill(skill: Skill): void
    {
      this.service.cambiarEstadoSkill({ ...skill, activo: !skill.activo }).subscribe({
        next: () => this.service.opciones().subscribe((opciones: TareaOpciones) => this.opciones.set(opciones)),
        error: (respuesta) => this.error.set(this.obtenerMensajeError(respuesta, 'No se pudo actualizar la skill.'))
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

    private formatearFechaParaControl(fecha?: string | null): string
    {
        if (!fecha) { return ''; }
        const coincidencia = fecha.match(/^\d{4}-\d{2}-\d{2}/);
        if (coincidencia) { return coincidencia[0]; }
        const valorFecha = new Date(fecha);
        if (Number.isNaN(valorFecha.getTime())) { return ''; }
        const anio = valorFecha.getFullYear();
        const mes = String(valorFecha.getMonth() + 1).padStart(2, '0');
        const dia = String(valorFecha.getDate()).padStart(2, '0');
        return `${anio}-${mes}-${dia}`;
    }

    private cargarRecomendacionesConservadas(idTarea: number): void
    {
        this.bestFitService.consultarRecomendaciones(idTarea).subscribe({
            next: (recomendaciones: RecomendacionBestFit[]) => this.recomendacionesConservadas.set(recomendaciones),
            error: () => this.recomendacionesConservadas.set([])
        });
    }

    private validarCamposObligatorios(): boolean
    {
        const camposFaltantes: string[] = [];
        if (!this.form.controls.titulo.value?.trim()) { camposFaltantes.push('Título'); }
        if (!this.form.controls.idProyecto.value) { camposFaltantes.push('Proyecto'); }
        if (!this.form.controls.idEstadoTarea.value) { camposFaltantes.push('Estado'); }
        if (!this.form.controls.idSkillRequerido.value) { camposFaltantes.push('Skill requerida'); }
        if (!this.form.controls.seniorityRequerido.value) { camposFaltantes.push('Seniority requerido'); }
        if (camposFaltantes.length)
        {
            this.form.markAllAsTouched();
            this.error.set(`Completá los campos obligatorios: ${camposFaltantes.join(', ')}.`);
            return false;
        }
        return true;
    }

    private senioritySegunComplejidad(complejidad?: string | null): string
    {
        switch (complejidad?.trim().toLowerCase())
        {
            case 'baja': return 'Junior';
            case 'alta': return 'Senior';
            default: return 'Semisenior';
        }
    }

    private validarRequisitosBestFit(): boolean
    {
        if (!this.validarCamposObligatorios()) { return false; }
        const camposFaltantes: string[] = [];
        if (!this.form.controls.deadline.value) { camposFaltantes.push('Deadline'); }
        if (!this.form.controls.fechaInicio.value) { camposFaltantes.push('Inicio'); }
        if (!this.form.controls.horasEstimadas.value || this.form.controls.horasEstimadas.value <= 0) { camposFaltantes.push('Horas estimadas'); }
        if (camposFaltantes.length)
        {
            this.error.set(`Para sugerir recursos con Best Fit completá: ${camposFaltantes.join(', ')}.`);
            return false;
        }
        return true;
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

import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AreaSkill, Skill } from '../../../../services/resources.service';
import { ArchivoAdjuntoTarea, AsignacionTarea, FiltroTarea, Tarea, TareaOpciones, TasksService } from '../../../../services/tasks.service';
import { LocalizationService } from '../../../../services/localization.service';
import { BestFitService, RecomendacionBestFit } from '../../../../services/best-fit.service';
import { PlantillaTarea, TaskTemplatesService } from '../../../../services/task-templates.service';
import { forkJoin } from 'rxjs';
import { HistorialHorasTareaService } from '../../../../services/historial-horas-tarea.service';
import { sugerirHoras } from './sugerencia-horas';

interface ChecklistItem {
    texto: string;
    completada: boolean;
}

interface ErrorGestionTarea {
    clave: string;
    campos?: string[];
    detalle?: string;
}

@Component({ selector: 'app-task-manager.component', imports: [ReactiveFormsModule], templateUrl: './task-manager.component.html' })
export class TaskManagerComponent {
  private readonly service = inject(TasksService);
    private readonly templatesService = inject(TaskTemplatesService);
    private readonly bestFitService = inject(BestFitService);
    readonly historialHoras = inject(HistorialHorasTareaService);
    private readonly fb = inject(FormBuilder);

    readonly localization = inject(LocalizationService);

    readonly tareas = signal<Tarea[]>([]);
    private readonly tareasSinFiltro = signal<Tarea[]>([]);
    readonly plantillas = signal<PlantillaTarea[]>([]);
    readonly opciones = signal<TareaOpciones>({ proyectos: [], estados: [], empleados: [], skills: [] });
    readonly mostrar = signal(false);
    readonly mostrarSkill = signal(false);
    readonly recomendaciones = signal<RecomendacionBestFit[]>([]);
    readonly recomendacionesConservadas = signal<RecomendacionBestFit[]>([]);
    readonly mensajeSugerencias = signal('');
    readonly buscandoRecurso = signal(false);
    readonly error = signal<ErrorGestionTarea | null>(null);
    readonly textoError = computed(() => {
        const error = this.error();
        if (!error) return '';
        const campos = error.campos?.map(clave => this.localization.traducir(clave)).join(', ') ?? '';
        const mensaje = this.localization.traducir(error.clave).replace('{fields}', campos);
        return error.detalle ? `${mensaje} ${this.localization.traducir(error.detalle)}` : mensaje;
    });
    private fechasOriginales: { inicio: string; limite: string } | null = null;
    readonly checklist = signal<ChecklistItem[]>([]);
    readonly nuevoChecklist = signal('');
    readonly asignaciones = signal<AsignacionTarea[]>([]);
    readonly areasSkills = signal<AreaSkill[]>([]);
    readonly skillEnEdicion = signal<Skill | null>(null);
    readonly adjuntos = signal<ArchivoAdjuntoTarea[]>([]);
    readonly archivosNuevos = signal<File[]>([]);

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
    private readonly valoresParaSugerencia = toSignal(this.form.valueChanges, { initialValue: this.form.value });
    readonly sugerenciaHoras = computed(() => {
        const { id, idSkillRequerido, complejidad } = this.valoresParaSugerencia();
        const horas = this.historialHoras.horasConsultadas();
        if (id || !idSkillRequerido || !complejidad || !horas) return null;

        return sugerirHoras(this.tareasSinFiltro(), horas, idSkillRequerido, complejidad);
    });

    horasDeReferencia(horas: number): string
    {
        return new Intl.NumberFormat(this.localization.culture(), { maximumFractionDigits: 1 }).format(horas);
    }

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
        this.service.consultar().subscribe({
            next: tareas => {
                this.tareas.set(tareas);
                this.tareasSinFiltro.set(tareas);
            },
            error: respuesta => this.mostrarErrorApi(respuesta, 'taskErrors.loadTasks'),
        });
        this.service.opciones().subscribe({
            next: opciones => this.opciones.set(opciones),
            error: respuesta => this.mostrarErrorApi(respuesta, 'taskErrors.loadOptions'),
        });
        this.service.consultarAreasSkills().subscribe({ next: areas => this.areasSkills.set(areas), error: () => this.areasSkills.set([]) });
        this.templatesService.consultar().subscribe({ next: (plantillas: PlantillaTarea[]) => this.plantillas.set(plantillas) });
    }

    nuevo(): void
    {
        this.fechasOriginales = null;
        const estadoPendiente = this.opciones().estados.find(estado => estado.nombre.toLowerCase() === 'pendiente')?.id ?? 0;
        this.form.reset({ id: 0, idPlantillaTarea: 0, idProyecto: 0, idEstadoTarea: estadoPendiente, idEmpleadoAsignado: null, idTareaPredecesora: null, idSkillRequerido: 0, titulo: '', descripcion: '', prioridad: 'Media', complejidad: 'Media', fechaInicio: '', deadline: '', seniorityRequerido: 'Semisenior', porcentajeAvance: 0, horasEstimadas: 0, checklistJson: '', archivosAdjuntosJson: '' });
        this.checklist.set([]);
        this.nuevoChecklist.set('');
        this.error.set(null);
        this.recomendaciones.set([]);
        this.recomendacionesConservadas.set([]);
        this.mensajeSugerencias.set('');
        this.asignaciones.set([]);
        this.adjuntos.set([]);
        this.archivosNuevos.set([]);
        this.mostrar.set(true);
    }

    editar(tarea: Tarea): void
    {
        this.error.set(null);
        this.fechasOriginales = {
            inicio: this.formatearFechaParaControl(tarea.fechaInicio),
            limite: this.formatearFechaParaControl(tarea.deadline),
        };
        this.form.patchValue({
            ...tarea,
            idPlantillaTarea: 0,
            seniorityRequerido: tarea.seniorityRequerido || this.senioritySegunComplejidad(tarea.complejidad),
            fechaInicio: this.fechasOriginales.inicio,
            deadline: this.fechasOriginales.limite,
            archivosAdjuntosJson: this.obtenerUrlsAdjuntas(tarea.archivosAdjuntosJson).join('\n')
        });
        this.checklist.set(this.obtenerChecklist(tarea.checklistJson));
        this.nuevoChecklist.set('');
        this.recomendaciones.set([]);
        this.mensajeSugerencias.set('');
        this.asignaciones.set([]);
        this.service.consultarAsignaciones(tarea.id).subscribe({ next: asignaciones => this.asignaciones.set(asignaciones.filter(item => item.activa !== false)) });
        this.cargarAdjuntos(tarea.id);
        this.archivosNuevos.set([]);
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
        this.error.set(null);
        if (!this.validarCamposObligatorios() || !this.validarFechas())
        {
            return;
        }

        if (sugerirRecurso && !this.validarRequisitosBestFit())
        {
            return;
        }

        const datos = this.form.getRawValue();
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
            archivosAdjuntosJson: datos.archivosAdjuntosJson || null,
            asignaciones
        } as unknown as Partial<Tarea>;

        this.service.guardar(tarea).subscribe({
            next: (resultado: Tarea) =>{
                this.subirPendientes(resultado, sugerirRecurso);
            },error: respuesta => this.mostrarErrorApi(respuesta, 'taskErrors.saveTask')
        });
    }

    async seleccionarArchivos(evento: Event): Promise<void>
    {
        const input = evento.target as HTMLInputElement;
        const seleccionados = Array.from(input.files ?? []);
        if (seleccionados.some(archivo => archivo.size > 25 * 1024 * 1024)) { this.error.set({ clave: 'taskErrors.fileSize' }); input.value = ''; return; }
        const disponibles = Math.max(0, 20 - this.adjuntos().length - this.archivosNuevos().length);
        const preparados: File[] = [];
        for (const archivo of seleccionados.slice(0, disponibles)) preparados.push(await this.optimizarImagen(archivo));
        this.archivosNuevos.update(actuales => [...actuales, ...preparados]);
        input.value = '';
    }

    quitarArchivoNuevo(indice: number): void { this.archivosNuevos.update(items => items.filter((_, posicion) => posicion !== indice)); }
    eliminarAdjunto(adjunto: ArchivoAdjuntoTarea): void { const idTarea = this.form.controls.id.value; if (!idTarea) return; this.service.eliminarAdjunto(idTarea, adjunto.id).subscribe({ next: () => this.cargarAdjuntos(idTarea), error: respuesta => this.mostrarErrorApi(respuesta, 'taskErrors.deleteAttachment') }); }
    abrirAdjunto(adjunto: ArchivoAdjuntoTarea): void { this.service.descargarAdjunto(adjunto.idTarea, adjunto.id).subscribe({ next: contenido => { const url = URL.createObjectURL(contenido); window.open(url, '_blank', 'noopener,noreferrer'); setTimeout(() => URL.revokeObjectURL(url), 60000); }, error: respuesta => this.mostrarErrorApi(respuesta, 'taskErrors.downloadAttachment') }); }
    tamanoArchivo(bytes: number): string { return bytes >= 1024 * 1024 ? `${(bytes / 1024 / 1024).toFixed(1)} MB` : `${Math.max(1, Math.round(bytes / 1024))} KB`; }

    private cargarAdjuntos(idTarea: number): void { this.service.consultarAdjuntos(idTarea).subscribe({ next: items => this.adjuntos.set(items), error: () => this.adjuntos.set([]) }); }
    private subirPendientes(tarea: Tarea, sugerirRecurso: boolean): void
    {
        const archivos = this.archivosNuevos();
        const finalizar = () => { this.archivosNuevos.set([]); if (sugerirRecurso) { this.form.patchValue({ id: tarea.id }); this.sugerirRecurso(true); } else { this.mostrar.set(false); } this.cargar(); };
        if (!archivos.length) { finalizar(); return; }
        forkJoin(archivos.map(archivo => this.service.subirAdjunto(tarea.id, archivo))).subscribe({ next: () => finalizar(), error: respuesta => { this.form.patchValue({ id: tarea.id }); this.cargarAdjuntos(tarea.id); this.error.set({ clave: 'taskErrors.partialUpload', detalle: this.claveErrorApi(respuesta, 'taskErrors.retryUpload') }); } });
    }

    private async optimizarImagen(archivo: File): Promise<File>
    {
        if (!archivo.type.startsWith('image/') || archivo.type === 'image/gif') return archivo;
        const bitmap = await createImageBitmap(archivo); const maximo = 1800; const escala = Math.min(1, maximo / Math.max(bitmap.width, bitmap.height));
        const canvas = document.createElement('canvas'); canvas.width = Math.max(1, Math.round(bitmap.width * escala)); canvas.height = Math.max(1, Math.round(bitmap.height * escala)); canvas.getContext('2d')!.drawImage(bitmap, 0, 0, canvas.width, canvas.height); bitmap.close();
        const blob = await new Promise<Blob | null>(resolve => canvas.toBlob(resolve, 'image/webp', 0.82));
        if (!blob || blob.size >= archivo.size) return archivo;
        return new File([blob], archivo.name.replace(/\.[^.]+$/, '') + '.webp', { type: 'image/webp', lastModified: Date.now() });
    }

    aplicarFiltro(): void
    {
        this.error.set(null);
        const valores = this.filtroForm.getRawValue();
        const filtro: FiltroTarea = { idProyecto: valores.idProyecto, estado: valores.estado || null, prioridad: valores.prioridad || null, idEmpleadoAsignado: valores.idEmpleadoAsignado, idSkillRequerido: valores.idSkillRequerido, deadlineDesde: valores.deadlineDesde || null, deadlineHasta: valores.deadlineHasta || null };
        this.service.filtrar(filtro).subscribe({
            next: tareas => this.tareas.set(tareas),
            error: respuesta => this.mostrarErrorApi(respuesta, 'taskErrors.filterTasks'),
        });
    }

    limpiarFiltro(): void
    {
        this.error.set(null);
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
        this.error.set(null);
        this.recomendaciones.set([]);
        this.mensajeSugerencias.set('');
        this.bestFitService.sugerir({ idTarea }).subscribe({
            next: (recomendaciones: RecomendacionBestFit[]) => {
                this.recomendaciones.set(recomendaciones);
                if (recomendaciones.length === 0)
                {
                    this.mensajeSugerencias.set('taskErrors.noCandidates');
                }
                this.buscandoRecurso.set(false);
            },
            error: respuesta => { this.mostrarErrorApi(respuesta, 'taskErrors.suggestResources'); this.buscandoRecurso.set(false); }
        });
    }

    conservarRecurso(recomendacion: RecomendacionBestFit): void
    {
        this.error.set(null);
        this.bestFitService.guardarRecomendacion(recomendacion).subscribe({
            next: () => {
                this.recomendaciones.update((items: RecomendacionBestFit[]) => items.map((item: RecomendacionBestFit) => item.idEmpleadoSugerido === recomendacion.idEmpleadoSugerido ? { ...item, seleccionadaPorPM: true, estado: 'Recomendada' } : item));
                this.cargarRecomendacionesConservadas(recomendacion.idTarea);
            },
            error: respuesta => this.mostrarErrorApi(respuesta, 'taskErrors.keepRecommendation')
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
            this.error.set({ clave: 'taskErrors.replacePrimary' });
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
        if (!idResponsable) { this.error.set({ clave: 'taskErrors.primaryRequired' }); return false; }
        if (asignaciones.some(item => !item.idEmpleado)) { this.error.set({ clave: 'taskErrors.invalidAssignee' }); return false; }
        if (asignaciones.some((item, indice) => asignaciones.findIndex(otro => otro.idEmpleado === item.idEmpleado) !== indice)) { this.error.set({ clave: 'taskErrors.duplicateAssignee' }); return false; }
        if (horasEstimadas <= 0) { this.error.set({ clave: 'taskErrors.positiveHours' }); return false; }
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
        this.error.set(null);
        this.skillEnEdicion.set(null);
        this.skillForm.reset({ id: 0, nombre: '', idAreaSkill: 0 });
        this.mostrarSkill.set(true);
    }

    crearSkill(): void
    {
        if (this.skillForm.invalid)
        {
            const campos = [];
            if (this.skillForm.controls.nombre.invalid) campos.push('taskErrors.fieldName');
            if (this.skillForm.controls.idAreaSkill.invalid) campos.push('taskManager.area');
            this.error.set({ clave: 'taskErrors.requiredFields', campos });
            return;
        }

        this.error.set(null);
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
            },error: (respuesta) => this.mostrarErrorApi(respuesta, 'taskErrors.saveSkill')
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
        error: (respuesta) => this.mostrarErrorApi(respuesta, 'taskErrors.changeSkillStatus')
      });
    }

    cambiar(tarea: Tarea): void
    {
      this.error.set(null);
      this.service.cambiarEstado({ ...tarea, activo: !tarea.activo }).subscribe({
        next: () => this.cargar(),
        error: respuesta => this.mostrarErrorApi(respuesta, 'taskErrors.changeTaskStatus'),
      });
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

    fechaMinimaTarea(): string
    {
        return this.formatearFechaParaControl(new Date());
    }

    fechaMinimaDeadline(): string
    {
        const hoy = this.fechaMinimaTarea();
        const inicio = this.form.controls.fechaInicio.value;
        if (!inicio) return hoy;

        const siguienteDia = new Date(`${inicio}T00:00:00`);
        siguienteDia.setDate(siguienteDia.getDate() + 1);
        const minimo = this.formatearFechaParaControl(siguienteDia);
        return minimo > hoy ? minimo : hoy;
    }

    private validarFechas(): boolean
    {
        const { fechaInicio, deadline } = this.form.getRawValue();
        const hoy = this.fechaMinimaTarea();
        if (fechaInicio && fechaInicio < hoy && fechaInicio !== this.fechasOriginales?.inicio)
        {
            this.error.set({ clave: 'taskDates.startInPast' });
            return false;
        }
        if (deadline && deadline < hoy && deadline !== this.fechasOriginales?.limite)
        {
            this.error.set({ clave: 'taskDates.deadlineInPast' });
            return false;
        }
        if (fechaInicio && deadline && deadline <= fechaInicio)
        {
            this.error.set({ clave: 'taskDates.deadlineAfterStart' });
            return false;
        }
        return true;
    }

    private formatearFechaParaControl(fecha?: string | Date | null): string
    {
        if (!fecha) { return ''; }
        if (typeof fecha === 'string')
        {
            const coincidencia = fecha.match(/^\d{4}-\d{2}-\d{2}/);
            if (coincidencia) { return coincidencia[0]; }
        }
        const valorFecha = typeof fecha === 'string' ? new Date(fecha) : fecha;
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
        if (!this.form.controls.titulo.value?.trim()) { camposFaltantes.push('taskErrors.fieldTitle'); }
        if (!this.form.controls.idProyecto.value) { camposFaltantes.push('taskErrors.fieldProject'); }
        if (!this.form.controls.idEstadoTarea.value) { camposFaltantes.push('taskErrors.fieldStatus'); }
        if (!this.form.controls.idSkillRequerido.value) { camposFaltantes.push('taskErrors.fieldSkill'); }
        if (!this.form.controls.seniorityRequerido.value) { camposFaltantes.push('taskErrors.fieldSeniority'); }
        if (camposFaltantes.length)
        {
            this.form.markAllAsTouched();
            this.error.set({ clave: 'taskErrors.requiredFields', campos: camposFaltantes });
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
        if (!this.form.controls.deadline.value) { camposFaltantes.push('taskErrors.fieldDeadline'); }
        if (!this.form.controls.fechaInicio.value) { camposFaltantes.push('taskErrors.fieldStart'); }
        if (!this.form.controls.horasEstimadas.value || this.form.controls.horasEstimadas.value <= 0) { camposFaltantes.push('taskErrors.fieldHours'); }
        if (camposFaltantes.length)
        {
            this.error.set({ clave: 'taskErrors.bestFitFields', campos: camposFaltantes });
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

    private mostrarErrorApi(respuesta: { status?: number; error?: unknown }, alternativa: string): void
    {
        this.error.set({ clave: this.claveErrorApi(respuesta, alternativa) });
    }

    private claveErrorApi(respuesta: { status?: number; error?: unknown }, alternativa: string): string
    {
        if (respuesta.status === 0) return 'projectForm.connection';
        const cuerpo = respuesta.error;
        if (typeof cuerpo === 'string')
        {
            if (/sesi[oó]n ya no es v[aá]lida/i.test(cuerpo)) return 'projectForm.invalidSession';
            if (/no ten[eé]s (permiso|acceso)|no pertenece a (una agencia|uno de tus proyectos)/i.test(cuerpo)) return 'projectForm.notAllowed';
            if (/t[ií]tulo de la tarea/i.test(cuerpo)) return 'taskManager.titleRequired';
            if (/seleccion[aá] (el proyecto|el estado)|skill requerida para la tarea/i.test(cuerpo))
            {
                if (/proyecto/i.test(cuerpo)) return 'taskManager.selectProjectError';
                if (/estado/i.test(cuerpo)) return 'taskManager.selectStatusError';
                return 'taskManager.selectSkillError';
            }
            if (/deadline.*venci[oó]/i.test(cuerpo)) return 'taskDates.deadlineInPast';
            if (/25\s*MB/i.test(cuerpo)) return 'taskErrors.fileSize';
            if (/tipo de archivo no est[aá] permitido/i.test(cuerpo)) return 'taskErrors.fileType';
            if (/cuota de almacenamiento/i.test(cuerpo)) return 'taskErrors.storageQuota';
            if (/m[aá]ximo de adjuntos/i.test(cuerpo)) return 'taskErrors.attachmentLimit';
            if (/responsable principal/i.test(cuerpo)) return 'taskErrors.primaryRequired';
            if (/dos veces al mismo empleado/i.test(cuerpo)) return 'taskErrors.duplicateAssignee';
            if (/horas estimadas mayores a cero/i.test(cuerpo)) return 'taskErrors.positiveHours';
            if (/empleados asignados no tienen una disponibilidad/i.test(cuerpo)) return 'taskErrors.invalidAvailability';
            if (/empleados asignados no est[aá] activo/i.test(cuerpo)) return 'taskErrors.invalidAssignee';
            if (/ya no cumple las condiciones/i.test(cuerpo)) return 'taskErrors.candidateUnavailable';
            if (/tarea (precedente|predecesora).*proyecto/i.test(cuerpo)) return 'taskErrors.invalidPredecessor';
        }
        if (cuerpo && typeof cuerpo === 'object')
        {
            const campos = Object.keys((cuerpo as { errors?: Record<string, unknown> }).errors ?? {});
            if (campos.some(campo => /(^|\.)(fechaInicio|deadline)$/i.test(campo))) return 'projectForm.invalidDates';
            if (campos.some(campo => /(^|\.)titulo$/i.test(campo))) return 'taskManager.titleRequired';
            if (campos.some(campo => /(^|\.)idProyecto$/i.test(campo))) return 'taskManager.selectProjectError';
            if (campos.some(campo => /(^|\.)idEstadoTarea$/i.test(campo))) return 'taskManager.selectStatusError';
            if (campos.some(campo => /(^|\.)idSkillRequerido$/i.test(campo))) return 'taskManager.selectSkillError';
        }
        if (respuesta.status === 401) return 'projectForm.invalidSession';
        if (respuesta.status === 403) return 'projectForm.notAllowed';
        if (respuesta.status === 413) return 'taskErrors.fileSize';
        return alternativa;
    }
}

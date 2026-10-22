using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;
public class BLLTarea
{
    private readonly MPPTarea _tareaMPP;
    private readonly MPPAsignacionTarea _asignacionMPP;
    private readonly MPPRecursos _recursosMPP;
    private readonly BLLRol _rolBLL;
    private readonly BLLBitacora _bitacoraBLL;
    public BLLTarea(MPPTarea tareaMPP, MPPAsignacionTarea asignacionMPP, MPPRecursos recursosMPP, BLLRol rolBLL, BLLBitacora bitacoraBLL) { _tareaMPP = tareaMPP; _asignacionMPP = asignacionMPP; _recursosMPP = recursosMPP; _rolBLL = rolBLL; _bitacoraBLL = bitacoraBLL; }
    public List<Tarea> Consultar(Usuario usuario)
    {
        try
        {
            ValidarGestionTareas(usuario);
            if (ObtenerAgencia(usuario))
            {
                return _tareaMPP.Consultar(usuario);
            }
            else{ throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.");}
        }
        catch(Exception ex) { throw new Exception(ex.Message); }
    }
    public List<Tarea> ConsultarAsignadas(Usuario usuario) {
        try
        {
            if (!_rolBLL.TienePermiso(usuario, "VerDashboard")) { throw new UnauthorizedAccessException("No tenés permiso para consultar tus tareas."); }
            return _tareaMPP.ConsultarPorEmpleado(usuario);
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (ArgumentException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public List<Tarea> Consultar(Usuario usuario, FiltroTarea filtro)
    {
        try
        {
            ValidarGestionTareas(usuario);
            List<Tarea> tareas = _tareaMPP.Consultar(usuario, filtro);
            return tareas;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public List<Tarea> ConsultarPorPM(Usuario usuario)
    {
        try
        {
            ValidarGestionTareas(usuario);
            List<Tarea> tareas = _tareaMPP.ConsultarPorPM(usuario);
            return tareas;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public bool GuardarComentarios(Tarea tarea, Usuario usuario)
    {
        try
        {
            List<Tarea> tareas = ConsultarAsignadas(usuario);
            if (!tareas.Any(item => item.ID == tarea.ID))
            {
                throw new UnauthorizedAccessException("No tenés acceso a esta tarea.");
            }
            return _tareaMPP.GuardarComentarios(tarea,usuario);
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public GestionTareaOpciones ConsultarOpciones(Usuario usuario)
    {
        try
        {
            ValidarGestionTareas(usuario);
            if (ObtenerAgencia(usuario))
            {
                return _tareaMPP.ConsultarOpciones(usuario);
            }
            else { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); }
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Tarea Guardar(Tarea tarea, Usuario usuario)
    {
        try
        {
            ValidarGestionTareas(usuario);
            if (string.IsNullOrWhiteSpace(tarea.Titulo)) { throw new ArgumentException("Completá el título de la tarea."); }
            if (tarea.IdProyecto <= 0) { throw new ArgumentException("Seleccioná el proyecto de la tarea."); }
            if (tarea.IdEstadoTarea <= 0) { throw new ArgumentException("Seleccioná el estado inicial de la tarea."); }
            if (!tarea.IdSkillRequerido.HasValue || tarea.IdSkillRequerido.Value <= 0) { throw new ArgumentException("Seleccioná la skill requerida para la tarea."); }
            NormalizarSeniorityRequerido(tarea);
            tarea.Titulo = tarea.Titulo.Trim();
            if (ObtenerAgencia(usuario))
            {
                PrepararAsignaciones(tarea, usuario);
                ValidarAsignaciones(tarea);
                Tarea resultado = _tareaMPP.Guardar(tarea, usuario);
                if (tarea.Asignaciones is not null)
                {
                    resultado.Asignaciones = _asignacionMPP.Sincronizar(resultado, tarea.Asignaciones, usuario);
                }
                return resultado;
            }
            else { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); }
        }
        catch (ArgumentException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<AsignacionTarea> ConsultarAsignaciones(int idTarea, Usuario usuario)
    {
        ValidarGestionTareas(usuario);
        List<Tarea> tareasGestionables = _tareaMPP.ConsultarPorPM(usuario);
        Tarea? tarea = tareasGestionables.FirstOrDefault(item => item.ID == idTarea && item.Activo);
        if (idTarea <= 0 || tarea is null)
        {
            throw new ArgumentException("La tarea no existe o no pertenece a uno de tus proyectos.");
        }
        return _asignacionMPP.Consultar(tarea, usuario);
    }

    /// <summary>
    /// Convierte la selección de empleados en una distribución planificada.
    /// No se aceptan horas cargadas desde la UI: se reparte el total de la tarea
    /// en proporción a la disponibilidad semanal que cada empleado configuró.
    /// </summary>
    private void PrepararAsignaciones(Tarea tarea, Usuario usuario)
    {
        if (tarea.Asignaciones is null) return;

        Dictionary<int, AsignacionTarea> porEmpleado = tarea.Asignaciones
            .Where(item => item.IdEmpleado > 0)
            .GroupBy(item => item.IdEmpleado)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First());

        if (tarea.IdEmpleadoAsignado.HasValue && !porEmpleado.ContainsKey(tarea.IdEmpleadoAsignado.Value))
        {
            porEmpleado[tarea.IdEmpleadoAsignado.Value] = new AsignacionTarea
            {
                IdEmpleado = tarea.IdEmpleadoAsignado.Value,
                Activa = true
            };
        }

        if (porEmpleado.Count == 0)
        {
            tarea.Asignaciones = new List<AsignacionTarea>();
            return;
        }

        if (!tarea.IdEmpleadoAsignado.HasValue)
        {
            throw new ArgumentException("Seleccioná un responsable principal de los empleados asignados.");
        }
        if (tarea.HorasEstimadas <= 0)
        {
            throw new ArgumentException("Indicá horas estimadas mayores a cero para distribuir el trabajo entre los empleados.");
        }

        List<Empleado> capacidades = _asignacionMPP.ConsultarCapacidades(porEmpleado.Keys, usuario);
        if (capacidades.Count != porEmpleado.Count)
        {
            throw new ArgumentException("Uno de los empleados asignados no está activo o no pertenece a la agencia.");
        }

        decimal capacidadTotal = capacidades.Sum(empleado => Math.Max(0, empleado.HorasDisponiblesSemanales));
        if (capacidadTotal <= 0)
        {
            throw new ArgumentException("Los empleados asignados no tienen una disponibilidad semanal válida configurada.");
        }

        Dictionary<int, decimal> capacidadPorEmpleado = capacidades.ToDictionary(empleado => empleado.ID, empleado => Math.Max(0, empleado.HorasDisponiblesSemanales));
        decimal horasDistribuidas = 0;
        List<AsignacionTarea> distribuidas = new List<AsignacionTarea>();
        List<AsignacionTarea> seleccionadas = porEmpleado.Values.OrderBy(item => item.IdEmpleado).ToList();

        for (int indice = 0; indice < seleccionadas.Count; indice++)
        {
            AsignacionTarea asignacion = seleccionadas[indice];
            decimal horas = indice == seleccionadas.Count - 1
                ? tarea.HorasEstimadas - horasDistribuidas
                : Math.Round(tarea.HorasEstimadas * capacidadPorEmpleado[asignacion.IdEmpleado] / capacidadTotal, 2, MidpointRounding.AwayFromZero);
            horasDistribuidas += horas;
            asignacion.HorasAsignadas = horas;
            asignacion.Activa = true;
            distribuidas.Add(asignacion);
        }

        tarea.Asignaciones = distribuidas;
    }

    private static void ValidarAsignaciones(Tarea tarea)
    {
        if (tarea.Asignaciones is null) return;
        List<AsignacionTarea> activas = tarea.Asignaciones.Where(item => item.IdEmpleado > 0 && item.HorasAsignadas > 0).ToList();
        if (activas.Count != tarea.Asignaciones.Count) throw new ArgumentException("No se pudo distribuir las horas entre los empleados asignados.");
        if (activas.GroupBy(item => item.IdEmpleado).Any(grupo => grupo.Count() > 1)) throw new ArgumentException("No podés asignar dos veces al mismo empleado en una tarea.");
        decimal total = activas.Sum(item => item.HorasAsignadas);
        if (Math.Abs(total - tarea.HorasEstimadas) > 0.01m) throw new ArgumentException("Distribuí exactamente las horas estimadas de la tarea entre los empleados asignados.");
        if (tarea.IdEmpleadoAsignado.HasValue && !activas.Any(item => item.IdEmpleado == tarea.IdEmpleadoAsignado.Value))
        {
            throw new ArgumentException("El responsable principal debe integrar los empleados asignados.");
        }
        if (!tarea.IdEmpleadoAsignado.HasValue && activas.Count > 0)
        {
            throw new ArgumentException("Seleccioná un responsable principal para los empleados asignados.");
        }
    }
    public bool CambiarEstado(Tarea tarea, Usuario usuario)
    {
        try
        {
            ValidarGestionTareas(usuario);
            if (ObtenerAgencia(usuario))
            {
                return _tareaMPP.CambiarEstado(tarea, usuario);
            }
            else { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); }
        }catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public bool CambiarEstadoPropio(Tarea tarea, Usuario usuario)
    {
        try
        {
            if (!_rolBLL.TienePermiso(usuario, "VerKanban")) { throw new UnauthorizedAccessException("No tenés permiso para actualizar tareas desde el tablero."); }
            if (tarea.ID <= 0 || string.IsNullOrWhiteSpace(tarea.Estado)) { throw new ArgumentException("Indicá la tarea y el estado a actualizar."); }
            List<Tarea> tareas = ConsultarAsignadas(usuario);
            if (!tareas.Any(item => item.ID == tarea.ID)) { throw new UnauthorizedAccessException("No tenés acceso a esta tarea."); }
            bool resultado = _tareaMPP.CambiarEstadoPropio(tarea, usuario);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<Tarea> ConsultarTableroProyectoPropio(Tarea tarea, Usuario usuario)
    {
        try
        {
            if (!_rolBLL.TienePermiso(usuario, "VerKanban")) { throw new UnauthorizedAccessException("No tenés permiso para consultar el tablero."); }
            if (tarea.IdProyecto <= 0) { throw new ArgumentException("Indicá el proyecto a consultar."); }
            return _tareaMPP.ConsultarTableroProyectoPropio(tarea, usuario);
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (ArgumentException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Tarea ActualizarAvancePropio(Tarea tarea, Usuario usuario)
    {
        try
        {
            if (!_rolBLL.TienePermiso(usuario, "VerKanban")) { throw new UnauthorizedAccessException("No tenés permiso para actualizar tareas desde el tablero."); }
            if (tarea.ID <= 0 || string.IsNullOrWhiteSpace(tarea.Estado)) { throw new ArgumentException("Indicá la tarea y el estado a actualizar."); }
            if (tarea.PorcentajeAvance < 0 || tarea.PorcentajeAvance > 100) { throw new ArgumentException("El avance debe estar entre 0 y 100."); }
            if (string.Equals(tarea.Estado, "Bloqueada", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(tarea.MotivoBloqueo)) { throw new ArgumentException("Indicá el motivo del bloqueo."); }
            List<Tarea> tareas = ConsultarAsignadas(usuario);
            if (!tareas.Any(item => item.ID == tarea.ID)) { throw new UnauthorizedAccessException("No tenés acceso a esta tarea."); }
            Tarea resultado = _tareaMPP.ActualizarAvancePropio(tarea, usuario);
            _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Tarea", tarea.ID, "Actualizar avance de tarea", "Se actualizó el avance de la tarea " + resultado.Titulo + ".", "Exitoso", "Informacion", "Kanban"));
            return resultado;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (ArgumentException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Skill RegistrarSkill(Skill skill, Usuario usuario) {
        try
        {
            ValidarGestionTareas(usuario);
            ValidarSkill(skill);
            return _recursosMPP.RegistrarSkill(skill);
        }
        catch (ArgumentException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Skill ModificarSkill(Skill skill, Usuario usuario)
    {
        try
        {
            ValidarGestionTareas(usuario);
            if (skill.ID <= 0) { throw new ArgumentException("Seleccioná una skill válida para modificar."); }
            ValidarSkill(skill);
            return _recursosMPP.ModificarSkill(skill);
        }
        catch (ArgumentException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<AreaSkill> ConsultarAreasSkills(Usuario usuario)
    {
        ValidarGestionTareas(usuario);
        return _recursosMPP.ConsultarAreasSkills().Where(area => area.Activo).ToList();
    }
    public bool CambiarEstadoSkill(Skill skill, Usuario usuario) { 
        ValidarGestionTareas(usuario); 
        return _recursosMPP.CambiarEstadoSkill(skill); 
    }
    private void ValidarGestionTareas(Usuario usuario) { 
        if (!_rolBLL.TienePermiso(usuario, "GestionarTareas")) { throw new UnauthorizedAccessException("No tenés permiso para gestionar tareas."); } 
    }

    private void ValidarSkill(Skill skill)
    {
        if (string.IsNullOrWhiteSpace(skill.Nombre)) { throw new ArgumentException("Ingresá el nombre de la skill."); }
        if (!skill.IdAreaSkill.HasValue || !_recursosMPP.ConsultarAreasSkills().Any(area => area.ID == skill.IdAreaSkill.Value && area.Activo))
        {
            throw new ArgumentException("Seleccioná un área válida para la skill.");
        }

        skill.Nombre = skill.Nombre.Trim();
    }

    private static void NormalizarSeniorityRequerido(Tarea tarea)
    {
        string valor = (tarea.SeniorityRequerido ?? string.Empty).Trim().Replace(" ", string.Empty).ToLowerInvariant();
        tarea.SeniorityRequerido = valor switch
        {
            "junior" => "Junior",
            "semisenior" or "ssr" => "Semisenior",
            "senior" or "sr" => "Senior",
            _ => throw new ArgumentException("Seleccioná el seniority requerido para la tarea.")
        };
    }
    private bool ObtenerAgencia(Usuario usuario) { 
        if (!usuario.IdAgencia.HasValue) { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); } 
        return true; 
    }
}

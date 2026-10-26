using System.Text;
using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public sealed class BLLSlack
{
    private const int MaximoTareasMostradas = 8;
    private readonly MPPSlack _slackMPP;
    private readonly BLLRol _rolBLL;
    private readonly BLLTarea _tareaBLL;
    private readonly BLLBitacora _bitacoraBLL;

    public BLLSlack(MPPSlack slackMPP, BLLRol rolBLL, BLLTarea tareaBLL, BLLBitacora bitacoraBLL)
    {
        _slackMPP = slackMPP;
        _rolBLL = rolBLL;
        _tareaBLL = tareaBLL;
        _bitacoraBLL = bitacoraBLL;
    }

    public string ConsultarBalance(ConsultaSlack consultaSlack)
    {
        Usuario usuario = ObtenerUsuario(consultaSlack);
        string consulta = (consultaSlack.Texto ?? string.Empty).Trim();
        string respuesta;
        string accion;

        if (string.IsNullOrWhiteSpace(consulta))
        {
            respuesta = CrearAyuda(usuario);
            accion = "ayuda";
        }
        else if (consulta.StartsWith("empleado ", StringComparison.OrdinalIgnoreCase))
        {
            respuesta = ConsultarEmpleado(usuario, consulta["empleado ".Length..].Trim());
            accion = "empleado";
        }
        else if (string.Equals(consulta, "pendientes", StringComparison.OrdinalIgnoreCase))
        {
            respuesta = ConsultarPendientes(usuario);
            accion = "pendientes";
        }
        else if (string.Equals(consulta, "completadas", StringComparison.OrdinalIgnoreCase))
        {
            respuesta = ConsultarCompletadas(usuario);
            accion = "completadas";
        }
        else if (string.Equals(consulta, "criticas", StringComparison.OrdinalIgnoreCase))
        {
            respuesta = ConsultarCriticas(usuario);
            accion = "criticas";
        }
        else if (string.Equals(consulta, "equipo", StringComparison.OrdinalIgnoreCase))
        {
            respuesta = ConsultarEquipo(usuario);
            accion = "equipo";
        }
        else
        {
            throw new ArgumentException("No reconozco esa consulta. Usá /balance para ver las opciones disponibles.");
        }

        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Slack", null, "ConsultarCargaOperativaSlack", "Se consultó /balance " + accion + " desde Slack.", "Exitoso", "Informacion", "Planificación"));
        return respuesta;
    }

    public List<Usuario> ConsultarUsuariosSinVincular(Usuario solicitante)
    {
        ValidarGestionVinculaciones(solicitante);
        return _slackMPP.ConsultarUsuariosSinVincular();
    }

    public void VincularUsuario(VinculacionSlack vinculacion, Usuario solicitante)
    {
        ValidarGestionVinculaciones(solicitante);
        string idNormalizado = (vinculacion.IdUsuarioSlack ?? string.Empty).Trim().ToUpperInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(idNormalizado, "^[UW][A-Z0-9]{8,}$"))
        {
            throw new ArgumentException("El Member ID de Slack debe comenzar con U o W y contener sólo letras mayúsculas y números.");
        }

        vinculacion.IdUsuarioSlack = idNormalizado;
        _slackMPP.VincularUsuario(vinculacion);
        _bitacoraBLL.Add(new Bitacora(solicitante.ID, solicitante.IdAgencia, "Slack", vinculacion.Usuario.ID, "VincularUsuarioSlack", "Se vinculó un usuario de TeamBalance con su Member ID de Slack.", "Exitoso", "Informacion", "Seguridad"));
    }

    private Usuario ObtenerUsuario(ConsultaSlack consulta)
    {
        if (string.IsNullOrWhiteSpace(consulta.IdUsuarioSlack))
        {
            throw new UnauthorizedAccessException("Slack no informó el usuario que ejecutó el comando.");
        }

        consulta.IdUsuarioSlack = consulta.IdUsuarioSlack.Trim();
        Usuario? usuario = _slackMPP.ConsultarUsuarioPorSlack(consulta);
        if (usuario is null)
        {
            throw new UnauthorizedAccessException("Tu usuario de Slack todavía no está asociado a una cuenta de TeamBalance.");
        }

        if (!usuario.Activo || !string.Equals(usuario.Estado, "Activo", StringComparison.OrdinalIgnoreCase) || !usuario.IdAgencia.HasValue)
        {
            throw new UnauthorizedAccessException("Tu cuenta de TeamBalance no está habilitada para realizar esta consulta.");
        }

        _rolBLL.CargarAutorizacion(usuario);
        return usuario;
    }

    private void ValidarGestionVinculaciones(Usuario solicitante)
    {
        _rolBLL.CargarAutorizacion(solicitante);
        bool esSoporte = solicitante.Roles.Any(rol => string.Equals(rol.TipoUsuario, "Soporte", StringComparison.OrdinalIgnoreCase));
        if (!esSoporte || !_rolBLL.TienePermiso(solicitante, "GestionarOperadores"))
        {
            throw new UnauthorizedAccessException("Sólo un operador de Soporte autorizado puede vincular usuarios de Slack.");
        }
    }

    private string CrearAyuda(Usuario usuario)
    {
        StringBuilder texto = new StringBuilder("Consultas disponibles:\n");
        if (EsPM(usuario))
        {
            texto.Append("• /balance empleado <nombre>\n• /balance completadas\n• /balance criticas\n• /balance equipo");
        }
        else if (EsEmpleado(usuario))
        {
            texto.Append("• /balance pendientes\n• /balance completadas\n• /balance criticas");
        }
        else
        {
            texto.Append("No tenés un rol operativo habilitado para este comando.");
        }

        return texto.ToString();
    }

    private string ConsultarEmpleado(Usuario usuario, string nombreEmpleado)
    {
        ValidarPM(usuario);
        if (string.IsNullOrWhiteSpace(nombreEmpleado))
        {
            throw new ArgumentException("Indicá el nombre del empleado. Ejemplo: /balance empleado Pedro");
        }

        List<(Empleado Empleado, decimal CargaActual, decimal CapacidadSemanal, int TareasActivas)> coincidencias = _slackMPP.ConsultarEmpleadoPM(usuario, nombreEmpleado);
        if (coincidencias.Count == 0)
        {
            return "No encontré un empleado con ese nombre dentro de tus proyectos activos.";
        }

        if (coincidencias.Count > 1)
        {
            return "Encontré más de un empleado: " + string.Join(", ", coincidencias.Select(item => NombreCompleto(item.Empleado))) + ". Indicá un nombre más específico.";
        }

        (Empleado Empleado, decimal CargaActual, decimal CapacidadSemanal, int TareasActivas) carga = coincidencias[0];
        decimal disponibilidad = carga.CapacidadSemanal - carga.CargaActual;
        decimal ocupacion = CalcularOcupacion(carga.CargaActual, carga.CapacidadSemanal);
        List<Tarea> tareas = _tareaBLL.ConsultarPorPM(usuario)
            .Where(tarea => tarea.Activo && tarea.IdEmpleadoAsignado == carga.Empleado.ID && !EstaFinalizada(tarea))
            .OrderBy(tarea => tarea.Deadline ?? DateTime.MaxValue)
            .ToList();
        Dictionary<int, string> proyectos = ConsultarNombresProyectos(usuario);

        StringBuilder texto = new StringBuilder();
        texto.Append("Carga de ").Append(NombreCompleto(carga.Empleado)).Append("\n\n");
        texto.Append("Ocupación: ").Append(ocupacion.ToString("0.#")).Append(" %\n");
        texto.Append("Disponibilidad: ").Append(disponibilidad.ToString("0.#")).Append(" h\n");
        texto.Append("Tareas activas: ").Append(carga.TareasActivas).Append("\n");
        texto.Append("Estado: ").Append(ClasificarCarga(carga.CargaActual, carga.CapacidadSemanal)).Append("\n");
        AgregarTareas(texto, tareas, proyectos, false, false);
        return texto.ToString();
    }

    private string ConsultarPendientes(Usuario usuario)
    {
        ValidarEmpleado(usuario);
        List<Tarea> tareas = _tareaBLL.ConsultarAsignadas(usuario)
            .Where(tarea => tarea.Activo && !EstaFinalizada(tarea))
            .OrderBy(tarea => PrioridadOrden(tarea.Prioridad))
            .ThenBy(tarea => tarea.Deadline ?? DateTime.MaxValue)
            .ToList();

        StringBuilder texto = new StringBuilder("Tus tareas pendientes\n");
        AgregarTareas(texto, tareas, ConsultarNombresProyectos(usuario), true, false);
        return texto.ToString();
    }

    private string ConsultarCompletadas(Usuario usuario)
    {
        List<Tarea> tareas = ConsultarTareasSegunAlcance(usuario)
            .Where(tarea => EstaFinalizada(tarea) && tarea.FechaFinReal.HasValue && tarea.FechaFinReal.Value.Date == DateTime.Today)
            .OrderByDescending(tarea => tarea.FechaFinReal)
            .ToList();

        StringBuilder texto = new StringBuilder("Tareas completadas hoy\n");
        AgregarTareas(texto, tareas, ConsultarNombresProyectos(usuario), false, true);
        return texto.ToString();
    }

    private string ConsultarCriticas(Usuario usuario)
    {
        List<Tarea> tareas = ConsultarTareasSegunAlcance(usuario)
            .Where(EsCritica)
            .OrderBy(tarea => tarea.Deadline ?? DateTime.MaxValue)
            .ToList();

        StringBuilder texto = new StringBuilder("Tareas que requieren atención\n");
        AgregarTareas(texto, tareas, ConsultarNombresProyectos(usuario), false, false);
        return texto.ToString();
    }

    private string ConsultarEquipo(Usuario usuario)
    {
        ValidarPM(usuario);
        List<(Empleado Empleado, decimal CargaActual, decimal CapacidadSemanal, int TareasActivas)> equipo = _slackMPP.ConsultarEquipoPM(usuario);
        int sobrecargados = equipo.Count(item => item.CapacidadSemanal > 0 && item.CargaActual > item.CapacidadSemanal);
        int balanceados = equipo.Count(item => item.CapacidadSemanal > 0 && item.CargaActual <= item.CapacidadSemanal && CalcularOcupacion(item.CargaActual, item.CapacidadSemanal) >= 60);
        int disponibles = equipo.Count(item => item.CapacidadSemanal > 0 && CalcularOcupacion(item.CargaActual, item.CapacidadSemanal) < 60);

        return "Carga del equipo\n\nSobrecargados: " + sobrecargados + "\nBalanceados: " + balanceados + "\nCon disponibilidad: " + disponibles;
    }

    private List<Tarea> ConsultarTareasSegunAlcance(Usuario usuario)
    {
        if (EsPM(usuario))
        {
            ValidarPM(usuario);
            return _tareaBLL.ConsultarPorPM(usuario);
        }

        ValidarEmpleado(usuario);
        return _tareaBLL.ConsultarAsignadas(usuario);
    }

    private Dictionary<int, string> ConsultarNombresProyectos(Usuario usuario)
    {
        return _slackMPP.ConsultarProyectosAccesibles(usuario).ToDictionary(proyecto => proyecto.ID, proyecto => proyecto.Nombre);
    }

    private static void AgregarTareas(StringBuilder texto, List<Tarea> tareas, Dictionary<int, string> proyectos, bool incluirPrioridad, bool incluirFinalizacion)
    {
        if (tareas.Count == 0)
        {
            texto.Append("\nNo hay tareas para mostrar.");
            return;
        }

        foreach (Tarea tarea in tareas.Take(MaximoTareasMostradas))
        {
            string nombreProyecto = proyectos.TryGetValue(tarea.IdProyecto, out string? nombre) ? nombre : "Proyecto";
            texto.Append("\n• ").Append(nombreProyecto).Append(" | ").Append(tarea.Titulo).Append(" | ").Append(tarea.Estado ?? "Pendiente");
            if (incluirPrioridad)
            {
                texto.Append(" | ").Append(tarea.Prioridad ?? "Sin prioridad");
            }

            if (incluirFinalizacion)
            {
                texto.Append(" | ").Append(tarea.FechaFinReal.HasValue ? tarea.FechaFinReal.Value.ToString("dd/MM HH:mm") : "Sin horario");
            }
            else
            {
                texto.Append(" | ").Append(tarea.Deadline.HasValue ? tarea.Deadline.Value.ToString("dd/MM") : "Sin deadline");
            }
        }

        if (tareas.Count > MaximoTareasMostradas)
        {
            texto.Append("\n… y ").Append(tareas.Count - MaximoTareasMostradas).Append(" más.");
        }
    }

    private void ValidarPM(Usuario usuario)
    {
        if (!EsPM(usuario) || !_rolBLL.TienePermiso(usuario, "GestionarTareas"))
        {
            throw new UnauthorizedAccessException("Esta consulta sólo está disponible para Project Managers autorizados.");
        }
    }

    private void ValidarEmpleado(Usuario usuario)
    {
        if (!EsEmpleado(usuario) || !_rolBLL.TienePermiso(usuario, "VerDashboard"))
        {
            throw new UnauthorizedAccessException("Esta consulta sólo está disponible para empleados habilitados.");
        }
    }

    private static bool EsPM(Usuario usuario)
    {
        return usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "PM", StringComparison.OrdinalIgnoreCase));
    }

    private static bool EsEmpleado(Usuario usuario)
    {
        return usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "Empleado", StringComparison.OrdinalIgnoreCase));
    }

    private static bool EstaFinalizada(Tarea tarea)
    {
        return string.Equals(tarea.Estado, "Finalizada", StringComparison.OrdinalIgnoreCase) || string.Equals(tarea.Estado, "Finalizado", StringComparison.OrdinalIgnoreCase) || string.Equals(tarea.Estado, "Completada", StringComparison.OrdinalIgnoreCase) || string.Equals(tarea.Estado, "Completado", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EsCritica(Tarea tarea)
    {
        if (!tarea.Activo || EstaFinalizada(tarea))
        {
            return false;
        }

        bool prioridadAlta = string.Equals(tarea.Prioridad, "Urgente", StringComparison.OrdinalIgnoreCase) || string.Equals(tarea.Prioridad, "Alta", StringComparison.OrdinalIgnoreCase);
        bool proximaADeadline = tarea.Deadline.HasValue && tarea.Deadline.Value.Date <= DateTime.Today.AddDays(3);
        return prioridadAlta || tarea.Bloqueada || string.Equals(tarea.Estado, "Bloqueada", StringComparison.OrdinalIgnoreCase) || proximaADeadline;
    }

    private static decimal CalcularOcupacion(decimal cargaActual, decimal capacidadSemanal)
    {
        if (capacidadSemanal <= 0)
        {
            return cargaActual > 0 ? 100 : 0;
        }

        return Math.Round(cargaActual * 100 / capacidadSemanal, 1);
    }

    private static int PrioridadOrden(string? prioridad)
    {
        if (string.Equals(prioridad, "Urgente", StringComparison.OrdinalIgnoreCase)) return 0;
        if (string.Equals(prioridad, "Alta", StringComparison.OrdinalIgnoreCase)) return 1;
        if (string.Equals(prioridad, "Media", StringComparison.OrdinalIgnoreCase)) return 2;
        return 3;
    }

    private static string ClasificarCarga(decimal cargaActual, decimal capacidadSemanal)
    {
        decimal ocupacion = CalcularOcupacion(cargaActual, capacidadSemanal);
        if (capacidadSemanal <= 0) return "sin disponibilidad configurada";
        if (cargaActual > capacidadSemanal) return "sobrecarga";
        if (ocupacion >= 80) return "carga alta";
        return "disponible";
    }

    private static string NombreCompleto(Empleado empleado)
    {
        return (empleado.Nombre + " " + empleado.Apellido).Trim();
    }
}

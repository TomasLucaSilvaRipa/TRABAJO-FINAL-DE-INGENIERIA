using System.Text.Json;
using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLPlantillaTarea
{
    private readonly MPPPlantillaTarea _plantillaMPP;
    private readonly MPPTarea _tareaMPP;
    private readonly MPPRecursos _recursosMPP;
    private readonly BLLRol _rolBLL;
    private readonly BLLBitacora _bitacoraBLL;

    public BLLPlantillaTarea(MPPPlantillaTarea plantillaMPP, MPPTarea tareaMPP, MPPRecursos recursosMPP, BLLRol rolBLL, BLLBitacora bitacoraBLL)
    {
        _plantillaMPP = plantillaMPP;
        _tareaMPP = tareaMPP;
        _recursosMPP = recursosMPP;
        _rolBLL = rolBLL;
        _bitacoraBLL = bitacoraBLL;
    }

    public List<PlantillaTarea> Consultar(Usuario usuario, bool incluirInactivas = false)
    {
        try
        {
            ValidarGestion(usuario);
            return _plantillaMPP.Consultar(usuario, incluirInactivas);
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
    }

    public List<Tarea> ConsultarTareasBase(Usuario usuario)
    {
        try
        {
            ValidarGestion(usuario);
            return _tareaMPP.ConsultarPorPM(usuario).Where(tarea => tarea.Activo).ToList();
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
    }

    public PlantillaTarea CrearDesdeTarea(Tarea tarea, Usuario usuario)
    {
        try
        {
            ValidarGestion(usuario);
            Tarea? tareaBase = _tareaMPP.ConsultarPorPM(usuario).FirstOrDefault(item => item.ID == tarea.ID && item.Activo);

            if (tareaBase is null)
            {
                throw new ArgumentException("La tarea seleccionada no pertenece a uno de tus proyectos activos.");
            }

            return new PlantillaTarea
            {
                IdSkillRequerido = tareaBase.IdSkillRequerido,
                Nombre = $"Plantilla de {tareaBase.Titulo}",
                TituloSugerido = tareaBase.Titulo,
                DescripcionBase = tareaBase.Descripcion,
                HorasEstimadas = tareaBase.HorasEstimadas,
                Complejidad = tareaBase.Complejidad,
                PrioridadSugerida = tareaBase.Prioridad,
                SeniorityRecomendado = tareaBase.SeniorityRequerido,
                ChecklistBaseJson = tareaBase.ChecklistJson,
                ArchivosAdjuntosJson = tareaBase.ArchivosAdjuntosJson
            };
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (ArgumentException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
    }

    public PlantillaTarea Guardar(PlantillaTarea plantilla, Usuario usuario)
    {
        try
        {
            ValidarGestion(usuario);
            ValidarPlantilla(plantilla);

            Skill? skill = _recursosMPP.ConsultarSkills().FirstOrDefault(item => item.ID == plantilla.IdSkillRequerido && item.Activo);
            if (skill is null)
            {
                throw new ArgumentException("Seleccioná una skill activa para la plantilla.");
            }

            plantilla.Nombre = plantilla.Nombre.Trim();
            plantilla.TituloSugerido = plantilla.TituloSugerido?.Trim();
            plantilla.DescripcionBase = string.IsNullOrWhiteSpace(plantilla.DescripcionBase) ? null : plantilla.DescripcionBase.Trim();
            plantilla.SkillRequerido = skill.Nombre;

            PlantillaTarea resultado = _plantillaMPP.Guardar(plantilla, usuario);
            string accion = plantilla.ID == 0 ? "Crear plantilla de tarea" : "Modificar plantilla de tarea";
            string mensaje = plantilla.ID == 0
                ? $"Se creó la plantilla {resultado.Nombre}."
                : $"Se modificó la plantilla {resultado.Nombre}.";
            _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "PlantillaTarea", resultado.ID, accion, mensaje, "Exitoso", "Información", "Proyectos"));
            return resultado;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (ArgumentException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
    }

    public bool DarBaja(PlantillaTarea plantilla, Usuario usuario)
    {
        try
        {
            ValidarGestion(usuario);
            if (plantilla.ID <= 0 || string.IsNullOrWhiteSpace(plantilla.MotivoBaja))
            {
                throw new ArgumentException("Indicá la plantilla y el motivo de la baja.");
            }

            plantilla.MotivoBaja = plantilla.MotivoBaja.Trim();
            bool resultado = _plantillaMPP.DarBaja(plantilla, usuario);
            _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "PlantillaTarea", plantilla.ID, "Dar de baja plantilla de tarea", $"Se dio de baja una plantilla. Motivo: {plantilla.MotivoBaja}", "Exitoso", "Información", "Proyectos"));
            return resultado;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (ArgumentException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
    }

    private void ValidarGestion(Usuario usuario)
    {
        if (!string.Equals(usuario.Rol.TipoUsuario, "PM", StringComparison.OrdinalIgnoreCase) || !_rolBLL.TienePermiso(usuario, "GestionarTareas"))
        {
            throw new UnauthorizedAccessException("Sólo un PM con permiso de gestión de tareas puede administrar plantillas.");
        }

        if (!usuario.IdAgencia.HasValue)
        {
            throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.");
        }
    }

    private static void ValidarPlantilla(PlantillaTarea plantilla)
    {
        if (string.IsNullOrWhiteSpace(plantilla.Nombre) || string.IsNullOrWhiteSpace(plantilla.TituloSugerido))
        {
            throw new ArgumentException("Completá el nombre de la plantilla y el título sugerido.");
        }

        if (!plantilla.IdSkillRequerido.HasValue || plantilla.IdSkillRequerido <= 0 || !plantilla.HorasEstimadas.HasValue || plantilla.HorasEstimadas <= 0)
        {
            throw new ArgumentException("Indicá una skill requerida y horas estimadas mayores a cero.");
        }

        ValidarJson(plantilla.ChecklistBaseJson, "el checklist base");
        ValidarJson(plantilla.ArchivosAdjuntosJson, "los archivos modelo");
    }

    private static void ValidarJson(string? json, string campo)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            JsonDocument documento = JsonDocument.Parse(json);
            if (documento.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException($"El formato de {campo} no es válido.");
            }
        }
        catch (JsonException)
        {
            throw new ArgumentException($"El formato de {campo} no es válido.");
        }
    }
}

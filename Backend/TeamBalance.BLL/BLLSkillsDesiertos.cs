using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLSkillsDesiertos
{
    private readonly MPPSkillsDesiertos _skillsMPP;
    private readonly BLLRol _rolBLL;
    private readonly BLLBitacora _bitacoraBLL;

    public BLLSkillsDesiertos(MPPSkillsDesiertos skillsMPP, BLLRol rolBLL, BLLBitacora bitacoraBLL)
    {
        _skillsMPP = skillsMPP; _rolBLL = rolBLL; _bitacoraBLL = bitacoraBLL;
    }

    public List<AnalisisCoberturaSkill> AnalizarCobertura(FiltroCoberturaSkill filtro, Usuario usuario, bool registrarConsulta = true)
    {
        ValidarAcceso(usuario); NormalizarFiltro(filtro);
        List<TareaCoberturaSkill> tareas = _skillsMPP.ConsultarTareas(filtro, usuario);
        List<EmpleadoCoberturaSkill> empleados = _skillsMPP.ConsultarEmpleados(usuario);
        empleados.ForEach(empleado => empleado.HorasDisponibles = empleado.HorasSemanales - empleado.CargaActual);
        List<AnalisisCoberturaSkill> resultados = ProcesarCobertura(tareas, empleados);
        if (registrarConsulta)
        {
            string alcance = $"Del {filtro.FechaDesde:dd/MM/yyyy} al {filtro.FechaHasta:dd/MM/yyyy}" + (filtro.IdProyecto.HasValue ? $"; proyecto {filtro.IdProyecto}" : string.Empty) + (filtro.IdCliente.HasValue ? $"; cliente {filtro.IdCliente}" : string.Empty);
            _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Skill", null, "Analizar skills desiertos", $"Se consultó el ranking de cobertura de skills. {alcance}.", "Exitoso", "Información", "Recursos"));
        }
        return OrdenarRanking(resultados);
    }

    public AnalisisCoberturaSkill ConsultarDetalle(int idSkill, FiltroCoberturaSkill filtro, Usuario usuario)
    {
        if (idSkill <= 0) { throw new ArgumentException("Seleccioná una skill válida."); }
        AnalisisCoberturaSkill resultado = AnalizarCobertura(filtro, usuario, false).FirstOrDefault(item => item.IdSkill == idSkill) ?? throw new KeyNotFoundException("La skill no pertenece al alcance seleccionado.");
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Skill", idSkill, "Consultar detalle de cobertura", $"Se consultó el detalle de la skill {resultado.NombreSkill}.", "Exitoso", "Información", "Recursos"));
        return resultado;
    }

    public bool RegistrarRecomendacion(RecomendacionSkill recomendacion, Usuario usuario)
    {
        ValidarAcceso(usuario);
        if (recomendacion.IdSkill <= 0 || string.IsNullOrWhiteSpace(recomendacion.TipoAccion) || string.IsNullOrWhiteSpace(recomendacion.Observacion)) { throw new ArgumentException("Indicá la skill, la acción sugerida y una observación."); }
        string[] accionesValidas = ["Capacitación", "Contratación", "Revisión de asignaciones"];
        if (!accionesValidas.Contains(recomendacion.TipoAccion)) { throw new ArgumentException("La acción seleccionada no es válida."); }
        recomendacion.Observacion = recomendacion.Observacion.Trim();
        bool resultado = _skillsMPP.RegistrarRecomendacion(recomendacion, usuario);
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Skill", recomendacion.IdSkill, "Registrar recomendación de skill", $"Se registró una recomendación de {recomendacion.TipoAccion}.", "Exitoso", "Información", "Recursos"));
        return resultado;
    }

    public List<SugerenciaEmpleadoSkill> SugerirEmpleados(ConsultaSugerenciasSkill consulta, Usuario usuario)
    {
        ValidarAcceso(usuario);
        if (consulta.IdSkill <= 0 || consulta.IdProyecto <= 0) { throw new ArgumentException("Seleccioná una skill y un proyecto válidos."); }
        FiltroCoberturaSkill filtro = new() { IdProyecto = consulta.IdProyecto, FechaDesde = DateTime.Today, FechaHasta = DateTime.Today.AddDays(90) };
        AnalisisCoberturaSkill analisis = AnalizarCobertura(filtro, usuario, false).FirstOrDefault(item => item.IdSkill == consulta.IdSkill)
            ?? throw new KeyNotFoundException("La skill no tiene demanda activa en el proyecto seleccionado.");

        List<SugerenciaEmpleadoSkill> candidatos = _skillsMPP.ConsultarSugerencias(consulta, usuario);
        List<SugerenciaEmpleadoSkill> internos = candidatos.Where(candidato => candidato.EsDelProyecto && (candidato.TieneSkillObjetivo || candidato.CantidadSkillsMismaArea > 0)).ToList();
        List<SugerenciaEmpleadoSkill> alcance = internos.Count > 0
            ? internos
            : candidatos.Where(candidato => candidato.TieneSkillObjetivo || candidato.CantidadSkillsMismaArea > 0).ToList();

        foreach (SugerenciaEmpleadoSkill candidato in alcance)
        {
            decimal afinidadTecnica = candidato.TieneSkillObjetivo ? 50m : Math.Min(50m, candidato.CantidadSkillsMismaArea * 25m);
            decimal disponibilidad = candidato.HorasSemanales <= 0 ? 0 : Math.Clamp(candidato.HorasDisponibles * 25m / candidato.HorasSemanales, 0, 25);
            decimal seniority = candidato.Seniority.ToLowerInvariant() switch { "senior" => 15m, "semisenior" => 11m, "junior" => 7m, _ => 5m };
            decimal contexto = candidato.EsDelProyecto ? 10m : 0m;
            candidato.PuntajeAfinidad = decimal.Round(afinidadTecnica + disponibilidad + seniority + contexto, 1);
            candidato.TipoSugerencia = candidato.TieneSkillObjetivo ? "Cobertura inmediata" : "Capacitación sugerida";
            candidato.Motivo = CrearMotivo(candidato);
        }

        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Skill", consulta.IdSkill, "Sugerir empleados para skill", $"Se generaron candidatos para cubrir {analisis.NombreSkill} en el proyecto {consulta.IdProyecto}.", "Exitoso", "Información", "Recursos"));
        return alcance.OrderByDescending(candidato => candidato.PuntajeAfinidad).ThenByDescending(candidato => candidato.TieneSkillObjetivo).ThenByDescending(candidato => candidato.HorasDisponibles).ToList();
    }

    private List<AnalisisCoberturaSkill> ProcesarCobertura(List<TareaCoberturaSkill> tareas, List<EmpleadoCoberturaSkill> empleados)
    {
        return tareas.GroupBy(tarea => new { tarea.IdSkill, tarea.NombreSkill, tarea.CategoriaSkill }).Select(grupo =>
        {
            List<TareaCoberturaSkill> tareasSkill = grupo.ToList();
            List<EmpleadoCoberturaSkill> empleadosSkill = empleados.Where(empleado => empleado.IdSkill == grupo.Key.IdSkill).ToList();
            decimal demanda = tareasSkill.Sum(tarea => tarea.HorasEstimadas);
            decimal capacidadTotal = empleadosSkill.Sum(empleado => empleado.HorasSemanales * FactorNivel(empleado.NivelSkill));
            decimal capacidadDisponible = empleadosSkill.Sum(empleado => Math.Max(0, empleado.HorasDisponibles) * FactorNivel(empleado.NivelSkill));
            DateTime deadline = tareasSkill.Min(tarea => tarea.Deadline);
            (string tipo, string criticidad, int puntaje) = Clasificar(demanda, capacidadTotal, capacidadDisponible, empleadosSkill.Count, deadline);
            return new AnalisisCoberturaSkill
            {
                IdSkill = grupo.Key.IdSkill, NombreSkill = grupo.Key.NombreSkill, CategoriaSkill = grupo.Key.CategoriaSkill, DemandaHoras = demanda,
                CapacidadDisponibleHoras = decimal.Round(capacidadDisponible, 2), CapacidadTotalHoras = decimal.Round(capacidadTotal, 2),
                PorcentajeCobertura = demanda == 0 ? 100 : decimal.Round(Math.Min(100, capacidadDisponible * 100 / demanda), 2), CantidadTareasAfectadas = tareasSkill.Count,
                CantidadProyectosAfectados = tareasSkill.Select(tarea => tarea.IdProyecto).Distinct().Count(), TipoBrecha = tipo, Criticidad = criticidad, PuntajeCriticidad = puntaje,
                DeadlineMasProximo = deadline, AccionSugerida = SugerirAccion(tipo), TareasAfectadas = tareasSkill, EmpleadosRelacionados = empleadosSkill
            };
        }).ToList();
    }

    private static List<AnalisisCoberturaSkill> OrdenarRanking(List<AnalisisCoberturaSkill> resultados) => resultados.OrderByDescending(item => item.PuntajeCriticidad).ThenBy(item => item.DeadlineMasProximo).ThenByDescending(item => item.DemandaHoras).ToList();
    private static decimal FactorNivel(string nivel) => nivel.ToLowerInvariant() switch { "junior" or "básico" or "basico" => .70m, "avanzado" => 1.15m, "experto" => 1.30m, _ => 1m };
    private static (string tipo, string criticidad, int puntaje) Clasificar(decimal demanda, decimal total, decimal disponible, int cantidadEmpleados, DateTime deadline)
    {
        int urgencia = deadline.Date <= DateTime.Today.AddDays(7) ? 25 : deadline.Date <= DateTime.Today.AddDays(14) ? 12 : 0;
        if (cantidadEmpleados == 0 || disponible == 0) { return ("Skill desierto", "Crítica", 100 + urgencia); }
        if (disponible < demanda)
        {
            decimal cobertura = demanda == 0 ? 1 : disponible / demanda;
            return ("Cobertura insuficiente", cobertura < .60m ? "Crítica" : "Alta", (cobertura < .60m ? 85 : 70) + urgencia);
        }
        if (total > 0 && demanda / total >= .85m) { return ("Skill saturado", "Media", 50 + urgencia); }
        return ("Cobertura suficiente", "Baja", 10);
    }
    private static string SugerirAccion(string tipo) => tipo switch { "Skill desierto" => "Contratación", "Cobertura insuficiente" => "Capacitación", "Skill saturado" => "Revisión de asignaciones", _ => "Revisión de asignaciones" };
    private static string CrearMotivo(SugerenciaEmpleadoSkill candidato)
    {
        string alcance = candidato.EsDelProyecto ? "Ya participa en este proyecto" : "Proviene de otro proyecto de la agencia";
        string capacidad = $"{decimal.Round(Math.Max(0, candidato.HorasDisponibles), 1)} h disponibles";
        string afinidad = candidato.TieneSkillObjetivo ? "ya domina la skill requerida" : $"tiene {candidato.CantidadSkillsMismaArea} skill(s) de la misma área: {candidato.SkillsRelacionadas}";
        return $"{alcance}; {afinidad}; {capacidad}.";
    }
    private void ValidarAcceso(Usuario usuario)
    {
        if (!usuario.IdAgencia.HasValue) { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); }
        if (!_rolBLL.TienePermiso(usuario, "AnalizarSkills")) { throw new UnauthorizedAccessException("No tenés permiso para analizar cobertura de skills."); }
    }
    private static void NormalizarFiltro(FiltroCoberturaSkill filtro)
    {
        filtro.FechaDesde ??= DateTime.Today; filtro.FechaHasta ??= DateTime.Today.AddDays(90);
        if (filtro.FechaHasta.Value.Date < filtro.FechaDesde.Value.Date) { throw new ArgumentException("La fecha final no puede ser anterior a la inicial."); }
    }
}

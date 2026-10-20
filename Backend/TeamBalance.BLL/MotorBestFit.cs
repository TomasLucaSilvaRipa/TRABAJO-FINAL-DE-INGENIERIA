using System.Globalization;
using System.Text;
using TeamBalance.BE.Entidades;

namespace TeamBalance.BLL;

public class MotorBestFit
{
    public List<RecomendacionBestFit> GenerarRanking(Tarea tarea, List<Empleado> empleados)
    {
        decimal horasPendientes = CalcularHorasPendientes(tarea);
        decimal horasRequeridasPorSemana = CalcularHorasRequeridasPorSemana(tarea, horasPendientes);
        string seniorityRequerido = ObtenerSeniorityRequerido(tarea);
        List<RecomendacionBestFit> recomendaciones = new List<RecomendacionBestFit>();

        foreach (Empleado empleado in empleados)
        {
            if (!CumpleSeniorityMinimo(seniorityRequerido, empleado.Seniority)) { continue; }

            decimal horasDisponibles = empleado.HorasDisponiblesSemanales;
            decimal horasComprometidas = Math.Max(0, empleado.HorasComprometidasSemanales);
            decimal margen = horasDisponibles - horasComprometidas - horasRequeridasPorSemana;
            if (margen < 0) { continue; }

            decimal puntajeSkill = CalcularPuntajeSkill(empleado);
            decimal puntajeSeniority = CalcularPuntajeSeniority(seniorityRequerido, empleado.Seniority);
            decimal puntajeCapacidad = CalcularPuntajeCapacidad(horasRequeridasPorSemana, margen);
            decimal puntajeCarga = CalcularPuntajeCarga(horasDisponibles, horasComprometidas);
            decimal puntajeHistorial = CalcularPuntajeHistorial(empleado.CantidadTareasHistoricasComparables, empleado.RatioHistoricoConsumo);
            decimal cargaActual = horasDisponibles <= 0 ? 100 : Math.Min(100, Math.Round((horasComprometidas / horasDisponibles) * 100, 2));
            decimal puntaje = Math.Round(puntajeSkill + puntajeSeniority + puntajeCapacidad + puntajeCarga + puntajeHistorial, 2);

            RecomendacionBestFit recomendacion = new RecomendacionBestFit();
            recomendacion.IdTarea = tarea.ID;
            recomendacion.IdEmpleadoSugerido = empleado.ID;
            recomendacion.PuntajeCompatibilidad = puntaje;
            recomendacion.FechaGeneracion = DateTime.Now;
            recomendacion.Estado = "Generada";
            recomendacion.Activo = true;
            recomendacion.NombreEmpleado = string.Join(" ", new string[] { empleado.Nombre, empleado.Apellido }.Where(valor => !string.IsNullOrWhiteSpace(valor))).Trim();
            recomendacion.SeniorityEmpleado = empleado.Seniority;
            recomendacion.HorasDisponibles = horasDisponibles;
            recomendacion.HorasComprometidasSemanales = horasComprometidas;
            recomendacion.CargaActualPorcentaje = cargaActual;
            recomendacion.MargenHorasSemanales = Math.Round(margen, 2);
            recomendacion.PuntajeSkill = puntajeSkill;
            recomendacion.PuntajeSeniority = puntajeSeniority;
            recomendacion.PuntajeCapacidad = puntajeCapacidad;
            recomendacion.PuntajeCargaActual = puntajeCarga;
            recomendacion.PuntajeHistorial = puntajeHistorial;
            recomendacion.CantidadTareasHistoricasComparables = empleado.CantidadTareasHistoricasComparables;
            recomendacion.DetalleCompatibilidad = CrearDetalleCompatibilidad(puntajeSkill, puntajeSeniority, puntajeCapacidad, puntajeCarga, puntajeHistorial);
            recomendacion.Fortalezas = CrearFortalezas(empleado, margen, horasRequeridasPorSemana);
            recomendacion.Advertencias = CrearAdvertencias(empleado, margen, horasRequeridasPorSemana);
            recomendaciones.Add(recomendacion);
        }

        return recomendaciones.OrderByDescending(item => item.PuntajeCompatibilidad).ThenByDescending(item => item.MargenHorasSemanales).ThenBy(item => item.CargaActualPorcentaje).ThenBy(item => item.NombreEmpleado).ToList();
    }

    private static decimal CalcularHorasPendientes(Tarea tarea)
    {
        decimal avance = Math.Clamp(tarea.PorcentajeAvance, 0, 100);
        return Math.Max(0, Math.Round(tarea.HorasEstimadas * (100 - avance) / 100, 2));
    }

    private static string ObtenerSeniorityRequerido(Tarea tarea)
    {
        string complejidad = Normalizar(tarea.Complejidad);
        if (complejidad.Contains("alta")) { return "Senior"; }
        if (complejidad.Contains("media")) { return "Semi Senior"; }
        return "Junior";
    }

    private static decimal CalcularHorasRequeridasPorSemana(Tarea tarea, decimal horasPendientes)
    {
        int diasRestantes = tarea.Deadline.HasValue ? Math.Max(0, (tarea.Deadline.Value.Date - DateTime.Today).Days) : 0;
        decimal semanasDisponibles = Math.Max(1, (decimal)Math.Ceiling(diasRestantes / 7m));
        return Math.Round(horasPendientes / semanasDisponibles, 2);
    }

    private static decimal CalcularPuntajeSkill(Empleado empleado)
    {
        EmpleadoSkill? skill = empleado.EmpleadoSkills.FirstOrDefault(item => item.Activo);
        string nivel = Normalizar(skill?.Nivel);
        if (nivel.Contains("experto") || nivel.Contains("avanzado")) { return 30; }
        if (nivel.Contains("intermedio") || nivel.Contains("medio")) { return 26; }
        return 22;
    }

    private static decimal CalcularPuntajeSeniority(string? seniorityRequerido, string? seniorityEmpleado)
    {
        int diferencia = ObtenerNivelSeniority(seniorityEmpleado) - ObtenerNivelSeniority(seniorityRequerido);
        if (diferencia == 0) { return 20; }
        if (diferencia == 1) { return 17; }
        return 14;
    }

    private static decimal CalcularPuntajeCapacidad(decimal horasRequeridasPorSemana, decimal margen)
    {
        if (horasRequeridasPorSemana <= 0) { return 25; }
        if (margen >= horasRequeridasPorSemana) { return 25; }
        return Math.Round(15 + (margen / horasRequeridasPorSemana) * 10, 2);
    }

    private static decimal CalcularPuntajeCarga(decimal horasDisponibles, decimal horasComprometidas)
    {
        if (horasDisponibles <= 0) { return 0; }
        decimal proporcionCarga = Math.Clamp(horasComprometidas / horasDisponibles, 0, 1);
        return Math.Round(15 * (1 - proporcionCarga), 2);
    }

    private static decimal CalcularPuntajeHistorial(int cantidadTareas, decimal ratioConsumo)
    {
        if (cantidadTareas < 3) { return 5; }
        if (ratioConsumo <= 1) { return 10; }
        return Math.Round(10 * Math.Max(0, 1 - ((ratioConsumo - 1) / 0.5m)), 2);
    }

    private static bool CumpleSeniorityMinimo(string? seniorityRequerido, string? seniorityEmpleado)
    {
        return ObtenerNivelSeniority(seniorityEmpleado) >= ObtenerNivelSeniority(seniorityRequerido);
    }

    private static int ObtenerNivelSeniority(string? seniority)
    {
        string valor = Normalizar(seniority);
        if (valor.Contains("lead") || valor.Contains("lider")) { return 4; }
        if (valor.Contains("senior")) { return 3; }
        if (valor.Contains("semi") || valor.Contains("ssr")) { return 2; }
        return 1;
    }

    private static string CrearDetalleCompatibilidad(decimal skill, decimal seniority, decimal capacidad, decimal carga, decimal historial)
    {
        return $"Skill {skill}/30 · Seniority {seniority}/20 · Capacidad {capacidad}/25 · Carga {carga}/15 · Historial {historial}/10";
    }

    private static string CrearFortalezas(Empleado empleado, decimal margen, decimal horasRequeridasPorSemana)
    {
        List<string> fortalezas = new List<string>();
        fortalezas.Add("Cuenta con la skill requerida.");
        if (margen >= horasRequeridasPorSemana) { fortalezas.Add("Tiene margen operativo holgado para cumplir el deadline."); }
        if (empleado.CantidadTareasHistoricasComparables >= 3 && empleado.RatioHistoricoConsumo <= 1) { fortalezas.Add("Su historial comparable se mantuvo dentro de las horas estimadas."); }
        return string.Join(" ", fortalezas);
    }

    private static string CrearAdvertencias(Empleado empleado, decimal margen, decimal horasRequeridasPorSemana)
    {
        List<string> advertencias = new List<string>();
        if (margen < horasRequeridasPorSemana) { advertencias.Add("El margen operativo es limitado para absorber imprevistos."); }
        if (empleado.CantidadTareasHistoricasComparables < 3) { advertencias.Add("Hay poca evidencia histórica comparable; se aplica un valor neutral."); }
        if (empleado.RatioHistoricoConsumo > 1.15m) { advertencias.Add("En tareas comparables registró más horas que las estimadas."); }
        return string.Join(" ", advertencias);
    }

    private static string Normalizar(string? valor)
    {
        string texto = valor?.Trim().ToLowerInvariant() ?? string.Empty;
        string textoNormalizado = texto.Normalize(NormalizationForm.FormD);
        return string.Concat(textoNormalizado.Where(caracter => CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark));
    }
}

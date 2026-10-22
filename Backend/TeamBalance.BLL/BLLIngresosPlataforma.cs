using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public sealed class BLLIngresosPlataforma
{
    private readonly MPPIngresosPlataforma _ingresosMPP;
    private readonly BLLRol _rolBLL;

    public BLLIngresosPlataforma(MPPIngresosPlataforma ingresosMPP, BLLRol rolBLL)
    {
        _ingresosMPP = ingresosMPP;
        _rolBLL = rolBLL;
    }

    public ResumenIngresosPlataforma ConsultarResumen(Usuario solicitante)
    {
        ValidarAccesoFinanciero(solicitante);
        DateTime fechaCorte = DateTime.Today;
        DateTime inicioSemana = fechaCorte.AddDays(-((int)fechaCorte.DayOfWeek + 6) % 7);
        List<CobroPlataformaConfirmado> cobros = _ingresosMPP.ConsultarCobrosConfirmados()
            .Where(cobro => cobro.FechaCobro.Date <= fechaCorte)
            .ToList();

        List<ResumenIngresosMoneda> monedas = cobros
                .GroupBy(cobro => string.IsNullOrWhiteSpace(cobro.Moneda) ? "Sin moneda" : cobro.Moneda.Trim().ToUpperInvariant())
                .Select(grupo => CrearResumenMoneda(grupo.Key, grupo.ToList(), fechaCorte, inicioSemana))
                .OrderBy(resumen => resumen.Moneda)
                .ToList();
        return new ResumenIngresosPlataforma(fechaCorte, monedas);
    }

    private static ResumenIngresosMoneda CrearResumenMoneda(string moneda, List<CobroPlataformaConfirmado> cobros, DateTime fechaCorte, DateTime inicioSemana)
    {
        List<IngresosPorAgencia> agencias = cobros
                .GroupBy(cobro => cobro.IdAgencia)
                .Select(grupo => new IngresosPorAgencia(grupo.Key, grupo.First().Agencia, SumarPeriodo(grupo, cobro => cobro.FechaCobro.Date == fechaCorte), SumarPeriodo(grupo, cobro => cobro.FechaCobro.Date >= inicioSemana && cobro.FechaCobro.Date <= fechaCorte), SumarPeriodo(grupo, cobro => cobro.FechaCobro.Year == fechaCorte.Year && cobro.FechaCobro.Month == fechaCorte.Month), SumarPeriodo(grupo, cobro => cobro.FechaCobro.Year == fechaCorte.Year), grupo.Sum(cobro => cobro.Importe), grupo.Count()))
                .OrderByDescending(agencia => agencia.Anio)
                .ThenByDescending(agencia => agencia.TotalHistorico)
                .ThenBy(agencia => agencia.Agencia)
                .ToList();
        return new ResumenIngresosMoneda(moneda, SumarPeriodo(cobros, cobro => cobro.FechaCobro.Date == fechaCorte), SumarPeriodo(cobros, cobro => cobro.FechaCobro.Date >= inicioSemana && cobro.FechaCobro.Date <= fechaCorte), SumarPeriodo(cobros, cobro => cobro.FechaCobro.Year == fechaCorte.Year && cobro.FechaCobro.Month == fechaCorte.Month), SumarPeriodo(cobros, cobro => cobro.FechaCobro.Year == fechaCorte.Year), cobros.Sum(cobro => cobro.Importe), cobros.Count, agencias);
    }

    private static decimal SumarPeriodo(IEnumerable<CobroPlataformaConfirmado> cobros, Func<CobroPlataformaConfirmado, bool> condicion)
        => cobros.Where(condicion).Sum(cobro => cobro.Importe);

    private void ValidarAccesoFinanciero(Usuario usuario)
    {
        bool esOperadorInterno = usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "Soporte", StringComparison.OrdinalIgnoreCase));
        if (!esOperadorInterno || !_rolBLL.TienePermiso(usuario, "ConsultarIngresosPlataforma"))
        {
            throw new UnauthorizedAccessException("No tenés permiso para consultar los ingresos de TeamBalance.");
        }
    }
}

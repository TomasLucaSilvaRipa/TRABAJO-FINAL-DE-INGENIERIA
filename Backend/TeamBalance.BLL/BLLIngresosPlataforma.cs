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

        return new ResumenIngresosPlataforma
        {
            FechaCorte = fechaCorte,
            Monedas = cobros
                .GroupBy(cobro => string.IsNullOrWhiteSpace(cobro.Moneda) ? "Sin moneda" : cobro.Moneda.Trim().ToUpperInvariant())
                .Select(grupo => CrearResumenMoneda(grupo.Key, grupo.ToList(), fechaCorte, inicioSemana))
                .OrderBy(resumen => resumen.Moneda)
                .ToList()
        };
    }

    private static ResumenIngresosMoneda CrearResumenMoneda(string moneda, List<CobroPlataformaConfirmado> cobros, DateTime fechaCorte, DateTime inicioSemana)
    {
        return new ResumenIngresosMoneda
        {
            Moneda = moneda,
            Hoy = SumarPeriodo(cobros, cobro => cobro.FechaCobro.Date == fechaCorte),
            Semana = SumarPeriodo(cobros, cobro => cobro.FechaCobro.Date >= inicioSemana && cobro.FechaCobro.Date <= fechaCorte),
            Mes = SumarPeriodo(cobros, cobro => cobro.FechaCobro.Year == fechaCorte.Year && cobro.FechaCobro.Month == fechaCorte.Month),
            Anio = SumarPeriodo(cobros, cobro => cobro.FechaCobro.Year == fechaCorte.Year),
            TotalHistorico = cobros.Sum(cobro => cobro.Importe),
            CobrosHistoricos = cobros.Count,
            Agencias = cobros
                .GroupBy(cobro => new { cobro.IdAgencia, cobro.Agencia })
                .Select(grupo => new IngresosPorAgencia
                {
                    IdAgencia = grupo.Key.IdAgencia,
                    Agencia = grupo.Key.Agencia,
                    Hoy = SumarPeriodo(grupo, cobro => cobro.FechaCobro.Date == fechaCorte),
                    Semana = SumarPeriodo(grupo, cobro => cobro.FechaCobro.Date >= inicioSemana && cobro.FechaCobro.Date <= fechaCorte),
                    Mes = SumarPeriodo(grupo, cobro => cobro.FechaCobro.Year == fechaCorte.Year && cobro.FechaCobro.Month == fechaCorte.Month),
                    Anio = SumarPeriodo(grupo, cobro => cobro.FechaCobro.Year == fechaCorte.Year),
                    TotalHistorico = grupo.Sum(cobro => cobro.Importe),
                    CobrosHistoricos = grupo.Count()
                })
                .OrderByDescending(agencia => agencia.Anio)
                .ThenByDescending(agencia => agencia.TotalHistorico)
                .ThenBy(agencia => agencia.Agencia)
                .ToList()
        };
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

namespace TeamBalance.BE.Entidades;

public class ConfiguracionReporteEjecutivo
{
    public ConfiguracionReporteEjecutivo()
    {
        Filtro = new FiltroReporteEjecutivo();
    }

    public ConfiguracionReporteEjecutivo(string titulo, FiltroReporteEjecutivo filtro, bool incluirOcupacionOperativa, bool incluirRiesgoRetraso, bool incluirDesvioHoras, bool incluirEficienciaPerfiles, bool incluirConvenienciaOperativa, bool incluirTodasLasSecciones)
    {
        Titulo = titulo;
        Filtro = filtro;
        IncluirOcupacionOperativa = incluirOcupacionOperativa;
        IncluirRiesgoRetraso = incluirRiesgoRetraso;
        IncluirDesvioHoras = incluirDesvioHoras;
        IncluirEficienciaPerfiles = incluirEficienciaPerfiles;
        IncluirConvenienciaOperativa = incluirConvenienciaOperativa;
        IncluirTodasLasSecciones = incluirTodasLasSecciones;
    }

    public string Titulo { get; set; } = string.Empty;

    public FiltroReporteEjecutivo Filtro { get; set; }

    public bool IncluirOcupacionOperativa { get; set; }

    public bool IncluirRiesgoRetraso { get; set; }

    public bool IncluirDesvioHoras { get; set; }

    public bool IncluirEficienciaPerfiles { get; set; }

    public bool IncluirConvenienciaOperativa { get; set; }

    public bool IncluirTodasLasSecciones { get; set; }
}

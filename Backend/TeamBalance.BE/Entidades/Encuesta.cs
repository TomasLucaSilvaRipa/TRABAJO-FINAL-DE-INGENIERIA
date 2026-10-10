namespace TeamBalance.BE.Entidades;

public sealed class Encuesta
{
    public int ID { get; set; }
    public string TituloEs { get; set; } = string.Empty;
    public string TituloEn { get; set; } = string.Empty;
    public string DescripcionEs { get; set; } = string.Empty;
    public string DescripcionEn { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public bool Activo { get; set; }
    public int CantidadRespuestas { get; set; }
    public List<PreguntaEncuesta> Preguntas { get; set; } = new List<PreguntaEncuesta>();
}

public sealed class PreguntaEncuesta
{
    public int ID { get; set; }
    public int IdEncuesta { get; set; }
    public string EnunciadoEs { get; set; } = string.Empty;
    public string EnunciadoEn { get; set; } = string.Empty;
    public int Orden { get; set; }
    public List<OpcionEncuesta> Opciones { get; set; } = new List<OpcionEncuesta>();
}

public sealed class OpcionEncuesta
{
    public int ID { get; set; }
    public int IdPreguntaEncuesta { get; set; }
    public string TextoEs { get; set; } = string.Empty;
    public string TextoEn { get; set; } = string.Empty;
    public int Orden { get; set; }
}

public sealed class RespuestaEncuesta
{
    public int IdEncuesta { get; set; }
    public string IdentificadorParticipante { get; set; } = string.Empty;
    public List<RespuestaPreguntaEncuesta> Respuestas { get; set; } = new List<RespuestaPreguntaEncuesta>();
}

public sealed class RespuestaPreguntaEncuesta
{
    public int IdPreguntaEncuesta { get; set; }
    public int IdOpcionEncuesta { get; set; }
}

public sealed class ResultadoEncuesta
{
    public int IdEncuesta { get; set; }
    public string TituloEs { get; set; } = string.Empty;
    public string TituloEn { get; set; } = string.Empty;
    public int CantidadRespuestas { get; set; }
    public List<ResultadoPreguntaEncuesta> Preguntas { get; set; } = new List<ResultadoPreguntaEncuesta>();
}

public sealed class ResultadoPreguntaEncuesta
{
    public int IdPreguntaEncuesta { get; set; }
    public string EnunciadoEs { get; set; } = string.Empty;
    public string EnunciadoEn { get; set; } = string.Empty;
    public int Orden { get; set; }
    public List<ResultadoOpcionEncuesta> Opciones { get; set; } = new List<ResultadoOpcionEncuesta>();
}

public sealed class ResultadoOpcionEncuesta
{
    public int IdOpcionEncuesta { get; set; }
    public string TextoEs { get; set; } = string.Empty;
    public string TextoEn { get; set; } = string.Empty;
    public int Orden { get; set; }
    public int CantidadRespuestas { get; set; }
    public decimal Porcentaje { get; set; }
}

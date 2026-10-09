namespace TeamBalance.BE.Entidades;

public enum OrigenBandejaSoporte
{
    HelpDesk = 1,
    ConsultaPlan = 2
}

public sealed class BandejaSoporteItem
{
    public BandejaSoporteItem(ConsultaSoporteResumen consulta)
    {
        Origen = OrigenBandejaSoporte.HelpDesk;
        ID = consulta.ID;
        IdUsuario = consulta.IdUsuario;
        IdAgencia = consulta.IdAgencia;
        IdSuscripcion = consulta.IdSuscripcion;
        Categoria = consulta.Categoria;
        Asunto = consulta.Asunto;
        Descripcion = consulta.Descripcion;
        Estado = consulta.Estado;
        FechaCreacion = consulta.FechaCreacion;
        FechaActualizacion = consulta.FechaActualizacion;
        NombreSolicitante = consulta.NombreSolicitante;
        EmailSolicitante = consulta.EmailSolicitante;
        NombreAgencia = consulta.NombreAgencia;
        NombrePlan = consulta.NombrePlan;
        CantidadMensajes = consulta.CantidadMensajes;
    }

    public BandejaSoporteItem(ConsultaPlan consulta)
    {
        Origen = OrigenBandejaSoporte.ConsultaPlan;
        ID = consulta.ID;
        IdPlanComercial = consulta.IdPlanComercial;
        Categoria = "Consulta sobre plan";
        Asunto = "Consulta sobre plan";
        Descripcion = consulta.Consulta;
        Estado = consulta.Estado;
        FechaCreacion = consulta.FechaAlta;
        FechaActualizacion = consulta.FechaRespuesta ?? consulta.FechaAlta;
        NombreSolicitante = consulta.Nombre;
        EmailSolicitante = consulta.Email;
        NombrePlan = consulta.NombrePlan;
        Respuesta = consulta.Respuesta;
        FechaRespuesta = consulta.FechaRespuesta;
        NombreRespondedor = consulta.NombreRespondedor;
    }

    public OrigenBandejaSoporte Origen { get; set; }
    public int ID { get; set; }
    public int? IdUsuario { get; set; }
    public int? IdAgencia { get; set; }
    public int? IdSuscripcion { get; set; }
    public int? IdPlanComercial { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public string NombreSolicitante { get; set; } = string.Empty;
    public string? EmailSolicitante { get; set; }
    public string? NombreAgencia { get; set; }
    public string? NombrePlan { get; set; }
    public int CantidadMensajes { get; set; }
    public string? Respuesta { get; set; }
    public DateTime? FechaRespuesta { get; set; }
    public string? NombreRespondedor { get; set; }
}

public sealed class ConsultaSoporteResumen
{
    public int ID { get; set; }
    public int IdUsuario { get; set; }
    public int? IdAgencia { get; set; }
    public int? IdSuscripcion { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public string NombreSolicitante { get; set; } = string.Empty;
    public string? EmailSolicitante { get; set; }
    public string? NombreAgencia { get; set; }
    public string? NombrePlan { get; set; }
    public int CantidadMensajes { get; set; }
}

public sealed class MensajeSoporteDetalle
{
    public int ID { get; set; }
    public int IdUsuario { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public bool EsSoporte { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}

public sealed class CrearConsultaSoporteRequest
{
    public int? IdSuscripcion { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}

public sealed class EnviarMensajeSoporteRequest
{
    public int IdConsulta { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string? Estado { get; set; }
}

public sealed class CambiarEstadoConsultaRequest
{
    public int IdConsulta { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public sealed class CategoriaNoticia
{
    public int ID { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

public sealed class Noticia
{
    public int ID { get; set; }
    public int IdCategoriaNoticia { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public DateTime FechaPublicacion { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public bool Activo { get; set; }
    public bool DifusionEnviada { get; set; }
}

public sealed class GuardarNoticiaRequest
{
    public int IdCategoriaNoticia { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public DateTime? FechaPublicacion { get; set; }
    public DateTime? FechaVencimiento { get; set; }
}

public sealed class PreferenciasNewsletterRequest
{
    public List<int> CategoriasIds { get; set; } = new List<int>();
}

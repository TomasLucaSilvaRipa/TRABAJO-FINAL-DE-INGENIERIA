namespace TeamBalance.BE.Entidades;

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
    public string Categoria { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}

public sealed class EnviarMensajeSoporteRequest
{
    public string Mensaje { get; set; } = string.Empty;
    public string? Estado { get; set; }
}

public sealed class CambiarEstadoConsultaRequest
{
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
    public List<int> CategoriasIds { get; set; } = [];
}

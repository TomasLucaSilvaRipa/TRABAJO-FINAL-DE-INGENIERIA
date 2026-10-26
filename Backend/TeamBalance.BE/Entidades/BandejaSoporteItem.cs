namespace TeamBalance.BE.Entidades;
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

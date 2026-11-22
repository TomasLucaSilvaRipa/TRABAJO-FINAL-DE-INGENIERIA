using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class BandejaSoporteItem
{
    public BandejaSoporteItem(ConsultaSoporteResumen consulta)
    {
        Origen = OrigenBandejaSoporte.HelpDesk;
        ID = consulta.ID;
        Usuario = consulta.Usuario;
        Agencia = consulta.Agencia;
        Suscripcion = consulta.Suscripcion;
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
        PlanComercial = consulta.PlanComercial;
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
    [JsonIgnore, ReferenciaInterna]
    public Usuario? Usuario { get; set; }

    public int? IdUsuario
    {
        get => Usuario?.ID;
        set
        {
            if (value is int id)
            {
                if (Usuario?.ID != id)
                    Usuario = new() { ID = id, Email = EmailSolicitante! };
            }
            else
            {
                _emailSolicitanteSinReferencia = EmailSolicitante;
                Usuario = null;
            }
        }
    }
    [JsonIgnore, ReferenciaInterna]
    public Agencia? Agencia { get; set; }

    public int? IdAgencia
    {
        get => Agencia?.ID;
        set
        {
            if (value is int id)
            {
                if (Agencia?.ID != id)
                    Agencia = new() { ID = id, NombreComercial = NombreAgencia! };
            }
            else
            {
                _nombreAgenciaSinReferencia = NombreAgencia;
                Agencia = null;
            }
        }
    }
    [JsonIgnore, ReferenciaInterna]
    public Suscripcion? Suscripcion { get; set; }

    public int? IdSuscripcion
    {
        get => Suscripcion?.ID;
        set => Suscripcion = value.HasValue
            ? (Suscripcion?.ID == value.Value ? Suscripcion : new() { ID = value.Value })
            : null;
    }
    [JsonIgnore, ReferenciaInterna]
    public PlanComercial? PlanComercial { get; set; }

    public int? IdPlanComercial
    {
        get => PlanComercial?.ID;
        set
        {
            if (value is int id)
            {
                if (PlanComercial?.ID != id)
                    PlanComercial = new() { ID = id, Nombre = NombrePlan! };
            }
            else
            {
                _nombrePlanSinReferencia = NombrePlan;
                PlanComercial = null;
            }
        }
    }
    public string Categoria { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public string NombreSolicitante { get; set; } = string.Empty;
    private string? _emailSolicitanteSinReferencia;

    public string? EmailSolicitante
    {
        get => Usuario is null ? _emailSolicitanteSinReferencia : Usuario.Email;
        set
        {
            if (Usuario is null)
                _emailSolicitanteSinReferencia = value;
            else
                Usuario.Email = value!;
        }
    }
    private string? _nombreAgenciaSinReferencia;

    public string? NombreAgencia
    {
        get => Agencia is null ? _nombreAgenciaSinReferencia : Agencia.NombreComercial;
        set
        {
            if (Agencia is null)
                _nombreAgenciaSinReferencia = value;
            else
                Agencia.NombreComercial = value!;
        }
    }
    private string? _nombrePlanSinReferencia;

    public string? NombrePlan
    {
        get => PlanComercial is null ? _nombrePlanSinReferencia : PlanComercial.Nombre;
        set
        {
            if (PlanComercial is null)
                _nombrePlanSinReferencia = value;
            else
                PlanComercial.Nombre = value!;
        }
    }
    public int CantidadMensajes { get; set; }
    public string? Respuesta { get; set; }
    public DateTime? FechaRespuesta { get; set; }
    public string? NombreRespondedor { get; set; }
}

namespace TeamBalance.BE.Entidades;
public sealed class ConsultaSoporteResumen
{
    public int ID { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Usuario Usuario { get; set; } = new();

    public int IdUsuario
    {
        get => Usuario.ID;
        set => Usuario = Usuario.ID == value ? Usuario : new() { ID = value, Email = Usuario.Email };
    }
    [System.Text.Json.Serialization.JsonIgnore]
    public Agencia? Agencia { get; set; }

    public int? IdAgencia
    {
        get => Agencia?.ID;
        set => Agencia = value.HasValue ? (Agencia?.ID == value.Value ? Agencia : new() { ID = value.Value }) : null;
    }
    [System.Text.Json.Serialization.JsonIgnore]
    public Suscripcion? Suscripcion { get; set; }

    public int? IdSuscripcion
    {
        get => Suscripcion?.ID;
        set => Suscripcion = value.HasValue ? (Suscripcion?.ID == value.Value ? Suscripcion : new() { ID = value.Value }) : null;
    }
    public string Categoria { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public string NombreSolicitante { get; set; } = string.Empty;
    public string? EmailSolicitante { get => Usuario.Email; set => Usuario.Email = value!; }
    public string? NombreAgencia => Agencia?.NombreComercial;
    public string? NombrePlan { get; set; }
    public int CantidadMensajes { get; set; }
}

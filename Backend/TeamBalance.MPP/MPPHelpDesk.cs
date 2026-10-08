using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public sealed class MPPHelpDesk
{
    private readonly Conexion _conexion;
    public MPPHelpDesk(Conexion conexion) => _conexion = conexion;

    public ConsultaSoporteResumen Crear(int idUsuario, int? idAgencia, int? idSuscripcion, CrearConsultaSoporteRequest solicitud) => CrearResumen(_conexion.Leer("dbo.usp_HelpDesk_CrearConsulta", new List<SqlParameter>
    {
        new("@IdUsuario", idUsuario), new("@IdAgencia", (object?)idAgencia ?? DBNull.Value), new("@IdSuscripcion", (object?)idSuscripcion ?? DBNull.Value), new("@Categoria", solicitud.Categoria), new("@Asunto", solicitud.Asunto), new("@Descripcion", solicitud.Descripcion),
    }).Rows[0]);

    public List<ConsultaSoporteResumen> ConsultarPropias(int idUsuario) => Consultar("dbo.usp_HelpDesk_ConsultarPropias", new("@IdUsuario", idUsuario));
    public List<ConsultaSoporteResumen> ConsultarBandeja(string? estado) => Consultar("dbo.usp_HelpDesk_ConsultarBandeja", new("@Estado", (object?)estado ?? DBNull.Value));

    public ConsultaSoporteResumen? ConsultarDetalle(int idConsulta) { DataTable tabla = _conexion.Leer("dbo.usp_HelpDesk_ConsultarDetalle", new List<SqlParameter> { new("@IdConsulta", idConsulta) }); return tabla.Rows.Count == 0 ? null : CrearResumen(tabla.Rows[0]); }

    public List<MensajeSoporteDetalle> ConsultarMensajes(int idConsulta) => _conexion.Leer("dbo.usp_HelpDesk_ConsultarMensajes", new List<SqlParameter> { new("@IdConsulta", idConsulta) }).Rows.Cast<DataRow>().Select(f => new MensajeSoporteDetalle
    {
        ID = Convert.ToInt32(f["ID"]), IdUsuario = Convert.ToInt32(f["IdUsuario"]), NombreUsuario = Convert.ToString(f["NombreUsuario"]) ?? string.Empty, EsSoporte = Convert.ToBoolean(f["EsSoporte"]), Mensaje = Convert.ToString(f["Mensaje"]) ?? string.Empty, Fecha = Convert.ToDateTime(f["Fecha"]),
    }).ToList();

    public void AgregarMensaje(int idConsulta, int idUsuario, string mensaje, string estado) => _conexion.Leer("dbo.usp_HelpDesk_AgregarMensaje", new List<SqlParameter> { new("@IdConsulta", idConsulta), new("@IdUsuario", idUsuario), new("@Mensaje", mensaje), new("@Estado", estado) });
    public void CambiarEstado(int idConsulta, string estado) => _conexion.Leer("dbo.usp_HelpDesk_CambiarEstado", new List<SqlParameter> { new("@IdConsulta", idConsulta), new("@Estado", estado) });

    private List<ConsultaSoporteResumen> Consultar(string sp, SqlParameter parametro) => _conexion.Leer(sp, new List<SqlParameter> { parametro }).Rows.Cast<DataRow>().Select(CrearResumen).ToList();
    private static ConsultaSoporteResumen CrearResumen(DataRow f) => new()
    {
        ID = Convert.ToInt32(f["ID"]), IdUsuario = Convert.ToInt32(f["IdUsuario"]), IdAgencia = f["IdAgencia"] == DBNull.Value ? null : Convert.ToInt32(f["IdAgencia"]), IdSuscripcion = f.Table.Columns.Contains("IdSuscripcion") && f["IdSuscripcion"] != DBNull.Value ? Convert.ToInt32(f["IdSuscripcion"]) : null,
        Categoria = Convert.ToString(f["Categoria"]) ?? string.Empty, Asunto = Convert.ToString(f["Asunto"]) ?? string.Empty, Descripcion = Convert.ToString(f["Descripcion"]) ?? string.Empty, Estado = Convert.ToString(f["Estado"]) ?? string.Empty, FechaCreacion = Convert.ToDateTime(f["FechaCreacion"]), FechaActualizacion = f["FechaActualizacion"] == DBNull.Value ? null : Convert.ToDateTime(f["FechaActualizacion"]),
        NombreSolicitante = f.Table.Columns.Contains("NombreSolicitante") ? Convert.ToString(f["NombreSolicitante"]) ?? string.Empty : string.Empty, EmailSolicitante = f.Table.Columns.Contains("EmailSolicitante") && f["EmailSolicitante"] != DBNull.Value ? Convert.ToString(f["EmailSolicitante"]) : null, NombreAgencia = f.Table.Columns.Contains("NombreAgencia") && f["NombreAgencia"] != DBNull.Value ? Convert.ToString(f["NombreAgencia"]) : null, NombrePlan = f.Table.Columns.Contains("NombrePlan") && f["NombrePlan"] != DBNull.Value ? Convert.ToString(f["NombrePlan"]) : null, CantidadMensajes = f.Table.Columns.Contains("CantidadMensajes") ? Convert.ToInt32(f["CantidadMensajes"]) : 0,
    };
}

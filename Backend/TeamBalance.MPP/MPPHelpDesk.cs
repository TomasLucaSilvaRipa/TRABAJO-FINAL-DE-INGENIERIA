using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public sealed class MPPHelpDesk
{
    private readonly Conexion _conexion;

    public MPPHelpDesk(Conexion conexion) {
        _conexion = conexion;
    }

    public ConsultaSoporteResumen Crear(Usuario usuario, CrearConsultaSoporteRequest solicitud) {
        List<SqlParameter> parametros = new List<SqlParameter>() {
            new SqlParameter("@IdUsuario", usuario.ID),
            new SqlParameter("@IdAgencia", (object?)usuario.IdAgencia ?? DBNull.Value),
            new SqlParameter("@IdSuscripcion", (object?)solicitud.IdSuscripcion ?? DBNull.Value),
            new SqlParameter("@Categoria", solicitud.Categoria),
            new SqlParameter("@Asunto", solicitud.Asunto),
            new SqlParameter("@Descripcion", solicitud.Descripcion)
        };
        DataTable tabla = _conexion.Leer("dbo.usp_HelpDesk_CrearConsulta", parametros);
        return CrearResumen(tabla.Rows[0]);
    }

    public List<ConsultaSoporteResumen> ConsultarPropias(Usuario usuario) {
        return Consultar("dbo.usp_HelpDesk_ConsultarPropias", new SqlParameter("@IdUsuario", usuario.ID));
    }

    public List<ConsultaSoporteResumen> ConsultarBandeja(ConsultaSoporteResumen filtro) {
        return Consultar("dbo.usp_HelpDesk_ConsultarBandeja", new SqlParameter("@Estado", string.IsNullOrWhiteSpace(filtro.Estado) ? DBNull.Value : filtro.Estado));
    }

    public ConsultaSoporteResumen? ConsultarDetalle(ConsultaSoporteResumen consulta) {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdConsulta", consulta.ID) };
        DataTable tabla = _conexion.Leer("dbo.usp_HelpDesk_ConsultarDetalle", parametros);
        return tabla.Rows.Count == 0 ? null : CrearResumen(tabla.Rows[0]);
    }

    public List<MensajeSoporteDetalle> ConsultarMensajes(ConsultaSoporteResumen consulta) {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdConsulta", consulta.ID) };
        DataTable tabla = _conexion.Leer("dbo.usp_HelpDesk_ConsultarMensajes", parametros);
        return tabla.Rows.Cast<DataRow>().Select(fila => new MensajeSoporteDetalle
        {
            ID = Convert.ToInt32(fila["ID"]),
            IdUsuario = Convert.ToInt32(fila["IdUsuario"]),
            NombreUsuario = Convert.ToString(fila["NombreUsuario"]) ?? string.Empty,
            EsSoporte = Convert.ToBoolean(fila["EsSoporte"]),
            Mensaje = Convert.ToString(fila["Mensaje"]) ?? string.Empty,
            Fecha = Convert.ToDateTime(fila["Fecha"]),
        }).ToList();
    }

    public void AgregarMensaje(ConsultaSoporteResumen consulta, Usuario usuario, EnviarMensajeSoporteRequest solicitud) {
        List<SqlParameter> parametros = new List<SqlParameter>() {
            new SqlParameter("@IdConsulta", consulta.ID),
            new SqlParameter("@IdUsuario", usuario.ID),
            new SqlParameter("@Mensaje", solicitud.Mensaje),
            new SqlParameter("@Estado", consulta.Estado)
        };
        _conexion.Leer("dbo.usp_HelpDesk_AgregarMensaje", parametros);
    }

    public void CambiarEstado(ConsultaSoporteResumen consulta) {
        List<SqlParameter> parametros = new List<SqlParameter>() {
            new SqlParameter("@IdConsulta", consulta.ID),
            new SqlParameter("@Estado", consulta.Estado)
        };
        _conexion.Leer("dbo.usp_HelpDesk_CambiarEstado", parametros);
    }

    private List<ConsultaSoporteResumen> Consultar(string procedimientoAlmacenado, SqlParameter parametro) {
        List<SqlParameter> parametros = new List<SqlParameter> { parametro };
        DataTable tabla = _conexion.Leer(procedimientoAlmacenado, parametros);
        return tabla.Rows.Cast<DataRow>().Select(CrearResumen).ToList();
    }

    private static ConsultaSoporteResumen CrearResumen(DataRow fila) {
        ConsultaSoporteResumen consulta = new ConsultaSoporteResumen();
        consulta.ID = Convert.ToInt32(fila["ID"]);
        consulta.IdUsuario = Convert.ToInt32(fila["IdUsuario"]);
        consulta.IdAgencia = fila["IdAgencia"] == DBNull.Value ? null : Convert.ToInt32(fila["IdAgencia"]);
        consulta.IdSuscripcion = fila.Table.Columns.Contains("IdSuscripcion") && fila["IdSuscripcion"] != DBNull.Value ? Convert.ToInt32(fila["IdSuscripcion"]) : null;
        consulta.Categoria = Convert.ToString(fila["Categoria"]) ?? string.Empty;
        consulta.Asunto = Convert.ToString(fila["Asunto"]) ?? string.Empty;
        consulta.Descripcion = Convert.ToString(fila["Descripcion"]) ?? string.Empty;
        consulta.Estado = Convert.ToString(fila["Estado"]) ?? string.Empty;
        consulta.FechaCreacion = Convert.ToDateTime(fila["FechaCreacion"]);
        consulta.FechaActualizacion = fila["FechaActualizacion"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaActualizacion"]);
        consulta.NombreSolicitante = fila.Table.Columns.Contains("NombreSolicitante") ? Convert.ToString(fila["NombreSolicitante"]) ?? string.Empty : string.Empty;
        consulta.EmailSolicitante = fila.Table.Columns.Contains("EmailSolicitante") && fila["EmailSolicitante"] != DBNull.Value ? Convert.ToString(fila["EmailSolicitante"]) : null;
        consulta.NombreAgencia = fila.Table.Columns.Contains("NombreAgencia") && fila["NombreAgencia"] != DBNull.Value ? Convert.ToString(fila["NombreAgencia"]) : null;
        consulta.NombrePlan = fila.Table.Columns.Contains("NombrePlan") && fila["NombrePlan"] != DBNull.Value ? Convert.ToString(fila["NombrePlan"]) : null;
        consulta.CantidadMensajes = fila.Table.Columns.Contains("CantidadMensajes") ? Convert.ToInt32(fila["CantidadMensajes"]) : 0;
        return consulta;
    }
}

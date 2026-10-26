using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPNotificacion
{
    private readonly Conexion _conexion;
    public MPPNotificacion(Conexion conexion) { _conexion = conexion; }

    public void RegistrarPorEmail(NotificacionUsuario notificacion)
    {
        List<SqlParameter> parametros = new()
        {
            new SqlParameter("@EmailExacto", notificacion.EmailDestinatario.Trim()),
            new SqlParameter("@Titulo", notificacion.Titulo),
            new SqlParameter("@Mensaje", notificacion.Mensaje),
        };
        _conexion.Escribir("dbo.usp_Notificacion_RegistrarPorEmail", parametros);
    }

    public List<NotificacionUsuario> ConsultarPropias(Usuario usuario)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Notificacion_ConsultarPropias", new List<SqlParameter> { new("@IdUsuario", usuario.ID) });
        return tabla.Rows.Cast<DataRow>().Select(Crear).ToList();
    }

    public void MarcarLeida(Usuario usuario, NotificacionUsuario notificacion)
    {
        _conexion.Escribir("dbo.usp_Notificacion_MarcarLeida", new List<SqlParameter> { new("@IdUsuario", usuario.ID), new("@IdNotificacion", notificacion.ID) });
    }

    public void MarcarTodasLeidas(Usuario usuario)
    {
        _conexion.Escribir("dbo.usp_Notificacion_MarcarTodasLeidas", new List<SqlParameter> { new("@IdUsuario", usuario.ID) });
    }

    private static NotificacionUsuario Crear(DataRow fila) => new()
    {
        ID = Convert.ToInt32(fila["ID"]), IdUsuario = Convert.ToInt32(fila["IdUsuario"]),
        Titulo = Convert.ToString(fila["Titulo"]) ?? string.Empty, Mensaje = Convert.ToString(fila["Mensaje"]) ?? string.Empty,
        Fecha = Convert.ToDateTime(fila["Fecha"]), Leida = Convert.ToBoolean(fila["Leida"]),
    };
}

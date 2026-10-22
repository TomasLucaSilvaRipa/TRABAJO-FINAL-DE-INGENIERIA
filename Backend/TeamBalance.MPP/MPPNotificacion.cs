using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPNotificacion
{
    private readonly Conexion _conexion;
    public MPPNotificacion(Conexion conexion) { _conexion = conexion; }

    public void RegistrarPorEmail(string emailExacto, string titulo, string mensaje)
    {
        List<SqlParameter> parametros = new()
        {
            new SqlParameter("@EmailExacto", emailExacto.Trim()),
            new SqlParameter("@Titulo", titulo),
            new SqlParameter("@Mensaje", mensaje),
        };
        _conexion.Escribir("dbo.usp_Notificacion_RegistrarPorEmail", parametros);
    }

    public List<NotificacionUsuario> ConsultarPropias(int idUsuario)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Notificacion_ConsultarPropias", new List<SqlParameter> { new("@IdUsuario", idUsuario) });
        return tabla.Rows.Cast<DataRow>().Select(Crear).ToList();
    }

    public void MarcarLeida(int idUsuario, int idNotificacion)
    {
        _conexion.Escribir("dbo.usp_Notificacion_MarcarLeida", new List<SqlParameter> { new("@IdUsuario", idUsuario), new("@IdNotificacion", idNotificacion) });
    }

    public void MarcarTodasLeidas(int idUsuario)
    {
        _conexion.Escribir("dbo.usp_Notificacion_MarcarTodasLeidas", new List<SqlParameter> { new("@IdUsuario", idUsuario) });
    }

    private static NotificacionUsuario Crear(DataRow fila) => new()
    {
        ID = Convert.ToInt32(fila["ID"]), IdUsuario = Convert.ToInt32(fila["IdUsuario"]),
        Titulo = Convert.ToString(fila["Titulo"]) ?? string.Empty, Mensaje = Convert.ToString(fila["Mensaje"]) ?? string.Empty,
        Fecha = Convert.ToDateTime(fila["Fecha"]), Leida = Convert.ToBoolean(fila["Leida"]),
    };
}

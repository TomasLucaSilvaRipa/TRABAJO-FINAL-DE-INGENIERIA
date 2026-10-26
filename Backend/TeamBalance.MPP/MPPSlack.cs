using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public sealed class MPPSlack
{
    private readonly Conexion _conexion;

    public MPPSlack(Conexion conexion)
    {
        _conexion = conexion;
    }

    public Usuario? ConsultarUsuarioPorSlack(ConsultaSlack consulta)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@SlackUserId", consulta.IdUsuarioSlack),
        };

        DataTable tabla = _conexion.Leer("dbo.usp_Slack_ConsultarUsuario", parametros);
        if (tabla.Rows.Count != 1)
        {
            return null;
        }

        return CrearUsuario(tabla.Rows[0]);
    }

    public List<Usuario> ConsultarUsuariosSinVincular()
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Slack_ConsultarUsuariosSinVincular");
        List<Usuario> usuarios = new List<Usuario>();
        foreach (DataRow fila in tabla.Rows)
        {
            usuarios.Add(CrearUsuario(fila));
        }

        return usuarios;
    }

    public void VincularUsuario(VinculacionSlack vinculacion)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@IdUsuario", vinculacion.Usuario.ID),
            new SqlParameter("@SlackUserId", vinculacion.IdUsuarioSlack),
        };

        _conexion.Escribir("dbo.usp_Slack_VincularUsuario", parametros);
    }

    private static Usuario CrearUsuario(DataRow fila)
    {
        Usuario usuario = new Usuario();
        usuario.ID = Convert.ToInt32(fila["ID"]);
        usuario.IdAgencia = fila["IdAgencia"] == DBNull.Value ? null : Convert.ToInt32(fila["IdAgencia"]);
        usuario.Nombre = Convert.ToString(fila["Nombre"]) ?? string.Empty;
        usuario.Apellido = Convert.ToString(fila["Apellido"]) ?? string.Empty;
        usuario.Email = Convert.ToString(fila["Email"]) ?? string.Empty;
        usuario.PasswordHash = Convert.ToString(fila["PasswordHash"]) ?? string.Empty;
        usuario.Estado = Convert.ToString(fila["Estado"]) ?? string.Empty;
        usuario.FechaAlta = Convert.ToDateTime(fila["FechaAlta"]);
        usuario.Activo = Convert.ToBoolean(fila["Activo"]);
        usuario.FechaBaja = fila["FechaBaja"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaBaja"]);
        return usuario;
    }

    public List<(Empleado Empleado, decimal CargaActual, decimal CapacidadSemanal, int TareasActivas)> ConsultarEmpleadoPM(Usuario usuario, string nombreEmpleado)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@IdUsuarioPM", usuario.ID),
            new SqlParameter("@IdAgencia", usuario.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.")),
            new SqlParameter("@NombreEmpleado", nombreEmpleado),
        };

        DataTable tabla = _conexion.Leer("dbo.usp_Slack_ConsultarCargaEmpleadoPM", parametros);
        return tabla.Rows.Cast<DataRow>().Select(CrearCargaEmpleado).ToList();
    }

    public List<(Empleado Empleado, decimal CargaActual, decimal CapacidadSemanal, int TareasActivas)> ConsultarEquipoPM(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@IdUsuarioPM", usuario.ID),
            new SqlParameter("@IdAgencia", usuario.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.")),
        };

        DataTable tabla = _conexion.Leer("dbo.usp_Slack_ConsultarCargaEquipoPM", parametros);
        return tabla.Rows.Cast<DataRow>().Select(CrearCargaEmpleado).ToList();
    }

    public List<Proyecto> ConsultarProyectosAccesibles(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@IdUsuario", usuario.ID),
            new SqlParameter("@IdAgencia", usuario.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.")),
        };

        DataTable tabla = _conexion.Leer("dbo.usp_Slack_ConsultarProyectosAccesibles", parametros);
        return tabla.Rows.Cast<DataRow>().Select(fila => new Proyecto
        {
            ID = Convert.ToInt32(fila["ID"]),
            Nombre = Convert.ToString(fila["Nombre"]) ?? string.Empty,
        }).ToList();
    }

    private static (Empleado Empleado, decimal CargaActual, decimal CapacidadSemanal, int TareasActivas) CrearCargaEmpleado(DataRow fila)
    {
        Empleado empleado = new Empleado();
        empleado.ID = Convert.ToInt32(fila["IdEmpleado"]);
        empleado.Nombre = Convert.ToString(fila["Nombre"]) ?? string.Empty;
        empleado.Apellido = Convert.ToString(fila["Apellido"]) ?? string.Empty;
        empleado.Activo = true;

        return (empleado, Convert.ToDecimal(fila["CargaActual"]), Convert.ToDecimal(fila["CapacidadSemanal"]), Convert.ToInt32(fila["TareasActivas"]));
    }
}

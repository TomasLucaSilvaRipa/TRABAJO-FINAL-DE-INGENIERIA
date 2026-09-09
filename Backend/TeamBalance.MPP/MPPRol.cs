using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPRol
{
    private readonly Conexion _conexion;

    public MPPRol(Conexion conexion)
    {
        _conexion = conexion;
    }

    public Rol ConsultarRolPorNombre(string nombre)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@Nombre", nombre),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Rol_ConsultarPorNombre", parametros);

        if (resultado.Rows.Count >= 1)
        {
            DataRow fila = resultado.Rows[0];

            return CrearRol(fila);
        }
        else{ throw new KeyNotFoundException("No existe un rol activo con el nombre indicado."); }
    }

    public List<Rol> ConsultarRolesActivos()
    {
        DataTable resultado = _conexion.Leer("dbo.usp_Rol_ConsultarActivos");
        return resultado.Rows.Cast<DataRow>().Select(CrearRol).ToList();
    }

    public List<Rol> ConsultarRolesUsuario(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID) };
        DataTable resultado = _conexion.Leer("dbo.usp_Rol_ConsultarPorUsuario", parametros);
        return resultado.Rows.Cast<DataRow>().Select(CrearRol).ToList();
    }

    public List<Permiso> ConsultarPermisosUsuario(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID) };
        DataTable resultado = _conexion.Leer("dbo.usp_Permiso_ConsultarPorUsuario", parametros);
        return resultado.Rows.Cast<DataRow>().Select(CrearPermiso).ToList();
    }

    public List<Permiso> ConsultarPermisosActivos()
    {
        DataTable resultado = _conexion.Leer("dbo.usp_Permiso_ConsultarActivos");
        return resultado.Rows.Cast<DataRow>().Select(CrearPermiso).ToList();
    }

    public List<Permiso> ConsultarPermisosRol(Rol rol)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdRol", rol.ID) };
        DataTable resultado = _conexion.Leer("dbo.usp_Permiso_ConsultarPorRol", parametros);
        return resultado.Rows.Cast<DataRow>().Select(CrearPermiso).ToList();
    }

    public int RegistrarRol(Rol rol)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@Nombre", rol.Nombre), new SqlParameter("@Descripcion", (object?)rol.Descripcion ?? DBNull.Value) };
        DataTable resultado = _conexion.Leer("dbo.usp_Rol_Registrar", parametros);
        return resultado.Rows.Count == 1 ? Convert.ToInt32(resultado.Rows[0]["ID"]) : throw new InvalidOperationException("No fue posible registrar el rol.");
    }

    public void ModificarRol(Rol rol)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdRol", rol.ID), new SqlParameter("@Nombre", rol.Nombre), new SqlParameter("@Descripcion", (object?)rol.Descripcion ?? DBNull.Value) };
        if (!_conexion.Escribir("dbo.usp_Rol_Modificar", parametros)){ throw new InvalidOperationException("No fue posible modificar el rol."); }
    }

    public void CambiarEstado(Rol rol, bool activo)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdRol", rol.ID), new SqlParameter("@Activo", activo) };
        if (!_conexion.Escribir("dbo.usp_Rol_CambiarEstado", parametros)){ throw new InvalidOperationException("No fue posible actualizar el rol."); }
    }

    public void ReemplazarPermisos(Rol rol)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdRol", rol.ID) };
        _conexion.Escribir("dbo.usp_Rol_LimpiarPermisos", parametros);
        foreach (Permiso permiso in rol.Permisos)
        {
            List<SqlParameter> parametrosPermiso = new List<SqlParameter>() { new SqlParameter("@IdRol", rol.ID), new SqlParameter("@IdPermiso", permiso.ID) };
            _conexion.Escribir("dbo.usp_Rol_AsignarPermiso", parametrosPermiso);
        }
    }

    private static Rol CrearRol(DataRow fila)
    {
        Rol rol = new Rol(Convert.ToInt32(fila["ID"]), Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["Descripcion"]), Convert.ToBoolean(fila["EsRolBase"]), Convert.ToBoolean(fila["Activo"]), new List<Permiso>());
        rol.FechaBaja = fila["FechaBaja"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaBaja"]);
        return rol;
    }

    private static Permiso CrearPermiso(DataRow fila)
    {
        string? codigo = fila.Table.Columns.Contains("Codigo") ? Convert.ToString(fila["Codigo"]) : null;
        string? url = fila.Table.Columns.Contains("Url") ? Convert.ToString(fila["Url"]) : null;
        Permiso permiso = new Permiso(Convert.ToInt32(fila["ID"]), Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["Descripcion"]), Convert.ToBoolean(fila["Activo"]), codigo, url);
        return permiso;
    }
}

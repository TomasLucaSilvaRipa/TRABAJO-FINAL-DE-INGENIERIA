using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPRegistroHora
{
    private readonly Conexion _conexion;

    public MPPRegistroHora(Conexion conexion)
    {
        _conexion = conexion;
    }

    public RegistroHora Registrar(RegistroHora registro, Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdTarea", registro.IdTarea), new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@Fecha", registro.Fecha), new SqlParameter("@CantidadHoras", registro.CantidadHoras), new SqlParameter("@Descripcion", (object?)registro.Descripcion ?? DBNull.Value) };
            DataTable tabla = _conexion.Leer("dbo.usp_RegistroHora_Registrar", parametros);
            RegistroHora resultado = CrearRegistro(tabla.Rows[0]);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<RegistroHora> ConsultarPropios(Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID) };
            DataTable tabla = _conexion.Leer("dbo.usp_RegistroHora_ConsultarPropios", parametros);
            List<RegistroHora> registros = new List<RegistroHora>();
            foreach (DataRow fila in tabla.Rows)
            {
                RegistroHora registro = CrearRegistro(fila);
                registros.Add(registro);
            }
            return registros;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static RegistroHora CrearRegistro(DataRow fila)
    {
        try
        {
            RegistroHora registro = new RegistroHora(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdTarea"]), Convert.ToInt32(fila["IdEmpleado"]), Convert.ToDateTime(fila["Fecha"]), Convert.ToDecimal(fila["CantidadHoras"]), Convert.ToString(fila["Descripcion"]), Convert.ToBoolean(fila["Activo"]));
            return registro;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
}

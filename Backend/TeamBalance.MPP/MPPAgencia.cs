using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPAgencia
{
    private readonly Conexion _conexion;

    public MPPAgencia(Conexion conexion)
    {
        _conexion = conexion;
    }

    public bool ExisteAgencia(string cuit, string emailContacto)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@CUIT", cuit),
            new SqlParameter("@EmailContacto", emailContacto),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Agencia_Existe", parametros);

        return resultado.Rows.Count == 1 && Convert.ToBoolean(resultado.Rows[0]["Existe"]);
    }

    public RegistroAgenciaResultado RegistrarAgencia(Agencia agencia, Usuario usuario, Dueño dueño, ValidacionCuentum validacion, string referenciaContratacion)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@ReferenciaContratacion", referenciaContratacion),
            new SqlParameter("@NombreComercial", agencia.NombreComercial),
            new SqlParameter("@RazonSocial", (object?)agencia.RazonSocial ?? DBNull.Value),
            new SqlParameter("@CUIT", agencia.CUIT),
            new SqlParameter("@CondicionFiscal", (object?)agencia.CondicionFiscal ?? DBNull.Value),
            new SqlParameter("@EmailContacto", agencia.EmailContacto),
            new SqlParameter("@TelefonoContacto", (object?)agencia.TelefonoContacto ?? DBNull.Value),
            new SqlParameter("@IdRol", usuario.Rol.ID),
            new SqlParameter("@Nombre", usuario.Nombre),
            new SqlParameter("@Apellido", usuario.Apellido),
            new SqlParameter("@Email", usuario.Email),
            new SqlParameter("@PasswordHash", usuario.PasswordHash),
            new SqlParameter("@EstadoUsuario", usuario.Estado),
            new SqlParameter("@ActivoUsuario", usuario.Activo),
            new SqlParameter("@ActivoDueno", dueño.Activo),
            new SqlParameter("@MetodoValidacion", validacion.Metodo),
            new SqlParameter("@TokenHash", validacion.TokenHash),
            new SqlParameter("@FechaExpiracion", validacion.FechaExpiracion),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Agencia_RegistrarDesdeContratacion", parametros);

        if (resultado.Rows.Count != 1)
        {
            throw new InvalidOperationException("No fue posible registrar la agencia.");
        }

        DataRow fila = resultado.Rows[0];

        int idAgencia = Convert.ToInt32(fila["IdAgencia"]);
        int idUsuario = Convert.ToInt32(fila["IdUsuario"]);
        RegistroAgenciaResultado resultadoRegistro = new RegistroAgenciaResultado(idAgencia, idUsuario);
        return resultadoRegistro;
    }
}

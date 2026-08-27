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
        var parametros = new List<SqlParameter>()
        {
            new("@CUIT", cuit),
            new("@EmailContacto", emailContacto),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Agencia_Existe", parametros);

        return resultado.Rows.Count == 1 && Convert.ToBoolean(resultado.Rows[0]["Existe"]);
    }

    public void RegistrarAgencia( Agencia agencia, Usuario usuario, Dueño dueño, ValidacionCuentum validacion, string referenciaContratacion)
    {
        var parametros = new List<SqlParameter>()
        {
            new("@ReferenciaContratacion", referenciaContratacion),
            new("@NombreComercial", agencia.NombreComercial),
            new("@RazonSocial", (object?)agencia.RazonSocial ?? DBNull.Value),
            new("@CUIT", agencia.CUIT),
            new("@CondicionFiscal", (object?)agencia.CondicionFiscal ?? DBNull.Value),
            new("@EmailContacto", agencia.EmailContacto),
            new("@TelefonoContacto", (object?)agencia.TelefonoContacto ?? DBNull.Value),
            new("@IdRol", usuario.IdRol),
            new("@Nombre", usuario.Nombre),
            new("@Apellido", usuario.Apellido),
            new("@Email", usuario.Email),
            new("@PasswordHash", usuario.PasswordHash),
            new("@EstadoUsuario", usuario.Estado),
            new("@ActivoUsuario", usuario.Activo),
            new("@ActivoDueno", dueño.Activo),
            new("@MetodoValidacion", validacion.Metodo),
            new("@TokenHash", validacion.TokenHash),
            new("@FechaExpiracion", validacion.FechaExpiracion),
        };

        _conexion.Leer("dbo.usp_Agencia_RegistrarDesdeContratacion", parametros);
    }
}

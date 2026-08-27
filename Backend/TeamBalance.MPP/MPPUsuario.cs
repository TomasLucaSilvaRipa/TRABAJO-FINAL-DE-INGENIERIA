using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPUsuario
{
    private readonly Conexion _conexion;

    public MPPUsuario(Conexion conexion)
    {
        _conexion = conexion;
    }

    public bool ExisteUsuarioPorEmail(string email)
    {
        var parametros = new List<SqlParameter>
        {
            new("@Email", email),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Usuario_ExisteEmail", parametros);

        return resultado.Rows.Count == 1 && Convert.ToBoolean(resultado.Rows[0]["Existe"]);
    }

    public Usuario? ConsultarUsuarioPorEmail(string email)
    {
        var parametros = new List<SqlParameter>
        {
            new("@Email", email),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Usuario_ConsultarPorEmail", parametros);

        if (resultado.Rows.Count != 1)
        {
            return null;
        }

        return CrearUsuario(resultado.Rows[0]);
    }

    public Usuario? ConsultarUsuarioPendienteValidacion(string email)
    {
        var parametros = new List<SqlParameter>
        {
            new("@Email", email),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Usuario_ConsultarPendienteValidacion", parametros);

        if (resultado.Rows.Count != 1)
        {
            return null;
        }

        return CrearUsuario(resultado.Rows[0]);
    }

    public void RegistrarSesion(SesionUsuario sesion)
    {
        var parametros = new List<SqlParameter>
        {
            new("@IdUsuario", sesion.IdUsuario),
            new("@TokenHash", sesion.TokenHash),
            new("@FechaExpiracion", sesion.FechaExpiracion),
        };

        if (!_conexion.Escribir("dbo.usp_SesionUsuario_Registrar", parametros))
        {
            throw new InvalidOperationException("No fue posible registrar la sesión del usuario.");
        }
    }

    public bool SesionVigente(string tokenHash)
    {
        var parametros = new List<SqlParameter>
        {
            new("@TokenHash", tokenHash),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_SesionUsuario_Validar", parametros);

        return resultado.Rows.Count == 1 && Convert.ToBoolean(resultado.Rows[0]["Vigente"]);
    }

    public void CerrarSesion(string tokenHash)
    {
        var parametros = new List<SqlParameter>
        {
            new("@TokenHash", tokenHash),
        };

        _conexion.Escribir("dbo.usp_SesionUsuario_Cerrar", parametros);
    }

    public bool ValidarCuenta(string tokenHash)
    {
        var parametros = new List<SqlParameter>
        {
            new("@TokenHash", tokenHash),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_ValidacionCuenta_Validar", parametros);

        return resultado.Rows.Count == 1 && Convert.ToBoolean(resultado.Rows[0]["Validada"]);
    }

    public void ReemplazarValidacionEmail(Usuario usuario, ValidacionCuentum validacion)
    {
        var parametros = new List<SqlParameter>
        {
            new("@IdUsuario", usuario.ID),
            new("@Metodo", validacion.Metodo),
            new("@TokenHash", validacion.TokenHash),
            new("@FechaExpiracion", validacion.FechaExpiracion),
        };

        if (!_conexion.Escribir("dbo.usp_ValidacionCuenta_Reenviar", parametros))
        {
            throw new InvalidOperationException("No fue posible generar una nueva validación de correo.");
        }
    }

    private static Usuario CrearUsuario(DataRow fila)
    {
        return new Usuario
        {
            ID = Convert.ToInt32(fila["ID"]),
            IdAgencia = fila["IdAgencia"] == DBNull.Value ? null : Convert.ToInt32(fila["IdAgencia"]),
            IdRol = Convert.ToInt32(fila["IdRol"]),
            Nombre = Convert.ToString(fila["Nombre"]) ?? string.Empty,
            Apellido = Convert.ToString(fila["Apellido"]) ?? string.Empty,
            Email = Convert.ToString(fila["Email"]) ?? string.Empty,
            PasswordHash = Convert.ToString(fila["PasswordHash"]) ?? string.Empty,
            Estado = Convert.ToString(fila["Estado"]) ?? string.Empty,
            FechaAlta = Convert.ToDateTime(fila["FechaAlta"]),
            Activo = Convert.ToBoolean(fila["Activo"]),
            FechaBaja = fila["FechaBaja"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaBaja"]),
        };
    }
}

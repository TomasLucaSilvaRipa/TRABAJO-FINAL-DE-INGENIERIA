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
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@Email", email),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Usuario_ExisteEmail", parametros);

        return resultado.Rows.Count == 1 && Convert.ToBoolean(resultado.Rows[0]["Existe"]);
    }

    public Usuario? ConsultarUsuarioPorEmail(string email)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@Email", email),
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
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@Email", email),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Usuario_ConsultarPendienteValidacion", parametros);

        if (resultado.Rows.Count != 1)
        {
            return null;
        }

        return CrearUsuario(resultado.Rows[0]);
    }

    public Usuario? ConsultarUsuarioPorSesion(SesionUsuario sesion)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@TokenHash", sesion.TokenHash),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Usuario_ConsultarPorSesion", parametros);

        if (resultado.Rows.Count != 1)
        {
            return null;
        }

        return CrearUsuario(resultado.Rows[0]);
    }

    public Usuario? ConsultarUsuarioPorRecuperacionPassword(ValidacionCuentum validacion)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@TokenHash", validacion.TokenHash),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_RecuperacionPassword_ConsultarUsuario", parametros);

        if (resultado.Rows.Count != 1)
        {
            return null;
        }

        return CrearUsuario(resultado.Rows[0]);
    }

    public void RegistrarSesion(SesionUsuario sesion)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@IdUsuario", sesion.IdUsuario),
            new SqlParameter("@TokenHash", sesion.TokenHash),
            new SqlParameter("@FechaExpiracion", sesion.FechaExpiracion),
        };

        if (!_conexion.Escribir("dbo.usp_SesionUsuario_Registrar", parametros))
        {
            throw new InvalidOperationException("No fue posible registrar la sesión del usuario.");
        }
    }

    public bool SesionVigente(string tokenHash)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@TokenHash", tokenHash),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_SesionUsuario_Validar", parametros);

        return resultado.Rows.Count == 1 && Convert.ToBoolean(resultado.Rows[0]["Vigente"]);
    }

    public void CerrarSesion(string tokenHash)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@TokenHash", tokenHash),
        };

        _conexion.Escribir("dbo.usp_SesionUsuario_Cerrar", parametros);
    }

    public bool ValidarCuenta(string tokenHash)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@TokenHash", tokenHash),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_ValidacionCuenta_Validar", parametros);
        return resultado.Rows.Count == 1 && Convert.ToBoolean(resultado.Rows[0]["Validada"]);
    }

    public void ReemplazarValidacionEmail(Usuario usuario, ValidacionCuentum validacion)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@IdUsuario", usuario.ID),
            new SqlParameter("@Metodo", validacion.Metodo),
            new SqlParameter("@TokenHash", validacion.TokenHash),
            new SqlParameter("@FechaExpiracion", validacion.FechaExpiracion),
        };

        if (!_conexion.Escribir("dbo.usp_ValidacionCuenta_Reenviar", parametros))
        {
            throw new InvalidOperationException("No fue posible generar una nueva validación de correo.");
        }
    }

    public void ReemplazarRecuperacionPassword(Usuario usuario, ValidacionCuentum validacion)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@IdUsuario", usuario.ID),
            new SqlParameter("@Metodo", validacion.Metodo),
            new SqlParameter("@TokenHash", validacion.TokenHash),
            new SqlParameter("@FechaExpiracion", validacion.FechaExpiracion),
        };

        if (!_conexion.Escribir("dbo.usp_ValidacionCuenta_Reenviar", parametros))
        {
            throw new InvalidOperationException("No fue posible generar el enlace de recuperación.");
        }
    }

    public bool RestablecerPassword(Usuario usuario, ValidacionCuentum validacion)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@IdUsuario", usuario.ID),
            new SqlParameter("@TokenHash", validacion.TokenHash),
            new SqlParameter("@PasswordHash", usuario.PasswordHash),
        };

        return _conexion.Escribir("dbo.usp_RecuperacionPassword_Restablecer", parametros);
    }

    public bool CambiarPassword(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@IdUsuario", usuario.ID),
            new SqlParameter("@PasswordHash", usuario.PasswordHash),
        };

        return _conexion.Escribir("dbo.usp_Usuario_CambiarPassword", parametros);
    }

    public List<Usuario> ConsultarUsuariosAgencia(Usuario solicitante)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdAgencia", solicitante.IdAgencia) };
        DataTable resultado = _conexion.Leer("dbo.usp_Usuario_ConsultarPorAgencia", parametros);
        return resultado.Rows.Cast<DataRow>().Select(CrearUsuarioConEmpleado).ToList();
    }

    public int RegistrarUsuario(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@IdAgencia", (object?)usuario.IdAgencia ?? DBNull.Value),
            new SqlParameter("@IdRolPrincipal", usuario.Rol.ID),
            new SqlParameter("@Nombre", usuario.Nombre),
            new SqlParameter("@Apellido", usuario.Apellido),
            new SqlParameter("@Email", usuario.Email),
            new SqlParameter("@PasswordHash", usuario.PasswordHash),
            new SqlParameter("@Estado", usuario.Estado),
        };
        DataTable resultado = _conexion.Leer("dbo.usp_Usuario_RegistrarGestion", parametros);
        return resultado.Rows.Count == 1 ? Convert.ToInt32(resultado.Rows[0]["ID"]) : throw new InvalidOperationException("No fue posible registrar el usuario.");
    }

    public void ModificarUsuario(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@Nombre", usuario.Nombre), new SqlParameter("@Apellido", usuario.Apellido), new SqlParameter("@Email", usuario.Email), new SqlParameter("@IdRolPrincipal", usuario.Rol.ID) };
        if (!_conexion.Escribir("dbo.usp_Usuario_ModificarGestion", parametros)){ throw new InvalidOperationException("No fue posible modificar el usuario."); }
    }

    public void ReemplazarRoles(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID) };
        _conexion.Escribir("dbo.usp_Usuario_LimpiarRoles", parametros);
        foreach (Rol rol in usuario.Roles)
        {
            List<SqlParameter> parametrosRol = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@IdRol", rol.ID) };
            _conexion.Escribir("dbo.usp_Usuario_AsignarRol", parametrosRol);
        }
    }

    public void CambiarEstado(Usuario usuario, bool activo)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@Activo", activo) };
        if (!_conexion.Escribir("dbo.usp_Usuario_CambiarEstado", parametros)){ throw new InvalidOperationException("No fue posible actualizar el estado del usuario."); }
    }

    public void RegistrarEmpleado(Usuario usuario)
    {
        Empleado empleado = usuario.Empleado ?? throw new ArgumentException("Completá los datos laborales del empleado.");
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@CostoHora", empleado.CostoHora), new SqlParameter("@HorasDisponiblesSemanales", empleado.HorasDisponiblesSemanales), new SqlParameter("@Seniority", empleado.Seniority), new SqlParameter("@EstadoLaboral", empleado.EstadoLaboral) };
        if (!_conexion.Escribir("dbo.usp_Empleado_RegistrarActualizar", parametros)){ throw new InvalidOperationException("No fue posible guardar el perfil de empleado."); }
    }

    public void RegistrarPM(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID) };
        if (!_conexion.Escribir("dbo.usp_PM_Registrar", parametros)){ throw new InvalidOperationException("No fue posible guardar el perfil de PM."); }
    }

    public bool ExisteSoporte()
    {
        DataTable resultado = _conexion.Leer("dbo.usp_Soporte_Existe");
        return resultado.Rows.Count == 1 && Convert.ToBoolean(resultado.Rows[0]["Existe"]);
    }

    private static Usuario CrearUsuario(DataRow fila)
    {
        int idRol = Convert.ToInt32(fila["IdRol"]);
        Rol rol = new Rol(idRol, null, string.Empty, null, string.Empty, false, true, new List<Permiso>());
        Usuario usuario = new Usuario(Convert.ToInt32(fila["ID"]), fila["IdAgencia"] == DBNull.Value ? null : Convert.ToInt32(fila["IdAgencia"]), rol, Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["Apellido"]) ?? string.Empty, Convert.ToString(fila["Email"]) ?? string.Empty, Convert.ToString(fila["PasswordHash"]) ?? string.Empty, Convert.ToString(fila["Estado"]) ?? string.Empty, Convert.ToDateTime(fila["FechaAlta"]), Convert.ToBoolean(fila["Activo"]));
        usuario.FechaBaja = fila["FechaBaja"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaBaja"]);
        return usuario;
    }

    private static Usuario CrearUsuarioConEmpleado(DataRow fila)
    {
        Usuario usuario = CrearUsuario(fila);
        if (fila["Seniority"] != DBNull.Value)
        {
            List<Skill> skills = new List<Skill>();
            Empleado empleado = new Empleado(usuario.ID, usuario.Nombre, usuario.Email, usuario.Activo, Convert.ToDecimal(fila["CostoHora"]), Convert.ToDecimal(fila["HorasDisponiblesSemanales"]), Convert.ToString(fila["Seniority"]) ?? string.Empty, Convert.ToString(fila["EstadoLaboral"]) ?? string.Empty, fila["FechaIngreso"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaIngreso"]), skills);
            usuario.Empleado = empleado;
        }
        return usuario;
    }
}

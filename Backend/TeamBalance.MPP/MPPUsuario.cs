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

    public bool ExisteUsuarioPorEmail(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@Email", usuario.Email.Trim().ToLowerInvariant()),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Usuario_ExisteEmail", parametros);

        return resultado.Rows.Count == 1 && Convert.ToBoolean(resultado.Rows[0]["Existe"]);
    }

    public Usuario? ConsultarUsuarioPorEmail(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@Email", usuario.Email),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Usuario_ConsultarPorEmail", parametros);

        if (resultado.Rows.Count != 1)
        {
            return null;
        }

        return CargarFotoPerfil(CrearUsuario(resultado.Rows[0]));
    }

    public Usuario? ConsultarUsuarioPendienteValidacion(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@Email", usuario.Email.Trim().ToLowerInvariant()),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Usuario_ConsultarPendienteValidacion", parametros);

        if (resultado.Rows.Count != 1)
        {
            return null;
        }

        return CargarFotoPerfil(CrearUsuario(resultado.Rows[0]));
    }

    public Usuario? ConsultarUsuarioPorId(Usuario usuarioBuscado)
    {
        DataTable resultado = _conexion.Leer("dbo.usp_Usuario_ConsultarPorId", new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuarioBuscado.ID) });
        return resultado.Rows.Count == 1 ? CargarFotoPerfil(CrearUsuario(resultado.Rows[0])) : null;
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

        return CargarFotoPerfil(CrearUsuario(resultado.Rows[0]));
    }

    public Usuario? ConsultarUsuarioPorRecuperacionPassword(ValidacionCuenta validacion)
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

        return CargarFotoPerfil(CrearUsuario(resultado.Rows[0]));
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

    public void ReemplazarValidacionEmail(Usuario usuario, ValidacionCuenta validacion)
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

    public void ReemplazarRecuperacionPassword(Usuario usuario, ValidacionCuenta validacion)
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

    public bool RestablecerPassword(Usuario usuario, ValidacionCuenta validacion)
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

    public void ActualizarFotoPerfil(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@IdUsuario", usuario.ID),
            new SqlParameter("@FotoPerfil", SqlDbType.NVarChar, -1) { Value = usuario.FotoPerfil ?? (object)DBNull.Value },
        };

        if (!_conexion.Escribir("dbo.usp_Usuario_ActualizarFotoPerfil", parametros))
        {
            throw new InvalidOperationException("No fue posible guardar la foto de perfil.");
        }
    }

    public ConfiguracionAutenticacionDosFactores? ConsultarAutenticacionDosFactores(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID) };
        DataTable resultado = _conexion.Leer("dbo.usp_Usuario2FA_Consultar", parametros);
        if (resultado.Rows.Count != 1) return null;
        DataRow fila = resultado.Rows[0];
        return new ConfiguracionAutenticacionDosFactores(Convert.ToInt32(fila["IdUsuario"]), fila["SecretoTotpCifrado"] == DBNull.Value ? null : Convert.ToString(fila["SecretoTotpCifrado"]), fila["SecretoPendienteCifrado"] == DBNull.Value ? null : Convert.ToString(fila["SecretoPendienteCifrado"]), fila["FechaExpiracionPendiente"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaExpiracionPendiente"]), Convert.ToInt32(fila["IntentosPendientes"]), Convert.ToBoolean(fila["TotpHabilitado"]), fila["FechaActivacionTotp"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaActivacionTotp"]), fila["UltimoPasoTotp"] == DBNull.Value ? null : Convert.ToInt64(fila["UltimoPasoTotp"]));
    }

    public void GuardarAutenticacionDosFactoresPendiente(Usuario usuario, string secretoCifrado, DateTime fechaExpiracion)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@SecretoPendienteCifrado", secretoCifrado), new SqlParameter("@FechaExpiracionPendiente", fechaExpiracion) };
        _conexion.Escribir("dbo.usp_Usuario2FA_GuardarPendiente", parametros);
    }

    public void ActivarAutenticacionDosFactores(Usuario usuario, string secretoCifrado, long pasoTotp)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@SecretoTotpCifrado", secretoCifrado), new SqlParameter("@PasoTotp", pasoTotp) };
        _conexion.Escribir("dbo.usp_Usuario2FA_Activar", parametros);
    }

    public void RegistrarIntentoAutenticacionDosFactoresPendiente(Usuario usuario)
    {
        _conexion.Escribir("dbo.usp_Usuario2FA_RegistrarIntentoPendiente", new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID) });
    }

    public void ActualizarUltimoPasoTotp(Usuario usuario, long pasoTotp)
    {
        _conexion.Escribir("dbo.usp_Usuario2FA_ActualizarUltimoPaso", new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@PasoTotp", pasoTotp) });
    }

    public void DesactivarAutenticacionDosFactores(Usuario usuario)
    {
        _conexion.Escribir("dbo.usp_Usuario2FA_Desactivar", new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID) });
    }

    public void ReemplazarCodigosRecuperacionDosFactores(Usuario usuario, List<string> hashes)
    {
        DataTable codigos = new DataTable(); codigos.Columns.Add("Valor", typeof(string));
        foreach (string hash in hashes) codigos.Rows.Add(hash);
        SqlParameter parametro = new SqlParameter("@Codigos", SqlDbType.Structured) { TypeName = "dbo.ListaTexto", Value = codigos };
        _conexion.Escribir("dbo.usp_Usuario2FA_ReemplazarCodigosRecuperacion", new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID), parametro });
    }

    public bool ConsumirCodigoRecuperacionDosFactores(Usuario usuario, string codigoHash)
    {
        DataTable resultado = _conexion.Leer("dbo.usp_CodigoRecuperacion2FA_Consumir", new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@CodigoHash", codigoHash) });
        return resultado.Rows.Count == 1 && Convert.ToBoolean(resultado.Rows[0]["Consumido"]);
    }

    public void CrearDesafioAutenticacionDosFactores(Usuario usuario, string tokenHash, bool mantenerSesion, DateTime fechaExpiracion)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@TokenHash", tokenHash), new SqlParameter("@MantenerSesion", mantenerSesion), new SqlParameter("@FechaExpiracion", fechaExpiracion) };
        _conexion.Escribir("dbo.usp_Desafio2FA_Crear", parametros);
    }

    public DesafioAutenticacionDosFactores ConsumirDesafioAutenticacionDosFactores(string tokenHash, bool exitoso)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@TokenHash", tokenHash), new SqlParameter("@Exitoso", exitoso) };
        DataTable resultado = _conexion.Leer("dbo.usp_Desafio2FA_Consumir", parametros);
        if (resultado.Rows.Count != 1) return new DesafioAutenticacionDosFactores();
        DataRow fila = resultado.Rows[0];
        return new DesafioAutenticacionDosFactores(Convert.ToBoolean(fila["Valido"]), fila["IdUsuario"] == DBNull.Value ? null : Convert.ToInt32(fila["IdUsuario"]), fila["MantenerSesion"] != DBNull.Value && Convert.ToBoolean(fila["MantenerSesion"]));
    }

    public EstadoTelegram ConsultarEstadoTelegram(Usuario usuario)
    {
        DataTable resultado = _conexion.Leer("dbo.usp_Telegram_ConsultarEstado", new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID) });
        if (resultado.Rows.Count != 1) return new EstadoTelegram();
        DataRow fila = resultado.Rows[0];
        return new EstadoTelegram(Convert.ToBoolean(fila["Vinculado"]), fila["EstadoSolicitud"] == DBNull.Value ? string.Empty : Convert.ToString(fila["EstadoSolicitud"]) ?? string.Empty, fila["FechaExpiracionSolicitud"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaExpiracionSolicitud"]), fila["FechaVinculacion"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaVinculacion"]));
    }

    public void CrearSolicitudTelegram(Usuario usuario, string tokenHash, string tipo, DateTime fechaExpiracion)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@TokenHash", tokenHash), new SqlParameter("@Tipo", tipo), new SqlParameter("@FechaExpiracion", fechaExpiracion) };
        _conexion.Escribir("dbo.usp_Telegram_CrearSolicitud", parametros);
    }

    public EntregaTelegram ProcesarInicioTelegram(string tokenHash, long telegramUserId, long chatId, string? nombreUsuario, string codigoHash, DateTime fechaExpiracionCodigo, string codigo)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@TokenHash", tokenHash), new SqlParameter("@TelegramUserId", telegramUserId), new SqlParameter("@ChatId", chatId), new SqlParameter("@NombreUsuario", (object?)nombreUsuario ?? DBNull.Value), new SqlParameter("@CodigoHash", codigoHash), new SqlParameter("@FechaExpiracionCodigo", fechaExpiracionCodigo),
        };
        DataTable resultado = _conexion.Leer("dbo.usp_Telegram_ProcesarInicio", parametros);
        if (resultado.Rows.Count != 1 || !Convert.ToBoolean(resultado.Rows[0]["Valida"])) return new EntregaTelegram();
        return new EntregaTelegram(true, Convert.ToInt64(resultado.Rows[0]["ChatId"]), codigo, resultado.Rows[0].Table.Columns.Contains("Tipo") ? Convert.ToString(resultado.Rows[0]["Tipo"]) ?? string.Empty : string.Empty);
    }

    public bool ConfirmarCodigoTelegram(Usuario usuario, string tipo, string codigoHash)
    {
        DataTable resultado = _conexion.Leer("dbo.usp_Telegram_ConfirmarCodigo", new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@Tipo", tipo), new SqlParameter("@CodigoHash", codigoHash) });
        return resultado.Rows.Count == 1 && Convert.ToBoolean(resultado.Rows[0]["Confirmada"]);
    }

    public EntregaTelegram ReenviarCodigoTelegram(Usuario usuario, string tipo, string codigoHash, DateTime fechaExpiracionCodigo, string codigo)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@Tipo", tipo), new SqlParameter("@CodigoHash", codigoHash), new SqlParameter("@FechaExpiracionCodigo", fechaExpiracionCodigo) };
        DataTable resultado = _conexion.Leer("dbo.usp_Telegram_ReenviarCodigo", parametros);
        if (resultado.Rows.Count != 1 || !Convert.ToBoolean(resultado.Rows[0]["Valida"])) return new EntregaTelegram();
        return new EntregaTelegram(true, Convert.ToInt64(resultado.Rows[0]["ChatId"]), codigo, tipo);
    }

    public EntregaTelegram CrearSolicitudDesvinculacionTelegram(Usuario usuario, string codigoHash, DateTime fechaExpiracionCodigo, string codigo)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@CodigoHash", codigoHash), new SqlParameter("@FechaExpiracionCodigo", fechaExpiracionCodigo) };
        DataTable resultado = _conexion.Leer("dbo.usp_Telegram_CrearDesvinculacion", parametros);
        if (resultado.Rows.Count != 1 || !Convert.ToBoolean(resultado.Rows[0]["Valida"])) return new EntregaTelegram();
        return new EntregaTelegram(true, Convert.ToInt64(resultado.Rows[0]["ChatId"]), codigo, "Desvinculacion");
    }

    public void CrearSesionValidacionRegistro(Usuario usuario, string tokenHash, DateTime fechaExpiracion)
    {
        _conexion.Escribir("dbo.usp_RegistroValidacion_CrearSesion", new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@TokenHash", tokenHash), new SqlParameter("@FechaExpiracion", fechaExpiracion) });
    }

    public int? ConsultarUsuarioSesionValidacionRegistro(string tokenHash)
    {
        DataTable resultado = _conexion.Leer("dbo.usp_RegistroValidacion_ConsultarSesion", new List<SqlParameter>() { new SqlParameter("@TokenHash", tokenHash) });
        return resultado.Rows.Count == 1 ? Convert.ToInt32(resultado.Rows[0]["IdUsuario"]) : null;
    }

    public List<Usuario> ConsultarUsuariosAgencia(Usuario solicitante)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdAgencia", solicitante.IdAgencia) };
        DataTable resultado = _conexion.Leer("dbo.usp_Usuario_ConsultarPorAgencia", parametros);
        return resultado.Rows.Cast<DataRow>().Select(CrearUsuarioConEmpleado).ToList();
    }

    public List<Usuario> ConsultarOperadores()
    {
        DataTable resultado = _conexion.Leer("dbo.usp_Usuario_ConsultarOperadores");
        return resultado.Rows.Cast<DataRow>().Select(CrearUsuario).ToList();
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

    public void CambiarEstado(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@Activo", usuario.Activo) };
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
        usuario.FotoPerfil = fila.Table.Columns.Contains("FotoPerfil") && fila["FotoPerfil"] != DBNull.Value
            ? Convert.ToString(fila["FotoPerfil"])
            : null;
        return usuario;
    }

    private Usuario CargarFotoPerfil(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID) };
        DataTable resultado = _conexion.Leer("dbo.usp_Usuario_ConsultarFotoPerfil", parametros);
        usuario.FotoPerfil = resultado.Rows.Count == 1 && resultado.Rows[0]["FotoPerfil"] != DBNull.Value
            ? Convert.ToString(resultado.Rows[0]["FotoPerfil"])
            : null;
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

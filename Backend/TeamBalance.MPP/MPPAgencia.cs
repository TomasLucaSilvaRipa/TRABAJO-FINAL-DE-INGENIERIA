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

    public Agencia ConsultarAgencia(int idAgencia)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdAgencia", idAgencia) };
        DataTable resultado = _conexion.Leer("dbo.usp_Agencia_Consultar", parametros);
        if (resultado.Rows.Count != 1) { throw new KeyNotFoundException("No existe la agencia solicitada."); }
        DataRow fila = resultado.Rows[0];
        Agencia agencia = new Agencia(Convert.ToInt32(fila["ID"]), Convert.ToString(fila["NombreComercial"]) ?? string.Empty, Convert.ToString(fila["RazonSocial"]) ?? string.Empty, Convert.ToString(fila["CUIT"]) ?? string.Empty, Convert.ToString(fila["EmailContacto"]) ?? string.Empty, Convert.ToString(fila["TelefonoContacto"]) ?? string.Empty, Convert.ToDateTime(fila["FechaAlta"]), Convert.ToString(fila["Estado"]) ?? string.Empty, Convert.ToBoolean(fila["Activo"]), new List<Proyecto>(), new List<Usuario>());
        agencia.CondicionFiscal = Convert.ToString(fila["CondicionFiscal"]);
        agencia.FechaBaja = fila["FechaBaja"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaBaja"]);
        return agencia;
    }

    public Agencia ModificarAgencia(Agencia agencia)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdAgencia", agencia.ID), new SqlParameter("@NombreComercial", agencia.NombreComercial), new SqlParameter("@RazonSocial", (object?)agencia.RazonSocial ?? DBNull.Value), new SqlParameter("@CondicionFiscal", (object?)agencia.CondicionFiscal ?? DBNull.Value), new SqlParameter("@EmailContacto", agencia.EmailContacto), new SqlParameter("@TelefonoContacto", (object?)agencia.TelefonoContacto ?? DBNull.Value) };
        DataTable resultado = _conexion.Leer("dbo.usp_Agencia_Modificar", parametros);
        if (resultado.Rows.Count != 1) { throw new KeyNotFoundException("No existe la agencia solicitada."); }
        DataRow fila = resultado.Rows[0];
        Agencia agenciaActualizada = new Agencia(Convert.ToInt32(fila["ID"]), Convert.ToString(fila["NombreComercial"]) ?? string.Empty, Convert.ToString(fila["RazonSocial"]) ?? string.Empty, Convert.ToString(fila["CUIT"]) ?? string.Empty, Convert.ToString(fila["EmailContacto"]) ?? string.Empty, Convert.ToString(fila["TelefonoContacto"]) ?? string.Empty, Convert.ToDateTime(fila["FechaAlta"]), Convert.ToString(fila["Estado"]) ?? string.Empty, Convert.ToBoolean(fila["Activo"]), new List<Proyecto>(), new List<Usuario>());
        agenciaActualizada.CondicionFiscal = Convert.ToString(fila["CondicionFiscal"]);
        return agenciaActualizada;
    }

    public Suscripcion? ConsultarSuscripcionActual(int idAgencia)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdAgencia", idAgencia) };
        DataTable resultado = _conexion.Leer("dbo.usp_Suscripcion_ConsultarActualPorAgencia", parametros);
        if (resultado.Rows.Count == 0) { return null; }
        DataRow fila = resultado.Rows[0];
        Suscripcion suscripcion = new Suscripcion(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdAgencia"]), Convert.ToInt32(fila["IdPlanComercial"]), Convert.ToString(fila["ReferenciaExterna"]), Convert.ToString(fila["Estado"]) ?? string.Empty, Convert.ToDateTime(fila["FechaAlta"]), Convert.ToDateTime(fila["FechaVencimiento"]), fila["FechaProximaRenovacion"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaProximaRenovacion"]), Convert.ToBoolean(fila["RenovacionAutomatica"]), Convert.ToDecimal(fila["ImporteVigente"]), Convert.ToBoolean(fila["Activo"]), fila["FechaBaja"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaBaja"]));
        suscripcion.NombrePlan = Convert.ToString(fila["NombrePlan"]);
        suscripcion.PeriodicidadPlan = Convert.ToString(fila["PeriodicidadPlan"]);
        suscripcion.Moneda = Convert.ToString(fila["Moneda"]);
        return suscripcion;
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

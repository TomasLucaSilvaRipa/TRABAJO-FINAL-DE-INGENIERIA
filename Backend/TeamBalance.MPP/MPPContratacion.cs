using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public sealed class MPPContratacion
{
    private readonly Conexion _conexion;

    public MPPContratacion(Conexion conexion)
    {
        _conexion = conexion;
    }

    public ContratacionPendiente CrearPendiente(ContratacionRequest request, string referenciaContratacion, string referenciaOperacion)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@ReferenciaContratacion", referenciaContratacion),
            new SqlParameter("@ReferenciaOperacion", referenciaOperacion),
            new SqlParameter("@NombreComercialAgencia", request.NombreComercialAgencia),
            new SqlParameter("@RazonSocial", request.RazonSocial),
            new SqlParameter("@CUIT", request.CUIT),
            new SqlParameter("@CondicionFiscal", request.CondicionFiscal),
            new SqlParameter("@EmailFacturacion", request.EmailFacturacion),
            new SqlParameter("@TelefonoContacto", request.TelefonoContacto),
            new SqlParameter("@NombreResponsable", request.NombreResponsable),
            new SqlParameter("@ApellidoResponsable", request.ApellidoResponsable),
            new SqlParameter("@EmailLaboralResponsable", request.EmailLaboralResponsable),
            new SqlParameter("@CargoResponsable", request.CargoResponsable),
            new SqlParameter("@ProveedorPagoSeleccionado", request.ProveedorPagoSeleccionado),
            new SqlParameter("@IdPlanComercial", request.IdPlanComercial),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Contratacion_CrearPendiente", parametros);

        if (resultado.Rows.Count != 1)
        {
            throw new InvalidOperationException("No fue posible iniciar la contratación.");
        }

        DataRow fila = resultado.Rows[0];

        ContratacionPendiente contratacion = new ContratacionPendiente(Convert.ToInt32(fila["IdContratacion"]), Convert.ToInt32(fila["IdPlanComercial"]), Convert.ToString(fila["ReferenciaContratacion"]) ?? string.Empty, Convert.ToString(fila["ReferenciaOperacion"]) ?? string.Empty, Convert.ToDecimal(fila["Importe"]), Convert.ToString(fila["Moneda"]) ?? string.Empty);
        return contratacion;
    }

    public EstadoContratacionPersistido ConsultarEstado(string referenciaContratacion)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@ReferenciaContratacion", referenciaContratacion),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Contratacion_ConsultarEstado", parametros);

        if (resultado.Rows.Count != 1){ throw new KeyNotFoundException("No existe una contratación con la referencia indicada."); }

        DataRow fila = resultado.Rows[0];

        EstadoContratacionPersistido estado = new EstadoContratacionPersistido(Convert.ToString(fila["ReferenciaContratacion"]) ?? string.Empty, Convert.ToString(fila["EstadoContratacion"]) ?? string.Empty, Convert.ToDecimal(fila["Importe"]), Convert.ToString(fila["Moneda"]) ?? string.Empty);
        return estado;
    }

    public ContratacionServicio ConsultarContratacionParaRegistro(string referenciaContratacion)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@ReferenciaContratacion", referenciaContratacion),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Contratacion_ConsultarParaRegistro", parametros);

        if (resultado.Rows.Count != 1)
        {
            throw new KeyNotFoundException("No existe una contratación aprobada disponible para registrar la agencia.");
        }

        DataRow fila = resultado.Rows[0];

        ContratacionServicio contratacion = new ContratacionServicio();
        contratacion.ID = Convert.ToInt32(fila["ID"]);
        contratacion.IdAgencia = fila["IdAgencia"] == DBNull.Value ? null : Convert.ToInt32(fila["IdAgencia"]);
        contratacion.IdUsuario = fila["IdUsuario"] == DBNull.Value ? null : Convert.ToInt32(fila["IdUsuario"]);
        contratacion.ReferenciaContratacion = Convert.ToString(fila["ReferenciaContratacion"]) ?? string.Empty;
        contratacion.NombreComercialAgencia = Convert.ToString(fila["NombreComercialAgencia"]) ?? string.Empty;
        contratacion.RazonSocial = Convert.ToString(fila["RazonSocial"]);
        contratacion.CUIT = Convert.ToString(fila["CUIT"]) ?? string.Empty;
        contratacion.CondicionFiscal = Convert.ToString(fila["CondicionFiscal"]);
        contratacion.EmailFacturacion = Convert.ToString(fila["EmailFacturacion"]);
        contratacion.TelefonoContacto = Convert.ToString(fila["TelefonoContacto"]);
        contratacion.NombreResponsable = Convert.ToString(fila["NombreResponsable"]) ?? string.Empty;
        contratacion.ApellidoResponsable = Convert.ToString(fila["ApellidoResponsable"]) ?? string.Empty;
        contratacion.EmailLaboralResponsable = Convert.ToString(fila["EmailLaboralResponsable"]) ?? string.Empty;
        contratacion.EstadoContratacion = Convert.ToString(fila["EstadoContratacion"]) ?? string.Empty;
        contratacion.Activo = Convert.ToBoolean(fila["Activo"]);
        return contratacion;
    }

    public EstadoContratacionPersistido ActualizarResultadoPago(string referenciaContratacion, string referenciaProveedor, string estadoProveedor, string mensajeRespuesta)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@ReferenciaContratacion", referenciaContratacion),
            new SqlParameter("@ReferenciaProveedor", referenciaProveedor),
            new SqlParameter("@EstadoProveedor", estadoProveedor),
            new SqlParameter("@MensajeRespuesta", mensajeRespuesta),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Contratacion_ActualizarResultadoPago", parametros);

        if (resultado.Rows.Count != 1)
        {
            throw new InvalidOperationException("No fue posible actualizar el estado del pago.");
        }

        DataRow fila = resultado.Rows[0];

        EstadoContratacionPersistido estado = new EstadoContratacionPersistido(Convert.ToString(fila["ReferenciaContratacion"]) ?? string.Empty, Convert.ToString(fila["EstadoContratacion"]) ?? string.Empty, Convert.ToDecimal(fila["Importe"]), Convert.ToString(fila["Moneda"]) ?? string.Empty);
        return estado;
    }
}

public sealed class EstadoContratacionPersistido
{
    public EstadoContratacionPersistido(string referenciaContratacion, string estadoContratacion, decimal importe, string moneda)
    {
        ReferenciaContratacion = referenciaContratacion;
        EstadoContratacion = estadoContratacion;
        Importe = importe;
        Moneda = moneda;
    }

    public string ReferenciaContratacion { get; set; }
    public string EstadoContratacion { get; set; }
    public decimal Importe { get; set; }
    public string Moneda { get; set; }
}

using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public sealed class MPPSuscripcion
{
    private readonly Conexion _conexion;

    public MPPSuscripcion(Conexion conexion) => _conexion = conexion;

    public Suscripcion? ConsultarActual(Usuario usuario)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Suscripcion_ConsultarActualPorAgencia", new List<SqlParameter>() { new SqlParameter("@IdAgencia", usuario.IdAgencia ?? throw new UnauthorizedAccessException("El usuario no pertenece a una agencia.")) });
        return tabla.Rows.Count == 0 ? null : CrearSuscripcion(tabla.Rows[0]);
    }

    public List<OperacionSuscripcionHistorial> ConsultarHistorial(Usuario usuario)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Suscripcion_ConsultarHistorialPorAgencia", new List<SqlParameter>() { new SqlParameter("@IdAgencia", usuario.IdAgencia ?? throw new UnauthorizedAccessException("El usuario no pertenece a una agencia.")) });
        return tabla.Rows.Cast<DataRow>().Select(fila => new OperacionSuscripcionHistorial(Convert.ToInt32(fila["ID"]), Convert.ToString(fila["TipoOperacion"]) ?? string.Empty, fila["Modalidad"] == DBNull.Value ? null : Convert.ToString(fila["Modalidad"]), fila["Importe"] == DBNull.Value ? null : Convert.ToDecimal(fila["Importe"]), fila["Moneda"] == DBNull.Value ? null : Convert.ToString(fila["Moneda"]), fila["Proveedor"] == DBNull.Value ? null : Convert.ToString(fila["Proveedor"]), fila["Referencia"] == DBNull.Value ? null : Convert.ToString(fila["Referencia"]), Convert.ToString(fila["Estado"]) ?? string.Empty, fila["Detalle"] == DBNull.Value ? null : Convert.ToString(fila["Detalle"]), Convert.ToDateTime(fila["Fecha"]))).ToList();
    }

    public OperacionSuscripcionPendiente CrearSolicitudCambioPlan(CambioPlanSuscripcionRequest solicitud)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Suscripcion_CrearSolicitudCambioPlan", new List<SqlParameter>
        {
            new SqlParameter("@IdSuscripcion", solicitud.IdSuscripcion), new SqlParameter("@IdPlanNuevo", solicitud.IdPlanComercial), new SqlParameter("@ReferenciaInterna", solicitud.ReferenciaInterna), new SqlParameter("@Proveedor", solicitud.Proveedor),
        });
        if (tabla.Rows.Count != 1) throw new InvalidOperationException("No fue posible generar la solicitud de actualización.");
        DataRow fila = tabla.Rows[0];
        return new OperacionSuscripcionPendiente(Convert.ToInt32(fila["IdOperacion"]), Convert.ToInt32(fila["IdSuscripcion"]), Convert.ToInt32(fila["IdPlanNuevo"]), Convert.ToString(fila["ReferenciaInterna"]) ?? string.Empty, Convert.ToDecimal(fila["Importe"]), Convert.ToString(fila["Moneda"]) ?? string.Empty, Convert.ToString(fila["Proveedor"]) ?? string.Empty);
    }

    public OperacionSuscripcionPendiente CrearSolicitudReactivacionConCreditoParcial(CambioPlanSuscripcionRequest solicitud)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_CuentaCorriente_CrearSolicitudReactivacionConCreditoParcial", new List<SqlParameter>
        {
            new SqlParameter("@IdSuscripcion", solicitud.IdSuscripcion), new SqlParameter("@IdPlanNuevo", solicitud.IdPlanComercial), new SqlParameter("@ReferenciaInterna", solicitud.ReferenciaInterna), new SqlParameter("@Proveedor", solicitud.Proveedor), new SqlParameter("@ImportePago", solicitud.ImportePago), new SqlParameter("@MonedaPago", solicitud.MonedaPago), new SqlParameter("@CreditoAplicado", solicitud.CreditoAplicado)
        });
        if (tabla.Rows.Count != 1) throw new InvalidOperationException("No fue posible preparar el pago de la diferencia.");
        DataRow fila = tabla.Rows[0];
        return new OperacionSuscripcionPendiente(Convert.ToInt32(fila["IdOperacion"]), Convert.ToInt32(fila["IdSuscripcion"]), Convert.ToInt32(fila["IdPlanNuevo"]), Convert.ToString(fila["ReferenciaInterna"]) ?? string.Empty, Convert.ToDecimal(fila["Importe"]), Convert.ToString(fila["Moneda"]) ?? string.Empty, Convert.ToString(fila["Proveedor"]) ?? string.Empty);
    }

    public OperacionSuscripcionPendiente ConsultarOperacionPendiente(OperacionSuscripcionPendiente operacion)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Suscripcion_ConsultarOperacionPendiente", new List<SqlParameter>() { new SqlParameter("@ReferenciaInterna", operacion.ReferenciaInterna) });
        if (tabla.Rows.Count != 1) throw new KeyNotFoundException("No existe una actualización de suscripción pendiente con la referencia indicada.");
        DataRow fila = tabla.Rows[0];
        return new OperacionSuscripcionPendiente(Convert.ToInt32(fila["IdOperacion"]), Convert.ToInt32(fila["IdSuscripcion"]), Convert.ToInt32(fila["IdPlanNuevo"]), Convert.ToString(fila["ReferenciaInterna"]) ?? string.Empty, Convert.ToDecimal(fila["Importe"]), Convert.ToString(fila["Moneda"]) ?? string.Empty, Convert.ToString(fila["Proveedor"]) ?? string.Empty, Convert.ToString(fila["TipoOperacion"]) ?? string.Empty);
    }

    public Suscripcion AplicarResultadoCambioPlan(OperacionSuscripcionPendiente operacion)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Suscripcion_AplicarResultadoCambioPlan", new List<SqlParameter>
        {
            new SqlParameter("@ReferenciaInterna", operacion.ReferenciaInterna), new SqlParameter("@ReferenciaProveedor", operacion.ReferenciaProveedor), new SqlParameter("@EstadoProveedor", operacion.EstadoProveedor), new SqlParameter("@Detalle", operacion.DetalleProveedor),
        });
        if (tabla.Rows.Count != 1) throw new InvalidOperationException("No fue posible actualizar el resultado de la suscripción.");
        return CrearSuscripcion(tabla.Rows[0]);
    }

    public Suscripcion ActualizarRenovacion(Suscripcion suscripcion)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Suscripcion_ActualizarRenovacion", new List<SqlParameter>
        {
            new SqlParameter("@IdSuscripcion", suscripcion.ID), new SqlParameter("@Activa", suscripcion.RenovacionAutomatica), new SqlParameter("@Motivo", (object?)suscripcion.MotivoRenovacion ?? DBNull.Value),
        });
        if (tabla.Rows.Count != 1) throw new KeyNotFoundException("No existe una suscripción activa para actualizar.");
        return CrearSuscripcion(tabla.Rows[0]);
    }

    public bool PuedeUsarAgencia(Usuario usuario)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Suscripcion_PuedeUsarAgencia", new List<SqlParameter>() { new SqlParameter("@IdAgencia", usuario.IdAgencia ?? throw new UnauthorizedAccessException("El usuario no pertenece a una agencia.")) });
        return tabla.Rows.Count == 1 && Convert.ToBoolean(tabla.Rows[0]["Vigente"]);
    }

    public List<SincronizacionVencimientosResultado> SincronizarVencimientos()
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Suscripcion_SincronizarVencimientos", new List<SqlParameter>());
        return tabla.Rows.Cast<DataRow>().Select(fila => new SincronizacionVencimientosResultado(Convert.ToInt32(fila["IdSuscripcion"]), Convert.ToInt32(fila["IdAgencia"]), Convert.ToString(fila["Estado"]) ?? string.Empty)).ToList();
    }

    public void RegistrarRenovacionProveedor(Suscripcion suscripcion)
    {
        _conexion.Leer("dbo.usp_Suscripcion_RegistrarRenovacionProveedor", new List<SqlParameter>
        {
            new SqlParameter("@IdSuscripcion", suscripcion.ID), new SqlParameter("@ReferenciaProveedor", suscripcion.ReferenciaRenovacionProveedor), new SqlParameter("@EstadoProveedor", suscripcion.EstadoRenovacionProveedor), new SqlParameter("@Proveedor", suscripcion.ProveedorRenovacion),
        });
    }

    public List<RenovacionProveedorPendiente> ConsultarRenovacionesProveedor()
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Suscripcion_ConsultarRenovacionesProveedor", new List<SqlParameter>());
        return tabla.Rows.Cast<DataRow>().Select(f => new RenovacionProveedorPendiente(Convert.ToInt32(f["IdSuscripcion"]), Convert.ToInt32(f["IdAgencia"]), Convert.ToInt32(f["IdPlanComercial"]), Convert.ToString(f["ReferenciaRenovacionProveedor"]) ?? string.Empty, Convert.ToString(f["ProveedorRenovacion"]) ?? "MercadoPago")).ToList();
    }

    public void AplicarCobroRecurrente(RenovacionProveedorPendiente renovacion)
    {
        _conexion.Leer("dbo.usp_Suscripcion_AplicarCobroRecurrente", new List<SqlParameter>
        {
            new SqlParameter("@IdSuscripcion", renovacion.IdSuscripcion), new SqlParameter("@ReferenciaPago", renovacion.ReferenciaPago), new SqlParameter("@Importe", renovacion.Importe), new SqlParameter("@Moneda", renovacion.Moneda), new SqlParameter("@Detalle", renovacion.Detalle), new SqlParameter("@Proveedor", renovacion.Proveedor),
        });
    }

    public void ActualizarEstadoRenovacionProveedor(Suscripcion suscripcion)
    {
        _conexion.Leer("dbo.usp_Suscripcion_ActualizarEstadoRenovacionProveedor", new List<SqlParameter>
        {
            new SqlParameter("@IdSuscripcion", suscripcion.ID), new SqlParameter("@EstadoProveedor", suscripcion.EstadoRenovacionProveedor),
        });
    }

    private static Suscripcion CrearSuscripcion(DataRow fila)
    {
        Suscripcion suscripcion = new Suscripcion(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdAgencia"]), Convert.ToInt32(fila["IdPlanComercial"]),
            fila["ReferenciaExterna"] == DBNull.Value ? null : Convert.ToString(fila["ReferenciaExterna"]), Convert.ToString(fila["Estado"]) ?? string.Empty,
            Convert.ToDateTime(fila["FechaAlta"]), Convert.ToDateTime(fila["FechaVencimiento"]), fila["FechaProximaRenovacion"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaProximaRenovacion"]),
            Convert.ToBoolean(fila["RenovacionAutomatica"]), Convert.ToDecimal(fila["ImporteVigente"]), Convert.ToBoolean(fila["Activo"]), fila["FechaBaja"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaBaja"]));
        suscripcion.NombrePlan = fila.Table.Columns.Contains("NombrePlan") ? Convert.ToString(fila["NombrePlan"]) : null;
        suscripcion.PeriodicidadPlan = fila.Table.Columns.Contains("PeriodicidadPlan") ? Convert.ToString(fila["PeriodicidadPlan"]) : null;
        suscripcion.Moneda = fila.Table.Columns.Contains("Moneda") ? Convert.ToString(fila["Moneda"]) : null;
        suscripcion.ReferenciaRenovacionProveedor = fila.Table.Columns.Contains("ReferenciaRenovacionProveedor") && fila["ReferenciaRenovacionProveedor"] != DBNull.Value ? Convert.ToString(fila["ReferenciaRenovacionProveedor"]) : null;
        suscripcion.EstadoRenovacionProveedor = fila.Table.Columns.Contains("EstadoRenovacionProveedor") && fila["EstadoRenovacionProveedor"] != DBNull.Value ? Convert.ToString(fila["EstadoRenovacionProveedor"]) : null;
        suscripcion.ProveedorRenovacion = fila.Table.Columns.Contains("ProveedorRenovacion") && fila["ProveedorRenovacion"] != DBNull.Value ? Convert.ToString(fila["ProveedorRenovacion"]) : null;
        suscripcion.FechaSincronizacionRenovacion = fila.Table.Columns.Contains("FechaSincronizacionRenovacion") && fila["FechaSincronizacionRenovacion"] != DBNull.Value ? Convert.ToDateTime(fila["FechaSincronizacionRenovacion"]) : null;
        return suscripcion;
    }
}

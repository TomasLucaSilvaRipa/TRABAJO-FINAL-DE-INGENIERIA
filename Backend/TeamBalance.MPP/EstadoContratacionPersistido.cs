using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;
public sealed class EstadoContratacionPersistido
{
    public EstadoContratacionPersistido(string referenciaContratacion, string estadoContratacion, decimal importe, string moneda, string proveedorPagoSeleccionado, int? idAgencia, int? idUsuario)
    {
        ReferenciaContratacion = referenciaContratacion;
        EstadoContratacion = estadoContratacion;
        Importe = importe;
        Moneda = moneda;
        ProveedorPagoSeleccionado = proveedorPagoSeleccionado;
        IdAgencia = idAgencia;
        IdUsuario = idUsuario;
    }

    public string ReferenciaContratacion { get; set; }
    public string EstadoContratacion { get; set; }
    public decimal Importe { get; set; }
    public string Moneda { get; set; }
    public string ProveedorPagoSeleccionado { get; set; }
    public int? IdAgencia { get; set; }
    public int? IdUsuario { get; set; }
}

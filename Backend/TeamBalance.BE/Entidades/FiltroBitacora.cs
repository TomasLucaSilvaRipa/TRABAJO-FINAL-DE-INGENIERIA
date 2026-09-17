using System;
using System.Collections.Generic;
using System.Text;

namespace TeamBalance.BE.Entidades
{
    public class FiltroBitacora
    {
        public FiltroBitacora() { }
        public FiltroBitacora(int? idAgencia = null, DateTime? desde = null, DateTime? hasta = null, int? idUsuario = null, string? entidad = null, string? accion = null, string? resultado = null, string? criticidad = null, string? modulo = null)
        {
            IdAgencia = idAgencia;
            Desde = desde;
            Hasta = hasta;
            IdUsuario = idUsuario;
            Entidad = entidad;
            Accion = accion;
            Resultado = resultado;
            Criticidad = criticidad;
            Modulo = modulo;
        }
        public int? IdAgencia { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public int? IdUsuario { get; set; }
        public string? Entidad { get; set; }
        public string? Accion { get; set; }
        public string? Resultado { get; set; }
        public string? Criticidad { get; set; }
        public string? Modulo { get; set; }
    }
}

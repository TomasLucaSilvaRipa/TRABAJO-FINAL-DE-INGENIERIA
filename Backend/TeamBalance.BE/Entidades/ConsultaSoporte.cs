using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class ConsultaSoporte
{
    public int ID { get; set; }

    public int IdUsuario { get; set; }

    public int? IdAgencia { get; set; }

    public string Asunto { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string? AdjuntosJson { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public bool Activo { get; set; }

    public DateTime? FechaBaja { get; set; }

    public virtual Agencia? IdAgenciaNavigation { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<MensajeConsultaSoporte> MensajeConsultaSoportes { get; set; } = new List<MensajeConsultaSoporte>();
}

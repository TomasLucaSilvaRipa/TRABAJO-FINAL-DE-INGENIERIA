using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Usuario
{
    public int ID { get; set; }

    public int? IdAgencia { get; set; }

    public int IdRol { get; set; }

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public DateTime FechaAlta { get; set; }

    public bool Activo { get; set; }

    public DateTime? FechaBaja { get; set; }

    public virtual ICollection<AceptacionTermino> AceptacionTerminos { get; set; } = new List<AceptacionTermino>();

    public virtual ICollection<AsignacionTarea> AsignacionTareas { get; set; } = new List<AsignacionTarea>();

    public virtual ICollection<AusenciaEmpleado> AusenciaEmpleados { get; set; } = new List<AusenciaEmpleado>();

    public virtual ICollection<Bitacora> Bitacoras { get; set; } = new List<Bitacora>();

    public virtual ICollection<ConsultaSoporte> ConsultaSoportes { get; set; } = new List<ConsultaSoporte>();

    public virtual ICollection<ContratacionServicio> ContratacionServicios { get; set; } = new List<ContratacionServicio>();

    public virtual Dueño? Dueño { get; set; }

    public virtual Empleado? Empleado { get; set; }

    public virtual Agencia? IdAgenciaNavigation { get; set; }

    public virtual Rol IdRolNavigation { get; set; } = null!;

    public virtual ICollection<MensajeConsultaSoporte> MensajeConsultaSoportes { get; set; } = new List<MensajeConsultaSoporte>();

    public virtual PM? PM { get; set; }

    public virtual ICollection<RecomendacionBestFit> RecomendacionBestFits { get; set; } = new List<RecomendacionBestFit>();

    public virtual ICollection<SesionUsuario> SesionUsuarios { get; set; } = new List<SesionUsuario>();

    public virtual ICollection<SimulacionImpacto> SimulacionImpactos { get; set; } = new List<SimulacionImpacto>();

    public virtual ICollection<ValidacionCuentum> ValidacionCuenta { get; set; } = new List<ValidacionCuentum>();
}

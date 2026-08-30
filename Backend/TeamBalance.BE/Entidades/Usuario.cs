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

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? PasswordActual { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? RecaptchaToken { get; set; }


    public virtual ICollection<AsignacionTarea> AsignacionTareas { get; set; } = new List<AsignacionTarea>();

    public virtual ICollection<AusenciaEmpleado> AusenciaEmpleados { get; set; } = new List<AusenciaEmpleado>();



    public virtual Rol IdRolNavigation { get; set; } = null!;

    
}

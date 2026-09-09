using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Usuario
{

    public Usuario()
    {
        Rol = new Rol();
        Roles = new List<Rol>();
        Permisos = new List<Permiso>();
    }

    public Usuario(int id, int? idAgencia, Rol rol, string nombre, string apellido, string email, string passwordHash, string estado, DateTime fechaAlta, bool activo)
    {
        ID = id;
        IdAgencia = idAgencia;
        Rol = rol;
        Nombre = nombre;
        Apellido = apellido;
        Email = email;
        PasswordHash = passwordHash;
        Estado = estado;
        FechaAlta = fechaAlta;
        Activo = activo;
        Roles = new List<Rol>();
        Permisos = new List<Permiso>();
    }

    public int ID { get; set; }

    public int? IdAgencia { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public Rol Rol { get; set; }

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


    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public List<Rol> Roles { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public List<Permiso> Permisos { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public Empleado? Empleado { get; set; }


    
}

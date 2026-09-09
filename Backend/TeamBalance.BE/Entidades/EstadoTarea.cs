using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class EstadoTarea
{
    public EstadoTarea()
    {
    }

    public EstadoTarea(int iD, int idAgencia, string nombre, int orden, bool esBase, bool esFinal, bool activo)
    {
        ID = iD;
        IdAgencia = idAgencia;
        Nombre = nombre;
        Orden = orden;
        EsBase = esBase;
        EsFinal = esFinal;
        Activo = activo;
    }

    public int ID { get; set; }

    public int IdAgencia { get; set; }

    public string Nombre { get; set; } = null!;

    public int Orden { get; set; }

    public bool EsBase { get; set; }

    public bool EsFinal { get; set; }

    public bool Activo { get; set; }
}

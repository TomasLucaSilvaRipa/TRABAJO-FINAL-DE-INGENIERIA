using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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

    [JsonIgnore, ReferenciaInterna]
    public Agencia Agencia { get; set; } = new();

    public int IdAgencia
    {
        get => Agencia.ID;
        set => Agencia = Agencia.ID == value ? Agencia : new Agencia { ID = value };
    }

    public string Nombre { get; set; } = null!;

    public int Orden { get; set; }

    public bool EsBase { get; set; }

    public bool EsFinal { get; set; }

    public bool Activo { get; set; }
}

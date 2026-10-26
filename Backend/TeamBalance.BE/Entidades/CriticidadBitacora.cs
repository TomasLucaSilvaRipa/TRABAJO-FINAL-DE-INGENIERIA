using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public enum CriticidadBitacora
{
    Informacion = 1,
    Advertencia = 2,
    Critico = 3
}

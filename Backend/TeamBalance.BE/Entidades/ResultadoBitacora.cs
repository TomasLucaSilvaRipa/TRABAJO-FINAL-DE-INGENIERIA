using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public enum ResultadoBitacora
{
    Pendiente = 1,
    Exitoso = 2,
    Parcial = 3,
    Denegado = 4,
    Error = 5
}

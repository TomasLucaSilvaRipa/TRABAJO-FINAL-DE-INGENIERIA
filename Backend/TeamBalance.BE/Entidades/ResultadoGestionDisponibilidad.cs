using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;
public class ResultadoGestionDisponibilidad
{
    public ResultadoGestionDisponibilidad()
    {
    }

    public ResultadoGestionDisponibilidad(AusenciaEmpleado ausencia, List<TareaDisponibilidadAfectada> tareasAfectadas)
    {
        Ausencia = ausencia;
        TareasAfectadas = tareasAfectadas;
    }

    public AusenciaEmpleado Ausencia { get; set; } = new AusenciaEmpleado();
    public List<TareaDisponibilidadAfectada> TareasAfectadas { get; set; } = new List<TareaDisponibilidadAfectada>();
}

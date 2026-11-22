using TeamBalance.BE.Entidades;

namespace Teambalance.API.Models;

internal static class AnaliticaOperativaResponse
{
    internal static object Desde(OcupacionEmpleado resultado) => new
    {
        IdEmpleado = resultado.Empleado.ID,
        Nombre = resultado.Empleado.Nombre,
        Seniority = resultado.Empleado.Seniority,
        resultado.HorasDisponibles,
        resultado.HorasEstimadasActivas,
        resultado.HorasRegistradas,
        resultado.PorcentajeOcupacion,
        resultado.EstadoCarga,
        resultado.CausaPrincipal,
        resultado.AccionSugerida,
        resultado.Proyectos,
    };

    private static object Desde(OcupacionProyecto resultado) => new
    {
        IdProyecto = resultado.Proyecto.ID,
        IdCliente = resultado.Proyecto.IdCliente,
        IdPMResponsable = resultado.Proyecto.IdPMResponsable,
        NombreProyecto = resultado.Proyecto.Nombre,
        NombreCliente = resultado.Proyecto.NombreCliente ?? "Cliente sin nombre",
        resultado.HorasEstimadasActivas,
        resultado.PorcentajeSobreCargaAgencia,
        resultado.Empleados,
    };

    private static object Desde(DetalleOcupacionTarea resultado) => new
    {
        IdTarea = resultado.Tarea.ID,
        IdProyecto = resultado.Proyecto.ID,
        IdPMResponsable = resultado.Proyecto.IdPMResponsable,
        Titulo = resultado.Tarea.Titulo,
        NombreProyecto = resultado.Proyecto.Nombre,
        NombreCliente = resultado.Proyecto.NombreCliente ?? "Cliente sin nombre",
        resultado.HorasEstimadas,
        resultado.IncidenciaSobreCargaEmpleado,
    };

    private static object Desde(DesvioHorasProyecto resultado) => new
    {
        IdProyecto = resultado.Proyecto.ID,
        NombreProyecto = resultado.Proyecto.Nombre,
        NombreCliente = resultado.Proyecto.NombreCliente ?? "Cliente sin nombre",
        resultado.HorasEstimadas,
        resultado.HorasReales,
        resultado.DesvioHoras,
        resultado.HorasRestantes,
        resultado.HorasExcedidas,
        resultado.PorcentajeConsumo,
        resultado.Estado,
        Tareas = resultado.Tareas.Select(Desde).ToList(),
    };

    private static object Desde(DesvioHorasTarea resultado) => new
    {
        IdTarea = resultado.Tarea.ID,
        IdEmpleadoResponsable = resultado.Tarea.IdEmpleadoAsignado,
        Titulo = resultado.Tarea.Titulo,
        Responsable = resultado.EmpleadoResponsable is { } responsable ? responsable.Nombre : "Sin asignar",
        EstadoTarea = resultado.Tarea.Estado ?? string.Empty,
        resultado.HorasEstimadas,
        resultado.HorasReales,
        resultado.DesvioHoras,
        resultado.HorasRestantes,
        resultado.HorasExcedidas,
        resultado.PorcentajeConsumo,
        resultado.Estado,
    };

    internal static object Desde(DetalleDesvioHorasTarea resultado) => new
    {
        IdProyecto = resultado.Proyecto.ID,
        IdTarea = resultado.Tarea.ID,
        NombreProyecto = resultado.Proyecto.Nombre,
        Titulo = resultado.Tarea.Titulo,
        Responsable = resultado.EmpleadoResponsable is { } responsable ? responsable.Nombre : "Sin asignar",
        EstadoTarea = resultado.Tarea.Estado ?? string.Empty,
        resultado.HorasEstimadas,
        resultado.HorasReales,
        resultado.DesvioHoras,
        resultado.HorasRestantes,
        resultado.HorasExcedidas,
        resultado.PorcentajeConsumo,
        resultado.EstadoDesvio,
        resultado.RegistrosHoras,
    };

    private static object Desde(ConvenienciaOperativaProyecto resultado) => new
    {
        IdProyecto = resultado.Proyecto.ID,
        IdCliente = resultado.Proyecto.IdCliente,
        NombreProyecto = resultado.Proyecto.Nombre,
        NombreCliente = resultado.Proyecto.NombreCliente ?? "Cliente sin nombre",
        resultado.HorasEstimadas,
        resultado.HorasReales,
        resultado.CostoPlanificado,
        resultado.CostoReal,
        resultado.DesvioCosto,
        resultado.PresupuestoNoConsumido,
        resultado.Sobrecosto,
        resultado.HorasRealesSinCosto,
        resultado.HorasPlanificadasSinCosto,
        resultado.CostoRealIncompleto,
        resultado.EstadoConveniencia,
        resultado.FactorPrincipal,
    };

    private static object Desde(SobrecalificacionTarea resultado) => new
    {
        IdTarea = resultado.Tarea.ID,
        IdProyecto = resultado.Proyecto.ID,
        IdEmpleado = resultado.Empleado.ID,
        Titulo = resultado.Tarea.Titulo,
        NombreProyecto = resultado.Proyecto.Nombre,
        NombreCliente = resultado.Proyecto.NombreCliente ?? "Cliente sin nombre",
        resultado.SeniorityRequerido,
        SeniorityAsignado = resultado.Empleado.Seniority,
        EmpleadoAsignado = resultado.Empleado.Nombre,
        resultado.HorasAfectadas,
        resultado.ImpactoOperativoEstimado,
        resultado.Nivel,
    };

    internal static object Desde(AnalisisOcupacionOperativa resultado) => new
    {
        resultado.PorcentajeOcupacionGeneral,
        resultado.EstadoCargaGeneral,
        resultado.HorasDisponiblesSemanales,
        resultado.HorasEstimadasActivas,
        resultado.CantidadSubutilizados,
        resultado.CantidadBalanceados,
        resultado.CantidadSobrecargados,
        Empleados = resultado.Empleados.Select(Desde).ToList(),
        Proyectos = resultado.Proyectos.Select(Desde).ToList(),
        Clientes = resultado.Clientes.Select(Desde).ToList(),
    };

    internal static object Desde(DetalleOcupacionEmpleado resultado) => new
    {
        Empleado = Desde(resultado.Empleado),
        Tareas = resultado.Tareas.Select(Desde).ToList(),
    };

    private static object Desde(OcupacionCliente resultado) => new
    {
        resultado.IdCliente,
        resultado.NombreCliente,
        resultado.HorasEstimadasActivas,
        resultado.PorcentajeSobreCargaAgencia,
        Proyectos = resultado.Proyectos.Select(Desde).ToList(),
    };

    internal static object Desde(AnalisisDesvioHoras resultado) => new
    {
        resultado.HorasEstimadas,
        resultado.HorasReales,
        resultado.DesvioHoras,
        resultado.HorasRestantes,
        resultado.HorasExcedidas,
        resultado.PorcentajeConsumo,
        Proyectos = resultado.Proyectos.Select(Desde).ToList(),
    };

    internal static object Desde(AnalisisConvenienciaOperativa resultado) => new
    {
        resultado.CostoPlanificado,
        resultado.CostoReal,
        resultado.DesvioCosto,
        resultado.PresupuestoNoConsumido,
        resultado.Sobrecosto,
        resultado.HorasRealesSinCosto,
        resultado.CantidadClientesEnRevision,
        Clientes = resultado.Clientes.Select(Desde).ToList(),
        Proyectos = resultado.Proyectos.Select(Desde).ToList(),
    };

    private static object Desde(ConvenienciaOperativaCliente resultado) => new
    {
        IdCliente = resultado.Cliente.ID,
        NombreCliente = resultado.Cliente.Nombre,
        resultado.HorasEstimadas,
        resultado.HorasReales,
        resultado.CostoPlanificado,
        resultado.CostoReal,
        resultado.DesvioCosto,
        resultado.PresupuestoNoConsumido,
        resultado.Sobrecosto,
        resultado.HorasRealesSinCosto,
        resultado.EnRevisionOperativa,
        resultado.EstadoConveniencia,
        Proyectos = resultado.Proyectos.Select(Desde).ToList(),
    };

    internal static object Desde(DetalleConvenienciaOperativa resultado) => new
    {
        Proyecto = Desde(resultado.Proyecto),
        ComposicionCosto = resultado.ComposicionCosto.Select(costo => new
        {
            IdTarea = costo.Tarea.ID,
            IdEmpleado = costo.Empleado.ID,
            Tarea = costo.Tarea.Titulo,
            Empleado = costo.Empleado.Nombre ?? "Empleado sin costo configurado",
            Seniority = costo.Empleado.Seniority ?? "Sin datos",
            CostoHora = costo.Empleado.CostoHora,
            costo.HorasPlanificadas,
            costo.HorasReales,
            costo.CostoPlanificado,
            costo.CostoReal,
            costo.CostoNoConfigurado
        }).ToList(),
        resultado.Seguimientos,
    };

    internal static object Desde(AnalisisSobrecalificacionPerfiles resultado) => new
    {
        resultado.HorasAfectadas,
        resultado.ImpactoOperativoEstimado,
        resultado.CantidadTareasAfectadas,
        Tareas = resultado.Tareas.Select(Desde).ToList(),
    };
}

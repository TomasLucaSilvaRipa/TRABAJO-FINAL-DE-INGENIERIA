using System.Text.Json.Serialization.Metadata;
using TeamBalance.BE.Entidades;

namespace Teambalance.API.Models;

internal static class FiltrosJson
{
    internal static void Configurar(JsonTypeInfo contrato)
    {
        if (contrato.Type == typeof(FiltroProyecto))
        {
            AgregarId<FiltroProyecto>(contrato, "IdCliente", filtro => filtro.Cliente?.ID,
                (filtro, id) => filtro.Cliente = id.HasValue ? new Cliente { ID = id.Value } : null);
            AgregarId<FiltroProyecto>(contrato, "IdPMResponsable", filtro => filtro.PMResponsable?.ID,
                (filtro, id) => filtro.PMResponsable = id.HasValue ? new PM { ID = id.Value } : null);
        }
        if (contrato.Type == typeof(FiltroTarea))
        {
            AgregarId<FiltroTarea>(contrato, "IdProyecto", filtro => filtro.Proyecto?.ID,
                (filtro, id) => filtro.Proyecto = id.HasValue ? new Proyecto { ID = id.Value } : null);
            AgregarId<FiltroTarea>(contrato, "IdEmpleadoAsignado", filtro => filtro.EmpleadoAsignado?.ID,
                (filtro, id) => filtro.EmpleadoAsignado = id.HasValue ? new Empleado { ID = id.Value } : null);
            AgregarId<FiltroTarea>(contrato, "IdSkillRequerido", filtro => filtro.SkillRequerido?.ID,
                (filtro, id) => filtro.SkillRequerido = id.HasValue ? new Skill { ID = id.Value } : null);
        }
        if (contrato.Type == typeof(FiltroRiesgoRetraso))
        {
            AgregarId<FiltroRiesgoRetraso>(contrato, "IdProyecto", filtro => filtro.Proyecto?.ID,
                (filtro, id) => filtro.Proyecto = id.HasValue ? new Proyecto { ID = id.Value } : null);
            AgregarId<FiltroRiesgoRetraso>(contrato, "IdCliente", filtro => filtro.Cliente?.ID,
                (filtro, id) => filtro.Cliente = id.HasValue ? new Cliente { ID = id.Value } : null);
            AgregarId<FiltroRiesgoRetraso>(contrato, "IdPM", filtro => filtro.PM?.ID,
                (filtro, id) => filtro.PM = id.HasValue ? new PM { ID = id.Value } : null);
        }
        if (contrato.Type == typeof(FiltroReporteEjecutivo))
        {
            AgregarId<FiltroReporteEjecutivo>(contrato, "IdCliente", filtro => filtro.Cliente?.ID,
                (filtro, id) => filtro.Cliente = id.HasValue ? new Cliente { ID = id.Value } : null);
            AgregarId<FiltroReporteEjecutivo>(contrato, "IdProyecto", filtro => filtro.Proyecto?.ID,
                (filtro, id) => filtro.Proyecto = id.HasValue ? new Proyecto { ID = id.Value } : null);
            AgregarId<FiltroReporteEjecutivo>(contrato, "IdResponsable", filtro => filtro.Responsable?.ID,
                (filtro, id) => filtro.Responsable = id.HasValue ? new PM { ID = id.Value } : null);
        }
        if (contrato.Type == typeof(FiltroCoberturaSkill))
        {
            AgregarId<FiltroCoberturaSkill>(contrato, "IdProyecto", filtro => filtro.Proyecto?.ID,
                (filtro, id) => filtro.Proyecto = id.HasValue ? new Proyecto { ID = id.Value } : null);
            AgregarId<FiltroCoberturaSkill>(contrato, "IdCliente", filtro => filtro.Cliente?.ID,
                (filtro, id) => filtro.Cliente = id.HasValue ? new Cliente { ID = id.Value } : null);
        }
    }

    private static void AgregarId<T>(JsonTypeInfo contrato, string nombre, Func<T, int?> leer, Action<T, int?> escribir)
    {
        string clave = contrato.Options.PropertyNamingPolicy?.ConvertName(nombre) ?? nombre;
        JsonPropertyInfo propiedad = contrato.CreateJsonPropertyInfo(typeof(int?), clave);
        propiedad.Get = instancia => leer((T)instancia);
        propiedad.Set = (instancia, valor) => escribir((T)instancia, (int?)valor);
        contrato.Properties.Add(propiedad);
    }
}

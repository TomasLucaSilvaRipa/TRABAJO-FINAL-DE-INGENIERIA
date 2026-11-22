using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Reflection;

namespace Teambalance.API.Models;

internal sealed class ReferenciaHttpApiDescriptionProvider(IModelMetadataProvider metadatos) : IApiDescriptionProvider
{
    public int Order => 0;

    public void OnProvidersExecuting(ApiDescriptionProviderContext contexto)
    {
    }

    public void OnProvidersExecuted(ApiDescriptionProviderContext contexto)
    {
        foreach (ApiDescription accion in contexto.Results)
        {
            foreach (ControllerParameterDescriptor parametro in accion.ActionDescriptor.Parameters.OfType<ControllerParameterDescriptor>())
            {
                var referencia = parametro.ParameterInfo.GetCustomAttribute<ReferenciaHttpAttribute>();
                if (referencia is null) continue;

                ApiParameterDescription? entrada = accion.ParameterDescriptions.FirstOrDefault(
                    descripcion => descripcion.Source == BindingSource.Custom && descripcion.Name == referencia.Name);
                if (entrada is null) continue;

                Type tipo = parametro.ParameterType.GetProperty(referencia.Propiedad)!.PropertyType;
                if (referencia.Opcional && tipo.IsValueType)
                    tipo = typeof(Nullable<>).MakeGenericType(tipo);

                entrada.Source = referencia.Consulta ? BindingSource.Query : BindingSource.Path;
                entrada.Type = tipo;
                entrada.ModelMetadata = metadatos.GetMetadataForType(tipo);
                entrada.IsRequired = !referencia.Consulta;
            }
        }
    }
}


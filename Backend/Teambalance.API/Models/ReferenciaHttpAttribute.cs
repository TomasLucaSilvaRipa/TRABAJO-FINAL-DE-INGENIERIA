using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Reflection;

namespace Teambalance.API.Models;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ReferenciaHttpAttribute : ModelBinderAttribute
{
    public ReferenciaHttpAttribute(string nombre, string propiedad = "ID", bool consulta = false, bool opcional = false)
        : base(typeof(ReferenciaHttpBinder))
    {
        Name = nombre;
        Propiedad = propiedad;
        Consulta = consulta;
        Opcional = opcional;
    }

    internal string Propiedad { get; }
    internal bool Consulta { get; }
    internal bool Opcional { get; }
}

public sealed class ReferenciaHttpBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext contexto)
    {
        var metadatos = (DefaultModelMetadata)contexto.ModelMetadata;
        var referencia = metadatos.Attributes.Attributes.OfType<ReferenciaHttpAttribute>().Single();
        string nombre = referencia.Name!;
        BindingSource origen = referencia.Consulta ? BindingSource.Query : BindingSource.Path;
        var proveedor = contexto.ValueProvider is IBindingSourceValueProvider porOrigen
            ? porOrigen.Filter(origen)
            : contexto.ValueProvider;
        ValueProviderResult valor = proveedor?.GetValue(nombre) ?? ValueProviderResult.None;

        if (valor == ValueProviderResult.None && referencia.Opcional)
        {
            contexto.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        PropertyInfo propiedad = contexto.ModelType.GetProperty(referencia.Propiedad)!;
        object? identidad;
        if (valor == ValueProviderResult.None)
        {
            identidad = propiedad.PropertyType == typeof(int) ? 0 : null;
        }
        else
        {
            contexto.ModelState.SetModelValue(nombre, valor);
            if (propiedad.PropertyType == typeof(int))
            {
                if (!int.TryParse(valor.FirstValue, System.Globalization.NumberStyles.Integer, valor.Culture, out int id))
                {
                    if (referencia.Opcional && string.IsNullOrWhiteSpace(valor.FirstValue))
                    {
                        contexto.ModelState.MarkFieldValid(nombre);
                        contexto.Result = ModelBindingResult.Success(null);
                    }
                    else
                        contexto.ModelState.TryAddModelError(nombre, contexto.ModelMetadata.ModelBindingMessageProvider.AttemptedValueIsInvalidAccessor(valor.FirstValue ?? string.Empty, nombre));
                    return Task.CompletedTask;
                }
                identidad = id;
            }
            else
            {
                identidad = valor.FirstValue;
            }
        }

        object entidad = Activator.CreateInstance(contexto.ModelType)!;
        propiedad.SetValue(entidad, identidad);
        if (valor != ValueProviderResult.None)
            contexto.ModelState.MarkFieldValid(nombre);
        contexto.ValidationState[entidad] = new ValidationStateEntry { SuppressValidation = true };
        contexto.Result = ModelBindingResult.Success(entidad);
        return Task.CompletedTask;
    }
}

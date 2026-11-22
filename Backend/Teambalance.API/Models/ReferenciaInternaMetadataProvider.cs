using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using TeamBalance.BE.Entidades;

namespace Teambalance.API.Models;

internal sealed class ReferenciaInternaMetadataProvider : IBindingMetadataProvider, IValidationMetadataProvider, IPropertyValidationFilter
{
    public void CreateBindingMetadata(BindingMetadataProviderContext context)
    {
        if (context.Attributes.OfType<ReferenciaInternaAttribute>().Any())
            context.BindingMetadata.IsBindingAllowed = false;
    }

    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        if (context.Attributes.OfType<ReferenciaInternaAttribute>().Any())
            context.ValidationMetadata.PropertyValidationFilter = this;
    }

    public bool ShouldValidateEntry(ValidationEntry entry, ValidationEntry parentEntry) => false;
}

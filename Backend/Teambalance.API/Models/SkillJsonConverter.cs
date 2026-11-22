using System.Text.Json;
using System.Text.Json.Serialization;
using TeamBalance.BE.Entidades;

namespace Teambalance.API.Models;

internal sealed class SkillJsonConverter : JsonConverter<Skill>
{
    public override Skill? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        SkillContrato? contrato = JsonSerializer.Deserialize<SkillContrato>(ref reader, options);
        if (contrato is null) return null;

        return new Skill(contrato.ID, contrato.Nombre, contrato.Categoria, contrato.Activo)
        {
            Area = contrato.IdAreaSkill is int id
                ? new AreaSkill { ID = id, Nombre = contrato.NombreArea! }
                : null
        };
    }

    public override void Write(Utf8JsonWriter writer, Skill value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, new SkillContrato(
            value.ID, value.Nombre, value.Categoria, value.Area?.ID, value.Area?.Nombre, value.Activo), options);
    }

    private sealed record SkillContrato(
        int ID, string Nombre, string? Categoria, int? IdAreaSkill, string? NombreArea, bool Activo);
}

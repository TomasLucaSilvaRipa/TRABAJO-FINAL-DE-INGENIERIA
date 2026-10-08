namespace TeamBalance.BE.Entidades;

/// <summary>Área funcional única que agrupa skills relacionadas.</summary>
public sealed class AreaSkill
{
    public int ID { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

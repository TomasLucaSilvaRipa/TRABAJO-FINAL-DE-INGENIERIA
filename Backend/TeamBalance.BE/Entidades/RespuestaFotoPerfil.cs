using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class RespuestaFotoPerfil
{
    public RespuestaFotoPerfil(string? fotoPerfil) => FotoPerfil = fotoPerfil;
    public string? FotoPerfil { get; set; }
}

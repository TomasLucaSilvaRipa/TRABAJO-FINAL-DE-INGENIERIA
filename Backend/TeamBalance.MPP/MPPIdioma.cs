using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPIdioma
{
    private readonly Conexion _conexion;
    public MPPIdioma(Conexion conexion) { _conexion = conexion; }

    public List<Idioma> ConsultarActivos() => Consultar("dbo.usp_Idioma_ConsultarActivos");
    public List<Idioma> ConsultarGestion() => Consultar("dbo.usp_Idioma_ConsultarGestion");

    public Idioma Registrar(Idioma idioma)
    {
        DataTable resultado = _conexion.Leer("dbo.usp_Idioma_Registrar", Parametros(idioma));
        if (resultado.Rows.Count != 1) throw new InvalidOperationException("No fue posible registrar el idioma.");
        idioma.ID = Convert.ToInt32(resultado.Rows[0]["ID"]);
        return idioma;
    }

    public void Modificar(Idioma idioma)
    {
        List<SqlParameter> parametros = Parametros(idioma);
        parametros.Insert(0, new SqlParameter("@IdIdioma", idioma.ID));
        if (!_conexion.Escribir("dbo.usp_Idioma_Modificar", parametros)) throw new InvalidOperationException("No fue posible guardar el idioma.");
    }

    private List<Idioma> Consultar(string procedimiento)
    {
        DataTable tabla = _conexion.Leer(procedimiento);
        return tabla.Rows.Cast<DataRow>().Select(Crear).ToList();
    }

    private static List<SqlParameter> Parametros(Idioma idioma) => new()
    {
        new SqlParameter("@Codigo", idioma.Codigo),
        new SqlParameter("@Nombre", idioma.Nombre),
        new SqlParameter("@Activo", idioma.Activo),
        new SqlParameter("@TraduccionesJson", JsonSerializer.Serialize(idioma.Traducciones)),
    };

    private static Idioma Crear(DataRow fila)
    {
        string json = Convert.ToString(fila["TraduccionesJson"]) ?? "{}";
        Dictionary<string, string>? traducciones;
        try { traducciones = JsonSerializer.Deserialize<Dictionary<string, string>>(json); }
        catch (JsonException) { traducciones = new Dictionary<string, string>(); }
        return new Idioma
        {
            ID = Convert.ToInt32(fila["ID"]),
            Codigo = Convert.ToString(fila["Codigo"]) ?? string.Empty,
            Nombre = Convert.ToString(fila["Nombre"]) ?? string.Empty,
            Activo = Convert.ToBoolean(fila["Activo"]),
            Traducciones = traducciones ?? new Dictionary<string, string>(),
        };
    }
}

using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public sealed class MPPPreguntaFrecuente
{
    private readonly Conexion _conexion;

    public MPPPreguntaFrecuente(Conexion conexion) {
        _conexion = conexion;
    }

    public List<PreguntaFrecuente> ConsultarPublicas() {
        return CrearPreguntas(_conexion.Leer("dbo.usp_PreguntaFrecuente_ConsultarPublicas"));
    }

    public List<PreguntaFrecuente> ConsultarGestion() {
        return CrearPreguntas(_conexion.Leer("dbo.usp_PreguntaFrecuente_ConsultarGestion"));
    }

    public PreguntaFrecuente Guardar(PreguntaFrecuente pregunta) {
        List<SqlParameter> parametros = new List<SqlParameter>() {
            new SqlParameter("@IdPreguntaFrecuente", pregunta.ID),
            new SqlParameter("@CategoriaCodigo", (int)pregunta.Categoria),
            new SqlParameter("@PreguntaEs", pregunta.PreguntaEs),
            new SqlParameter("@RespuestaEs", pregunta.RespuestaEs),
            new SqlParameter("@PreguntaEn", pregunta.PreguntaEn),
            new SqlParameter("@RespuestaEn", pregunta.RespuestaEn),
            new SqlParameter("@Orden", pregunta.Orden)
        };
        return CrearPreguntas(_conexion.Leer("dbo.usp_PreguntaFrecuente_Guardar", parametros)).Single();
    }

    public void DarDeBaja(PreguntaFrecuente pregunta) {
        _conexion.Escribir("dbo.usp_PreguntaFrecuente_DarDeBaja", new List<SqlParameter>() { new SqlParameter("@IdPreguntaFrecuente", pregunta.ID) });
    }

    private static List<PreguntaFrecuente> CrearPreguntas(DataTable tabla) {
        List<PreguntaFrecuente> preguntas = new List<PreguntaFrecuente>();
        foreach (DataRow fila in tabla.Rows) {
            PreguntaFrecuente pregunta = new PreguntaFrecuente();
            pregunta.ID = Convert.ToInt32(fila["ID"]);
            pregunta.Categoria = (CategoriaPreguntaFrecuente)Convert.ToInt32(fila["Categoria"]);
            pregunta.PreguntaEs = Convert.ToString(fila["PreguntaEs"]) ?? string.Empty;
            pregunta.RespuestaEs = Convert.ToString(fila["RespuestaEs"]) ?? string.Empty;
            pregunta.PreguntaEn = Convert.ToString(fila["PreguntaEn"]) ?? string.Empty;
            pregunta.RespuestaEn = Convert.ToString(fila["RespuestaEn"]) ?? string.Empty;
            pregunta.Orden = Convert.ToInt32(fila["Orden"]);
            pregunta.Activo = Convert.ToBoolean(fila["Activo"]);
            preguntas.Add(pregunta);
        }
        return preguntas;
    }
}

using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public sealed class MPPEncuesta
{
    private readonly Conexion _conexion;

    public MPPEncuesta(Conexion conexion)
    {
        _conexion = conexion;
    }

    public List<Encuesta> ConsultarPublicas()
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Encuesta_ConsultarPublicas");
        return CrearEncuestasResumen(tabla);
    }

    public Encuesta ConsultarPublica(Encuesta encuesta)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdEncuesta", encuesta.ID) };
        DataTable tabla = _conexion.Leer("dbo.usp_Encuesta_ConsultarDetallePublica", parametros);
        return CrearEncuestaDetalle(tabla);
    }

    public List<Encuesta> ConsultarGestion()
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Encuesta_ConsultarGestion");
        return CrearEncuestasResumen(tabla);
    }

    public Encuesta ConsultarGestion(Encuesta encuesta)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdEncuesta", encuesta.ID) };
        DataTable tabla = _conexion.Leer("dbo.usp_Encuesta_ConsultarDetalleGestion", parametros);
        return CrearEncuestaDetalle(tabla);
    }

    public Encuesta Guardar(Encuesta encuesta)
    {
        string preguntasJson = JsonSerializer.Serialize(encuesta.Preguntas);
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdEncuesta", encuesta.ID), new SqlParameter("@TituloEs", encuesta.TituloEs), new SqlParameter("@TituloEn", encuesta.TituloEn), new SqlParameter("@DescripcionEs", encuesta.DescripcionEs), new SqlParameter("@DescripcionEn", encuesta.DescripcionEn), new SqlParameter("@FechaInicio", encuesta.FechaInicio), new SqlParameter("@FechaVencimiento", encuesta.FechaVencimiento), new SqlParameter("@PreguntasJson", preguntasJson) };
        DataTable tabla = _conexion.Leer("dbo.usp_Encuesta_Guardar", parametros);
        Encuesta resultado = new Encuesta();
        resultado.ID = Convert.ToInt32(tabla.Rows[0]["ID"]);
        return ConsultarGestion(resultado);
    }

    public void DarDeBaja(Encuesta encuesta)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdEncuesta", encuesta.ID) };
        _conexion.Escribir("dbo.usp_Encuesta_DarDeBaja", parametros);
    }

    public void Responder(RespuestaEncuesta respuesta)
    {
        string respuestasJson = JsonSerializer.Serialize(respuesta.Respuestas);
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdEncuesta", respuesta.IdEncuesta), new SqlParameter("@IdentificadorParticipante", respuesta.IdentificadorParticipante), new SqlParameter("@RespuestasJson", respuestasJson) };
        _conexion.Escribir("dbo.usp_Encuesta_Responder", parametros);
    }

    public ResultadoEncuesta ConsultarResultados(Encuesta encuesta)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdEncuesta", encuesta.ID) };
        DataTable tabla = _conexion.Leer("dbo.usp_Encuesta_ConsultarResultados", parametros);
        ResultadoEncuesta resultado = new ResultadoEncuesta();
        if (tabla.Rows.Count == 0) { return resultado; }

        DataRow primeraFila = tabla.Rows[0];
        resultado.IdEncuesta = Convert.ToInt32(primeraFila["IdEncuesta"]);
        resultado.TituloEs = Convert.ToString(primeraFila["TituloEs"]) ?? string.Empty;
        resultado.TituloEn = Convert.ToString(primeraFila["TituloEn"]) ?? string.Empty;
        resultado.CantidadRespuestas = Convert.ToInt32(primeraFila["CantidadRespuestasEncuesta"]);

        foreach (IGrouping<int, DataRow> grupoPregunta in tabla.AsEnumerable().GroupBy(fila => Convert.ToInt32(fila["IdPreguntaEncuesta"])))
        {
            DataRow filaPregunta = grupoPregunta.First();
            ResultadoPreguntaEncuesta pregunta = new ResultadoPreguntaEncuesta();
            pregunta.IdPreguntaEncuesta = Convert.ToInt32(filaPregunta["IdPreguntaEncuesta"]);
            pregunta.EnunciadoEs = Convert.ToString(filaPregunta["EnunciadoEs"]) ?? string.Empty;
            pregunta.EnunciadoEn = Convert.ToString(filaPregunta["EnunciadoEn"]) ?? string.Empty;
            pregunta.Orden = Convert.ToInt32(filaPregunta["OrdenPregunta"]);

            foreach (DataRow filaOpcion in grupoPregunta.OrderBy(fila => Convert.ToInt32(fila["OrdenOpcion"])))
            {
                ResultadoOpcionEncuesta opcion = new ResultadoOpcionEncuesta();
                opcion.IdOpcionEncuesta = Convert.ToInt32(filaOpcion["IdOpcionEncuesta"]);
                opcion.TextoEs = Convert.ToString(filaOpcion["TextoEs"]) ?? string.Empty;
                opcion.TextoEn = Convert.ToString(filaOpcion["TextoEn"]) ?? string.Empty;
                opcion.Orden = Convert.ToInt32(filaOpcion["OrdenOpcion"]);
                opcion.CantidadRespuestas = Convert.ToInt32(filaOpcion["CantidadRespuestas"]);
                opcion.Porcentaje = Convert.ToDecimal(filaOpcion["Porcentaje"]);
                pregunta.Opciones.Add(opcion);
            }
            resultado.Preguntas.Add(pregunta);
        }
        return resultado;
    }

    private static List<Encuesta> CrearEncuestasResumen(DataTable tabla)
    {
        List<Encuesta> encuestas = new List<Encuesta>();
        foreach (DataRow fila in tabla.Rows)
        {
            Encuesta encuesta = CrearEncuesta(fila);
            encuesta.CantidadRespuestas = Convert.ToInt32(fila["CantidadRespuestas"]);
            encuestas.Add(encuesta);
        }
        return encuestas;
    }

    private static Encuesta CrearEncuestaDetalle(DataTable tabla)
    {
        if (tabla.Rows.Count == 0) { throw new ArgumentException("No encontramos la encuesta solicitada."); }
        Encuesta encuesta = CrearEncuesta(tabla.Rows[0]);
        foreach (IGrouping<int, DataRow> grupoPregunta in tabla.AsEnumerable().GroupBy(fila => Convert.ToInt32(fila["IdPreguntaEncuesta"])))
        {
            DataRow filaPregunta = grupoPregunta.First();
            PreguntaEncuesta pregunta = new PreguntaEncuesta();
            pregunta.ID = Convert.ToInt32(filaPregunta["IdPreguntaEncuesta"]);
            pregunta.IdEncuesta = encuesta.ID;
            pregunta.EnunciadoEs = Convert.ToString(filaPregunta["EnunciadoEs"]) ?? string.Empty;
            pregunta.EnunciadoEn = Convert.ToString(filaPregunta["EnunciadoEn"]) ?? string.Empty;
            pregunta.Orden = Convert.ToInt32(filaPregunta["OrdenPregunta"]);

            foreach (DataRow filaOpcion in grupoPregunta.OrderBy(fila => Convert.ToInt32(fila["OrdenOpcion"])))
            {
                OpcionEncuesta opcion = new OpcionEncuesta();
                opcion.ID = Convert.ToInt32(filaOpcion["IdOpcionEncuesta"]);
                opcion.IdPreguntaEncuesta = pregunta.ID;
                opcion.TextoEs = Convert.ToString(filaOpcion["TextoEs"]) ?? string.Empty;
                opcion.TextoEn = Convert.ToString(filaOpcion["TextoEn"]) ?? string.Empty;
                opcion.Orden = Convert.ToInt32(filaOpcion["OrdenOpcion"]);
                pregunta.Opciones.Add(opcion);
            }
            encuesta.Preguntas.Add(pregunta);
        }
        return encuesta;
    }

    private static Encuesta CrearEncuesta(DataRow fila)
    {
        Encuesta encuesta = new Encuesta();
        encuesta.ID = Convert.ToInt32(fila["ID"]);
        encuesta.TituloEs = Convert.ToString(fila["TituloEs"]) ?? string.Empty;
        encuesta.TituloEn = Convert.ToString(fila["TituloEn"]) ?? string.Empty;
        encuesta.DescripcionEs = Convert.ToString(fila["DescripcionEs"]) ?? string.Empty;
        encuesta.DescripcionEn = Convert.ToString(fila["DescripcionEn"]) ?? string.Empty;
        encuesta.FechaInicio = Convert.ToDateTime(fila["FechaInicio"]);
        encuesta.FechaVencimiento = Convert.ToDateTime(fila["FechaVencimiento"]);
        encuesta.Activo = Convert.ToBoolean(fila["Activo"]);
        return encuesta;
    }
}

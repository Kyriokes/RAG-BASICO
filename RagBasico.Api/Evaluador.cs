using System.Diagnostics;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;

namespace RagBasico.Api;

/// <summary>Corre el examen: cada pregunta contra el buscador, y las preguntas sin respuesta contra el redactor.</summary>
public sealed class Evaluador
{
    private static readonly string[] Modos = new string[] { "palabras" };

    private readonly Buscador _buscador;
    private readonly Redactor _redactor;
    private readonly string _conexion;
    private readonly string _archivoExamen;

    /// <summary>Recibe el buscador y el redactor, y lee de la configuración la conexión y la ruta del examen.</summary>
    public Evaluador(Buscador buscador, Redactor redactor, IConfiguration config, IHostEnvironment entorno)
    {
        _buscador = buscador;
        _redactor = redactor;
        _conexion = config.GetConnectionString("Rag") ?? throw new InvalidOperationException("Falta ConnectionStrings:Rag en la configuración.");
        // La ruta del examen es relativa a la carpeta del proyecto: "..\examen.csv" es la raíz del repo.
        _archivoExamen = Path.GetFullPath(Path.Combine(entorno.ContentRootPath, config["Rag:ArchivoExamen"] ?? "..\\examen.csv"));
    }

    /// <summary>Corre el examen completo y devuelve la tabla por modo, el detalle por pregunta, las fallas y los avisos.</summary>
    public async Task<InformeEvaluacion> CorrerAsync(CancellationToken ct)
    {
        List<string> avisos = new List<string>();
        List<string> fallas = new List<string>();
        List<DetallePregunta> detalle = new List<DetallePregunta>();
        List<ResultadoModo> resultados = new List<ResultadoModo>();

        if (!File.Exists(_archivoExamen))
        {
            avisos.Add("No existe el examen '" + _archivoExamen + "'. Revisá Rag:ArchivoExamen.");
            return new InformeEvaluacion(resultados, 0, 0, detalle, fallas, avisos);
        }
        List<PreguntaExamen> examen = LeerExamen(_archivoExamen);
        if (examen.Count == 0)
        {
            avisos.Add("El examen no tiene preguntas. Escribilas en " + _archivoExamen + " siguiendo examen.ejemplo.csv.");
            return new InformeEvaluacion(resultados, 0, 0, detalle, fallas, avisos);
        }

        HashSet<string> cargados = await LeerDocumentosCargadosAsync(ct);
        foreach (PreguntaExamen pregunta in examen)
        {
            foreach (string documento in pregunta.Documentos)
            {
                if (!cargados.Contains(documento))
                {
                    avisos.Add("El examen nombra " + documento + ", que no está cargado.");
                }
            }
        }
        int pendientes = await EsperarFullTextAsync(ct);
        if (pendientes > 0)
        {
            avisos.Add("El Full-Text todavía tiene " + pendientes + " cambios pendientes: la búsqueda por palabras puede salir baja.");
        }

        // 1. Las preguntas con respuesta, contra el buscador, en cada modo.
        foreach (string modo in Modos)
        {
            List<DetallePregunta> delModo = new List<DetallePregunta>();
            foreach (PreguntaExamen pregunta in examen.Where(p => p.Documentos.Count > 0))
            {
                Stopwatch reloj = Stopwatch.StartNew();
                List<Fragmento> encontrados = await _buscador.PorPalabrasAsync(pregunta.Pregunta, 5, ct);
                int puesto = 0;
                for (int i = 0; i < encontrados.Count && puesto == 0; i++)
                {
                    if (pregunta.Documentos.Contains(encontrados[i].Archivo, StringComparer.OrdinalIgnoreCase))
                    {
                        puesto = i + 1;
                    }
                }
                DetallePregunta d = new DetallePregunta(pregunta.Pregunta, modo, puesto, (int)reloj.ElapsedMilliseconds);
                delModo.Add(d);
                if (puesto == 0)
                {
                    fallas.Add("[" + modo + "] no encontró: " + pregunta.Pregunta);
                }
            }
            detalle.AddRange(delModo);
            resultados.Add(Puntuar(modo, delModo));
        }

        // 2. Las preguntas sin respuesta, contra el redactor: tiene que decir la frase fija.
        int sinRespuestaTotal = 0;
        int sinRespuestaCorrectas = 0;
        foreach (PreguntaExamen pregunta in examen.Where(p => p.Documentos.Count == 0))
        {
            sinRespuestaTotal++;
            RespuestaRag respuesta = await _redactor.ResponderAsync(pregunta.Pregunta, ct);
            if (Redactor.EsSinRespuesta(respuesta.Texto))
            {
                sinRespuestaCorrectas++;
            }
            else
            {
                fallas.Add("[sin respuesta] respondió algo en lugar de la frase fija: " + pregunta.Pregunta);
            }
        }

        return new InformeEvaluacion(resultados, sinRespuestaCorrectas, sinRespuestaTotal, detalle, fallas, avisos);
    }

    /// <summary>Resume un modo: cuántas preguntas tuvieron un documento correcto en el primer lugar, cuántas entre los 5 primeros, y el tiempo promedio.</summary>
    public static ResultadoModo Puntuar(string modo, List<DetallePregunta> detalle)
    {
        int primerLugar = detalle.Count(d => d.Puesto == 1);
        int entreLosCinco = detalle.Count(d => d.Puesto >= 1 && d.Puesto <= 5);
        int msPromedio = detalle.Count == 0 ? 0 : (int)detalle.Average(d => d.Ms);
        return new ResultadoModo(modo, detalle.Count, primerLugar, entreLosCinco, msPromedio);
    }

    /// <summary>Lee el examen: una pregunta por línea, "pregunta;documento". Acepta UTF-8 o la codificación de Excel, punto y coma adentro de una pregunta entre comillas, y varios documentos separados con "|". "ninguno" marca una pregunta sin respuesta.</summary>
    public static List<PreguntaExamen> LeerExamen(string ruta)
    {
        byte[] bytes = File.ReadAllBytes(ruta);
        string texto;
        try
        {
            texto = new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            // Excel guarda "CSV (delimitado por comas)" en la codificación de Windows, no en UTF-8.
            texto = Encoding.Latin1.GetString(bytes);
        }
        texto = texto.TrimStart('﻿');

        List<PreguntaExamen> preguntas = new List<PreguntaExamen>();
        foreach (string lineaCruda in texto.Replace("\r\n", "\n").Split('\n'))
        {
            string linea = lineaCruda.Trim();
            if (linea.Length == 0 || linea.StartsWith("pregunta;", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            List<string> columnas = PartirLinea(linea);
            while (columnas.Count > 0 && columnas[columnas.Count - 1].Trim().Length == 0)
            {
                columnas.RemoveAt(columnas.Count - 1);
            }
            if (columnas.Count < 2)
            {
                continue;
            }
            // La última columna son los documentos; si la pregunta tenía ";" sin comillas, el resto se vuelve a unir.
            string documentos = columnas[columnas.Count - 1].Trim();
            string pregunta = string.Join(";", columnas.Take(columnas.Count - 1)).Trim().Normalize(NormalizationForm.FormC);
            List<string> lista = new List<string>();
            if (!documentos.Equals("ninguno", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string documento in documentos.Split('|'))
                {
                    if (documento.Trim().Length > 0)
                    {
                        lista.Add(documento.Trim().Normalize(NormalizationForm.FormC));
                    }
                }
            }
            preguntas.Add(new PreguntaExamen(pregunta, lista));
        }
        return preguntas;
    }

    /// <summary>Parte una línea de CSV por ";", respetando los campos entre comillas y las comillas dobladas adentro.</summary>
    private static List<string> PartirLinea(string linea)
    {
        List<string> columnas = new List<string>();
        StringBuilder actual = new StringBuilder();
        bool entreComillas = false;
        for (int i = 0; i < linea.Length; i++)
        {
            char c = linea[i];
            if (c == '"')
            {
                if (entreComillas && i + 1 < linea.Length && linea[i + 1] == '"')
                {
                    actual.Append('"');
                    i++;
                }
                else
                {
                    entreComillas = !entreComillas;
                }
            }
            else if (c == ';' && !entreComillas)
            {
                columnas.Add(actual.ToString());
                actual.Clear();
            }
            else
            {
                actual.Append(c);
            }
        }
        columnas.Add(actual.ToString());
        return columnas;
    }

    /// <summary>Devuelve los nombres de archivo cargados en la base, sin distinguir mayúsculas.</summary>
    private async Task<HashSet<string>> LeerDocumentosCargadosAsync(CancellationToken ct)
    {
        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            IEnumerable<string> archivos = await conexion.QueryAsync<string>(
                new CommandDefinition("SELECT Archivo FROM rag.Documento;", cancellationToken: ct));
            HashSet<string> cargados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string archivo in archivos)
            {
                cargados.Add(archivo.Normalize(NormalizationForm.FormC));
            }
            return cargados;
        }
    }

    /// <summary>Espera hasta un minuto a que el Full-Text termine de indexar. Devuelve cuántos cambios quedaron pendientes.</summary>
    private async Task<int> EsperarFullTextAsync(CancellationToken ct)
    {
        const string sql = "SELECT CAST(OBJECTPROPERTYEX(OBJECT_ID(N'rag.Fragmento'), 'TableFulltextPendingChanges') AS INT);";
        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            for (int intento = 0; intento < 30; intento++)
            {
                int pendientes = await conexion.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: ct));
                if (pendientes == 0)
                {
                    return 0;
                }
                await Task.Delay(2000, ct);
            }
            return await conexion.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: ct));
        }
    }
}

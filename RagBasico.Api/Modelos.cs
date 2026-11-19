namespace RagBasico.Api;

/// <summary>Lo que se manda a POST /preguntar.</summary>
public sealed record PedidoPregunta(string Pregunta);

/// <summary>Lo que devuelve el modelo de chat: el texto y los tiempos que informa Ollama.</summary>
public sealed record RespuestaModelo(
    string Texto,
    int MsCarga,
    int MsLectura,
    int MsEscritura,
    int TokensPregunta,
    int TokensRespuesta);

/// <summary>Un fragmento encontrado por el buscador: su número, el archivo de donde salió y su texto.</summary>
public sealed record Fragmento(int FragmentoId, string Archivo, string Texto);

/// <summary>Lo que devuelve POST /indexar: cuántos documentos y fragmentos se cargaron, cuánto tardó y el detalle por archivo.</summary>
public sealed record InformeIndexado(int Documentos, int Fragmentos, string Tiempo, List<string> Detalle);

/// <summary>Una fuente de la respuesta: el número con que la cita el modelo, el archivo y el fragmento.</summary>
public sealed record Fuente(int Numero, string Archivo, int FragmentoId);

/// <summary>Lo que devuelve POST /preguntar: la respuesta, sus fuentes y lo que tardó cada parte.</summary>
public sealed record RespuestaRag(
    string Texto,
    List<Fuente> Fuentes,
    int MsBusqueda,
    int MsModelo,
    int MsCarga,
    int MsLectura,
    int MsEscritura);

/// <summary>Una pregunta del examen y los documentos que la responden. Sin documentos, es una pregunta sin respuesta.</summary>
public sealed record PreguntaExamen(string Pregunta, List<string> Documentos);

/// <summary>Cómo le fue a una pregunta del examen en un modo de búsqueda: en qué puesto apareció un documento correcto (0 si no apareció) y cuánto tardó.</summary>
public sealed record DetallePregunta(string Pregunta, string Modo, int Puesto, int Ms);

/// <summary>El resultado de un modo de búsqueda en todo el examen.</summary>
public sealed record ResultadoModo(string Modo, int Preguntas, int PrimerLugar, int EntreLosCinco, int MsPromedio);

/// <summary>Lo que devuelve POST /evaluar.</summary>
public sealed record InformeEvaluacion(
    List<ResultadoModo> Resultados,
    int SinRespuestaCorrectas,
    int SinRespuestaTotal,
    List<DetallePregunta> Detalle,
    List<string> Fallas,
    List<string> Avisos);

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

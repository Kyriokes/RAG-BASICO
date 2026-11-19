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

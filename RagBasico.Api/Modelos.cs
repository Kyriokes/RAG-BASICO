namespace RagBasico.Api;

/// <summary>Lo que se manda a POST /preguntar: la pregunta y, si se quiere, el modo de búsqueda.</summary>
public sealed record PedidoPregunta(string Pregunta, string? Modo);

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

/// <summary>Un punto para subir a Qdrant: el número de fragmento como id, su vector y el número de documento.</summary>
public sealed record PuntoQdrant(int Id, float[] Vector, int DocumentoId);

/// <summary>El estado de la colección según Qdrant: el color, cuántos puntos tiene y cuántos vectores están en el índice HNSW.</summary>
public sealed record InfoColeccion(string Estado, int Puntos, int VectoresIndexados);

/// <summary>Un chequeo de GET /salud: qué se revisó, cómo dio ("ok", "aviso" o "error") y el detalle.</summary>
public sealed record Chequeo(string Nombre, string Estado, string Detalle);

/// <summary>Lo que devuelve GET /salud: el estado general y cada chequeo.</summary>
public sealed record InformeSalud(string Estado, List<Chequeo> Chequeos);

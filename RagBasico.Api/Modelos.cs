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

/// <summary>Un fragmento recién cortado por el Chunker: su texto, la página del PDF donde está su primera palabra y la página donde está la última. Si no cruza de página, las dos son la misma.</summary>
public sealed record Parte(string Texto, int Pagina, int PaginaFin);

/// <summary>Un fragmento encontrado por el buscador: su número, el número y el archivo del documento de donde salió, la página del PDF donde empieza, la página donde termina si cruza a otra (si no cruza, null) y su texto. Pagina es null en un fragmento cargado antes de que se guardara la página.</summary>
public sealed record Fragmento(int FragmentoId, int DocumentoId, string Archivo, int? Pagina, int? PaginaFin, string Texto);

/// <summary>Una fuente de la respuesta: el número con que la cita el modelo, el número y el archivo del documento, la página donde empieza el fragmento, la página donde termina si cruza a otra, y el número del fragmento.</summary>
public sealed record Fuente(int Numero, int DocumentoId, string Archivo, int? Pagina, int? PaginaFin, int FragmentoId);

/// <summary>El PDF de un documento, para abrirlo: el nombre del archivo y sus bytes. Contenido es null si el documento no tiene el PDF guardado o ya no está en la base de origen.</summary>
public sealed record PdfGuardado(string Archivo, byte[]? Contenido);

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

/// <summary>Un archivo que llegó a la página: su nombre y sus bytes.</summary>
public sealed record ArchivoSubido(string Nombre, byte[] Contenido);

/// <summary>Lo que devuelve anotar documentos, arrastrados o importados: cuántos son nuevos, cuántos ya estaban igual, cuántos se reemplazaron y cuántos se rechazaron, con el detalle.</summary>
public sealed record InformeAnotado(int Nuevos, int SinCambios, int Reemplazados, int Rechazados, List<string> Detalle);

/// <summary>Un documento que espera ser procesado. OrigenId es su número en la base de origen, o null si se arrastró a la página. Huella es la del PDF que se va a procesar, e Intentos, cuántas veces se empezó a procesar sin terminar.</summary>
public sealed record Pendiente(int DocumentoId, string Archivo, string? OrigenId, byte[]? Huella, int Intentos);

/// <summary>Un documento en la lista de la página: su estado, el motivo si falló y cuántos fragmentos tiene.</summary>
public sealed record DocumentoFila(int DocumentoId, string Archivo, string? OrigenId, string Estado, string? Detalle, int Fragmentos, DateTime FechaCarga);

/// <summary>Por dónde va el documento que se está procesando: su archivo, el paso en que está, cuántos fragmentos ya tienen su vector y cuántos fragmentos son. Total es 0 mientras todavía no se cortó el texto.</summary>
public sealed record AvanceDeCarga(string Archivo, string Paso, int Hechos, int Total);

/// <summary>Lo que devuelve GET /documentos: cuántos documentos hay en cada estado, cuántos fragmentos, la velocidad de carga, lo que falta, una página de la lista, el aviso que dice por qué está frenada la carga, si lo está, y por dónde va el documento que se está procesando, o null si no hay ninguno.</summary>
public sealed record ResumenDocumentos(int Listos, int Pendientes, int SinTexto, int ConError, int Fragmentos, double DocumentosPorMinuto, string TiempoQueFalta, List<DocumentoFila> Pagina, string Aviso, AvanceDeCarga? EnCurso);

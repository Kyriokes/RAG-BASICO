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

/// <summary>Lo que devuelve POST /indexar: cuántos PDF se cargaron, cuántos quedaron igual y cuántos se borraron, cuántos fragmentos se guardaron, cuánto tardó y el detalle.</summary>
public sealed record InformeIndexado(int Cargados, int SinCambios, int Borrados, int Fragmentos, string Tiempo, List<string> Detalle);

/// <summary>Un PDF de la carpeta con su huella de carga. La huella queda en null si el archivo no se pudo leer.</summary>
public sealed record ArchivoEnCarpeta(string Archivo, string Ruta, byte[]? Huella);

/// <summary>Un documento ya cargado en la base, con la huella con que se cargó. La huella es null si se cargó antes de que existiera la columna.</summary>
public sealed record DocumentoCargado(int DocumentoId, string Archivo, byte[]? Huella);

/// <summary>Qué hacer con cada PDF: cargar los nuevos y los cambiados, dejar los que siguen igual y borrar de la base los que ya no están en la carpeta.</summary>
public sealed record PlanDeCarga(
    List<ArchivoEnCarpeta> Nuevos,
    List<ArchivoEnCarpeta> Cambiados,
    List<ArchivoEnCarpeta> SinCambios,
    List<DocumentoCargado> Borrar);

/// <summary>Lo que devuelve POST /vectores: cuántos vectores se calcularon, cuántos fallaron, cuántos fragmentos quedan sin vector, la velocidad, el tiempo y el detalle.</summary>
public sealed record InformeVectores(int Calculados, int Fallados, int Pendientes, double FragmentosPorSegundo, string Tiempo, List<string> Detalle);

/// <summary>Una fuente de la respuesta: el número con que la cita el modelo, el archivo y el fragmento.</summary>
public sealed record Fuente(int Numero, string Archivo, int FragmentoId);

/// <summary>Lo que devuelve POST /preguntar: la respuesta, sus fuentes, lo que tardó cada parte y, si la búsqueda tuvo que seguir sin Qdrant, el aviso.</summary>
public sealed record RespuestaRag(
    string Texto,
    List<Fuente> Fuentes,
    int MsBusqueda,
    int MsModelo,
    int MsCarga,
    int MsLectura,
    int MsEscritura,
    string Aviso);

/// <summary>Lo que devuelve una búsqueda: los fragmentos y un aviso, vacío salvo que la híbrida haya tenido que seguir sin Qdrant.</summary>
public sealed record ResultadoBusqueda(List<Fragmento> Fragmentos, string Aviso);

/// <summary>Un punto para subir a Qdrant: el número de fragmento como id, su vector y el número de documento.</summary>
public sealed record PuntoQdrant(int Id, float[] Vector, int DocumentoId);

/// <summary>El estado de la colección según Qdrant: el color, cuántos puntos tiene y cuántos vectores están en el índice HNSW.</summary>
public sealed record InfoColeccion(string Estado, int Puntos, int VectoresIndexados);

/// <summary>La comparación entre SQL y Qdrant: los números de fragmento que faltan en Qdrant y los que sobran.</summary>
public sealed record ComparacionIds(List<int> Faltan, List<int> Sobran);

/// <summary>Lo que devuelve GET /qdrant/estado.</summary>
public sealed record EstadoQdrant(
    bool Iguales,
    string Coleccion,
    int ConVectorEnSql,
    int PuntosEnQdrant,
    int VectoresIndexados,
    string EstadoColeccion,
    int CantidadFaltan,
    int CantidadSobran,
    List<int> Faltan,
    List<int> Sobran,
    bool? MuestraCoincide,
    string Mensaje);

/// <summary>Lo que devuelve POST /qdrant/reconstruir: cuántos puntos se subieron y cuántos se borraron, el tiempo y el detalle.</summary>
public sealed record InformeQdrant(int Subidos, int Borrados, string Tiempo, List<string> Detalle);

/// <summary>Un chequeo de GET /salud: qué se revisó, cómo dio ("ok", "aviso" o "error") y el detalle.</summary>
public sealed record Chequeo(string Nombre, string Estado, string Detalle);

/// <summary>Lo que devuelve GET /salud: el estado general y cada chequeo.</summary>
public sealed record InformeSalud(string Estado, List<Chequeo> Chequeos);

/// <summary>Una pregunta del examen y los documentos que la responden. Sin documentos, es una pregunta sin respuesta.</summary>
public sealed record PreguntaExamen(string Pregunta, List<string> Documentos);

/// <summary>Cómo le fue a una pregunta del examen en un modo de búsqueda: en qué puesto apareció un documento correcto (0 si no apareció) y cuánto tardó.</summary>
public sealed record DetallePregunta(string Pregunta, string Modo, int Puesto, int Ms);

/// <summary>El resultado de un modo de búsqueda en todo el examen, también con las preguntas sin respuesta.</summary>
public sealed record ResultadoModo(
    string Modo,
    int Preguntas,
    int PrimerLugar,
    int EntreLosCinco,
    int MsPromedio,
    int SinRespuestaCorrectas,
    int SinRespuestaTotal);

/// <summary>Lo que devuelve POST /evaluar.</summary>
public sealed record InformeEvaluacion(
    List<ResultadoModo> Resultados,
    List<DetallePregunta> Detalle,
    List<string> Fallas,
    List<string> Avisos);

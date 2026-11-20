using Dapper;
using Microsoft.Data.SqlClient;

namespace RagBasico.Api;

/// <summary>Revisa que estén andando todas las piezas: SQL Server, el Full-Text, los modelos de Ollama, la dimensión de los vectores, Qdrant y, si está configurada, la base de origen. Cada chequeo dice qué dirección probó y qué hacer.</summary>
public sealed class Salud
{
    private readonly Ollama _ollama;
    private readonly Qdrant _qdrant;
    private readonly string _conexion;
    private readonly string _conexionOrigen;
    private readonly string _consultaLista;
    private readonly string _direccionOllama;
    private readonly string _direccionQdrant;
    private readonly string _modeloChat;
    private readonly string _modeloEmbeddings;

    /// <summary>Recibe los clientes de Ollama y de Qdrant, y lee de la configuración las conexiones y los nombres de los dos modelos.</summary>
    public Salud(Ollama ollama, Qdrant qdrant, IConfiguration config)
    {
        _ollama = ollama;
        _qdrant = qdrant;
        _conexion = config["Conexiones:SqlTextos"] ?? throw new InvalidOperationException("Falta Conexiones:SqlTextos en la configuración.");
        _conexionOrigen = config["Conexiones:SqlOrigen"] ?? "";
        _consultaLista = config["Conexiones:ConsultaLista"] ?? "";
        _direccionOllama = config["Conexiones:Ollama"] ?? "";
        _direccionQdrant = config["Conexiones:Qdrant"] ?? "";
        _modeloChat = config["Conexiones:ModeloChat"] ?? "qwen3.5:4b";
        _modeloEmbeddings = config["Conexiones:ModeloEmbeddings"] ?? "qwen3-embedding:0.6b";
    }

    /// <summary>Corre todos los chequeos. El estado general es "error" si falla algo sin lo que no se puede preguntar, "aviso" si algo anda a medias, y "ok" si anda todo.</summary>
    public async Task<InformeSalud> RevisarAsync(CancellationToken ct)
    {
        List<Chequeo> chequeos = new List<Chequeo>();
        chequeos.Add(await RevisarSqlAsync(ct));
        chequeos.Add(await RevisarFullTextAsync(ct));
        chequeos.Add(await RevisarModelosAsync(ct));
        chequeos.Add(await RevisarDimensionAsync(ct));
        chequeos.Add(await RevisarQdrantAsync(ct));
        if (_conexionOrigen != "")
        {
            chequeos.Add(await RevisarOrigenAsync(ct));
        }

        string estado = "ok";
        if (chequeos.Any(c => c.Estado == "aviso"))
        {
            estado = "aviso";
        }
        if (chequeos.Any(c => c.Estado == "error"))
        {
            estado = "error";
        }
        return new InformeSalud(estado, chequeos);
    }

    /// <summary>Devuelve el servidor y la base de una cadena de conexión, sin el usuario ni la clave, para mostrarlos en un mensaje.</summary>
    public static string Describir(string conexion)
    {
        try
        {
            SqlConnectionStringBuilder partes = new SqlConnectionStringBuilder(conexion);
            return "servidor " + partes.DataSource + ", base " + partes.InitialCatalog;
        }
        catch (Exception)
        {
            return "una cadena de conexión que no se puede leer";
        }
    }

    /// <summary>SQL Server: se conecta a la base y cuenta los documentos por estado y los fragmentos.</summary>
    private async Task<Chequeo> RevisarSqlAsync(CancellationToken ct)
    {
        const string nombre = "SQL Server";
        string donde = Describir(_conexion);
        try
        {
            using (SqlConnection conexion = new SqlConnection(_conexion))
            {
                Conteo conteo = await conexion.QuerySingleAsync<Conteo>(new CommandDefinition(
                    "SELECT " +
                    "(SELECT COUNT(*) FROM rag.Documento WHERE Estado = 'listo') AS Listos, " +
                    "(SELECT COUNT(*) FROM rag.Documento WHERE Estado = 'pendiente') AS Pendientes, " +
                    "(SELECT COUNT(*) FROM rag.Documento WHERE Estado IN ('error', 'sin texto')) AS SinCargar, " +
                    "(SELECT COUNT(*) FROM rag.Fragmento) AS Fragmentos;",
                    cancellationToken: ct));
                string detalle = "En " + donde + ": " + conteo.Listos + " documentos listos, " + conteo.Pendientes + " pendientes, " + conteo.SinCargar + " sin cargar, " + conteo.Fragmentos + " fragmentos.";
                if (conteo.Listos == 0)
                {
                    return new Chequeo(nombre, "aviso", detalle + " Todavía no hay documentos listos: cargalos en la página Documentos.");
                }
                return new Chequeo(nombre, "ok", detalle);
            }
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested)
            {
                throw;
            }
            SqlException? deSql = ex as SqlException;
            // 208: no existe la tabla. 4060: no existe la base o el usuario no puede abrirla.
            if (deSql != null && deSql.Number == 208)
            {
                return new Chequeo(nombre, "error", "En " + donde + " faltan las tablas del RAG. Corré sql\\base.sql en ese servidor (en casa, scripts\\crear-base.ps1).");
            }
            if (deSql != null && deSql.Number == 4060)
            {
                return new Chequeo(nombre, "error", "No se pudo abrir la base (" + donde + "): no existe, o el usuario no tiene permiso. Corré sql\\base.sql en ese servidor (en casa, scripts\\crear-base.ps1). " + ex.Message);
            }
            return new Chequeo(nombre, "error", "No se pudo conectar (" + donde + "): " + ex.Message + " Revisá Conexiones:SqlTextos, que el servicio de SQL Server esté andando y que se llegue a ese servidor por la red.");
        }
    }

    /// <summary>El Full-Text: que esté instalado y que no tenga cambios pendientes, porque mientras tanto la búsqueda por palabras no ve todo.</summary>
    private async Task<Chequeo> RevisarFullTextAsync(CancellationToken ct)
    {
        const string nombre = "Full-Text";
        try
        {
            using (SqlConnection conexion = new SqlConnection(_conexion))
            {
                int instalado = await conexion.ExecuteScalarAsync<int>(new CommandDefinition(
                    "SELECT CAST(ISNULL(FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'), 0) AS INT);", cancellationToken: ct));
                if (instalado == 0)
                {
                    return new Chequeo(nombre, "error", "Este SQL Server no tiene instalado el Full-Text: la búsqueda por palabras no anda. Se agrega con el instalador de SQL Server, en \"Selección de características\".");
                }
                int pendientes = await conexion.ExecuteScalarAsync<int>(new CommandDefinition(
                    "SELECT CAST(ISNULL(OBJECTPROPERTYEX(OBJECT_ID(N'rag.Fragmento'), 'TableFulltextPendingChanges'), 0) AS INT);",
                    cancellationToken: ct));
                if (pendientes > 0)
                {
                    return new Chequeo(nombre, "aviso", pendientes + " cambios pendientes: la búsqueda por palabras todavía no ve todo. Se pone al día solo.");
                }
                return new Chequeo(nombre, "ok", "Instalado y sin cambios pendientes.");
            }
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested)
            {
                throw;
            }
            return new Chequeo(nombre, "error", "No se pudo revisar el Full-Text: " + ex.Message);
        }
    }

    /// <summary>Los dos modelos: que Ollama responda y que los tenga instalados.</summary>
    private async Task<Chequeo> RevisarModelosAsync(CancellationToken ct)
    {
        const string nombre = "Modelos de Ollama";
        try
        {
            List<string> instalados = await _ollama.ListarModelosAsync(ct);
            List<string> faltan = new List<string>();
            foreach (string modelo in new string[] { _modeloChat, _modeloEmbeddings })
            {
                if (!EstaInstalado(instalados, modelo))
                {
                    faltan.Add(modelo);
                }
            }
            if (faltan.Count > 0)
            {
                List<string> comandos = faltan.Select(modelo => "ollama pull " + modelo).ToList();
                return new Chequeo(nombre, "error", "En " + _direccionOllama + " faltan " + string.Join(" y ", faltan) + ". En la máquina de Ollama, se bajan con: " + string.Join(" y ", comandos) + ".");
            }
            return new Chequeo(nombre, "ok", "En " + _direccionOllama + ": " + _modeloChat + " y " + _modeloEmbeddings + " instalados.");
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested)
            {
                throw;
            }
            return new Chequeo(nombre, "error", ex.Message + " Revisá Conexiones:Ollama y que Ollama esté abierto. Si está en otra máquina, tiene que escuchar en la red (OLLAMA_HOST=0.0.0.0).");
        }
    }

    /// <summary>La dimensión: calcula el vector de una palabra y controla que tenga 1024 números, como la colección de Qdrant.</summary>
    private async Task<Chequeo> RevisarDimensionAsync(CancellationToken ct)
    {
        const string nombre = "Dimensión de los vectores";
        try
        {
            // EmbeddingsAsync ya lanza un error claro si el modelo devuelve otra dimensión.
            List<float[]> vectores = await _ollama.EmbeddingsAsync(new List<string> { "prueba" }, ct);
            return new Chequeo(nombre, "ok", vectores[0].Length + " números, igual que la colección de Qdrant.");
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested)
            {
                throw;
            }
            return new Chequeo(nombre, "error", ex.Message);
        }
    }

    /// <summary>Qdrant: que responda y que tenga un punto por cada fragmento de los documentos listos. Si no responde es un error: sin Qdrant no hay búsqueda por significado.</summary>
    private async Task<Chequeo> RevisarQdrantAsync(CancellationToken ct)
    {
        const string nombre = "Qdrant";
        try
        {
            string version = await _qdrant.LeerVersionAsync(ct);
            string donde = "En " + _direccionQdrant + ", versión " + version + ". ";
            if (!await _qdrant.ExisteColeccionAsync(ct))
            {
                return new Chequeo(nombre, "aviso", donde + "Todavía no existe la colección " + _qdrant.Coleccion + ": se crea sola al cargar el primer documento.");
            }
            InfoColeccion info = await _qdrant.LeerInfoAsync(ct);
            Fragmentos fragmentos = await ContarFragmentosAsync(ct);
            // Mientras hay documentos pendientes, los números se mueven: recién se comparan cuando no queda ninguno.
            if (fragmentos.Pendientes == 0 && info.Puntos != fragmentos.DeListos)
            {
                return new Chequeo(nombre, "aviso", donde + "La colección " + _qdrant.Coleccion + " tiene " + info.Puntos + " puntos y la base " + fragmentos.DeListos + " fragmentos: no coinciden. En la página Documentos, apretá \"Reprocesar todo\".");
            }
            return new Chequeo(nombre, "ok", donde + "Colección " + _qdrant.Coleccion + ": " + info.Puntos + " puntos.");
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested)
            {
                throw;
            }
            if (ex is SqlException)
            {
                return new Chequeo(nombre, "aviso", "Qdrant responde en " + _direccionQdrant + ", pero no se pudo comparar con la base: SQL Server no responde.");
            }
            return new Chequeo(nombre, "error", ex.Message + " Revisá Conexiones:Qdrant y que Qdrant esté andando. En casa lo arranca iniciar.cmd; en otra máquina tiene que escuchar en la red.");
        }
    }

    /// <summary>La base de origen: que se pueda conectar y que la consulta de la lista ande.</summary>
    private async Task<Chequeo> RevisarOrigenAsync(CancellationToken ct)
    {
        const string nombre = "SQL de origen";
        string donde = Describir(_conexionOrigen);
        try
        {
            using (SqlConnection conexion = new SqlConnection(_conexionOrigen))
            {
                // Se cuenta con la consulta de la lista adentro de otra: así se prueba la consulta sin traer las filas.
                int cuantos = await conexion.ExecuteScalarAsync<int>(new CommandDefinition(
                    "SELECT COUNT(*) FROM (" + _consultaLista + ") AS lista;", cancellationToken: ct));
                return new Chequeo(nombre, "ok", "En " + donde + ": la consulta de la lista devuelve " + cuantos + " documentos.");
            }
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested)
            {
                throw;
            }
            return new Chequeo(nombre, "aviso", "No se pudo leer la base de origen (" + donde + "): " + ex.Message + " Revisá Conexiones:SqlOrigen y Conexiones:ConsultaLista. Preguntar sigue andando; importar, no.");
        }
    }

    /// <summary>Cuenta los fragmentos de los documentos listos y los documentos pendientes.</summary>
    private async Task<Fragmentos> ContarFragmentosAsync(CancellationToken ct)
    {
        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            return await conexion.QuerySingleAsync<Fragmentos>(new CommandDefinition(
                "SELECT " +
                "(SELECT COUNT(*) FROM rag.Fragmento AS f JOIN rag.Documento AS d ON d.DocumentoId = f.DocumentoId WHERE d.Estado = 'listo') AS DeListos, " +
                "(SELECT COUNT(*) FROM rag.Documento WHERE Estado = 'pendiente') AS Pendientes;",
                cancellationToken: ct));
        }
    }

    /// <summary>Dice si un modelo está en la lista de instalados. Un nombre sin etiqueta, como "llama3", es el mismo que "llama3:latest".</summary>
    private static bool EstaInstalado(List<string> instalados, string modelo)
    {
        string buscado = modelo.Contains(':') ? modelo : modelo + ":latest";
        return instalados.Any(m => string.Equals(m, buscado, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Los números de la base que muestra el chequeo de SQL Server.</summary>
    private sealed record Conteo(int Listos, int Pendientes, int SinCargar, int Fragmentos);

    /// <summary>Los números que se comparan con los puntos de Qdrant.</summary>
    private sealed record Fragmentos(int DeListos, int Pendientes);
}

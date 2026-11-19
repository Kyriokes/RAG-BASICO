using System.Diagnostics;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;

namespace RagBasico.Api;

/// <summary>Mantiene la colección de Qdrant igual a los vectores de SQL Server: compara los números de fragmento de los dos lados, sube lo que falta, borra lo que sobra y reconstruye la colección.</summary>
public sealed class Sincronizador
{
    // Cuántos vectores se leen de SQL y se suben a Qdrant en cada pedido.
    private const int Lote = 256;

    private readonly Qdrant _qdrant;
    private readonly string _conexion;

    /// <summary>Recibe el cliente de Qdrant y lee la cadena de conexión de la configuración.</summary>
    public Sincronizador(Qdrant qdrant, IConfiguration config)
    {
        _qdrant = qdrant;
        _conexion = config.GetConnectionString("Rag") ?? throw new InvalidOperationException("Falta ConnectionStrings:Rag en la configuración.");
    }

    /// <summary>Compara los números de fragmento con vector en SQL con los ids de los puntos de Qdrant. Faltan los que están en SQL y no en Qdrant; sobran los que están en Qdrant y no en SQL.</summary>
    public static ComparacionIds Comparar(List<int> enSql, List<int> enQdrant)
    {
        HashSet<int> sql = new HashSet<int>(enSql);
        HashSet<int> qdrant = new HashSet<int>(enQdrant);
        List<int> faltan = new List<int>();
        foreach (int id in sql)
        {
            if (!qdrant.Contains(id))
            {
                faltan.Add(id);
            }
        }
        List<int> sobran = new List<int>();
        foreach (int id in qdrant)
        {
            if (!sql.Contains(id))
            {
                sobran.Add(id);
            }
        }
        faltan.Sort();
        sobran.Sort();
        return new ComparacionIds(faltan, sobran);
    }

    /// <summary>Compara SQL con Qdrant sin cambiar nada: los números de fragmento de los dos lados, una muestra de vectores y cuántos vectores tiene el índice HNSW.</summary>
    public async Task<EstadoQdrant> EstadoAsync(CancellationToken ct)
    {
        List<int> enSql = await LeerIdsSqlAsync(ct);
        if (!await _qdrant.ExisteColeccionAsync(ct))
        {
            return new EstadoQdrant(false, _qdrant.Coleccion, enSql.Count, 0, 0, "no existe", enSql.Count, 0,
                enSql.Take(20).ToList(), new List<int>(), null,
                "La colección no existe: creala con POST /qdrant/reconstruir.");
        }

        List<int> enQdrant = await _qdrant.ListarIdsAsync(ct);
        ComparacionIds comparacion = Comparar(enSql, enQdrant);
        bool? muestra = await MuestraCoincideAsync(enSql, comparacion, ct);
        InfoColeccion info = await _qdrant.LeerInfoAsync(ct);

        bool iguales = comparacion.Faltan.Count == 0 && comparacion.Sobran.Count == 0 && muestra != false;
        string mensaje;
        if (muestra == false)
        {
            mensaje = "Los vectores de Qdrant no son los de esta base: seguramente se recreó la base. Reconstruí la colección con POST /qdrant/reconstruir.";
        }
        else if (!iguales)
        {
            mensaje = "Faltan " + comparacion.Faltan.Count + " fragmentos en Qdrant y sobran " + comparacion.Sobran.Count + ". POST /vectores los pone al día.";
        }
        else
        {
            mensaje = "Iguales: los mismos " + enSql.Count + " números de fragmento en SQL y en Qdrant.";
        }
        if (iguales && info.VectoresIndexados < info.Puntos)
        {
            mensaje = mensaje + " El índice HNSW todavía cubre " + info.VectoresIndexados + " de " + info.Puntos + " vectores: el resto se busca recorriéndolos hasta que Qdrant termine de indexar.";
        }

        return new EstadoQdrant(iguales, _qdrant.Coleccion, enSql.Count, info.Puntos, info.VectoresIndexados, info.Estado,
            comparacion.Faltan.Count, comparacion.Sobran.Count,
            comparacion.Faltan.Take(20).ToList(), comparacion.Sobran.Take(20).ToList(), muestra, mensaje);
    }

    /// <summary>Pone Qdrant al día con SQL y anota el resultado en el detalle: sube los vectores que faltan y borra los puntos que sobran. Si los vectores de Qdrant son de otra base, la reconstruye. Si Qdrant no responde, lo anota como aviso y no falla: SQL ya quedó bien.</summary>
    public async Task PonerAlDiaAsync(List<string> detalle, CancellationToken ct)
    {
        try
        {
            if (!await _qdrant.ExisteColeccionAsync(ct))
            {
                await _qdrant.CrearColeccionAsync(ct);
                detalle.Add("QDRANT: se creó la colección " + _qdrant.Coleccion + ".");
            }
            List<int> enSql = await LeerIdsSqlAsync(ct);
            ComparacionIds comparacion = Comparar(enSql, await _qdrant.ListarIdsAsync(ct));
            if (await MuestraCoincideAsync(enSql, comparacion, ct) == false)
            {
                InformeQdrant reconstruccion = await ReconstruirAsync(ct);
                detalle.Add("QDRANT: los vectores eran de otra base; se reconstruyó la colección con " + reconstruccion.Subidos + " puntos.");
                return;
            }

            for (int i = 0; i < comparacion.Sobran.Count; i += 1000)
            {
                await _qdrant.BorrarPuntosAsync(comparacion.Sobran.Skip(i).Take(1000).ToList(), ct);
            }
            int subidos = await SubirAsync(comparacion.Faltan, ct);
            if (subidos == 0 && comparacion.Sobran.Count == 0)
            {
                detalle.Add("QDRANT: sin cambios, igual a SQL.");
            }
            else
            {
                detalle.Add("QDRANT: " + subidos + " puntos subidos y " + comparacion.Sobran.Count + " borrados.");
            }
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested)
            {
                throw;
            }
            // El tipo de error dice dónde falló: HttpRequestException es Qdrant que no responde, SqlException es SQL Server.
            detalle.Add("AVISO: no se pudo poner Qdrant al día (" + ex.GetType().Name + ": " + ex.Message + "). Cuando se arregle, POST /vectores lo pone al día.");
        }
    }

    /// <summary>Borra la colección, comprueba que se borró, la vuelve a crear y le sube todos los vectores de SQL.</summary>
    public async Task<InformeQdrant> ReconstruirAsync(CancellationToken ct)
    {
        Stopwatch reloj = Stopwatch.StartNew();
        List<string> detalle = new List<string>();
        int borrados = 0;
        if (await _qdrant.ExisteColeccionAsync(ct))
        {
            borrados = (await _qdrant.LeerInfoAsync(ct)).Puntos;
            // En Windows, antes de Qdrant 1.20.1, el borrado podía decir que sí y dejar la colección: por eso se comprueba.
            int intentos = 0;
            while (await _qdrant.ExisteColeccionAsync(ct))
            {
                intentos++;
                if (intentos > 3)
                {
                    throw new InvalidOperationException("Qdrant no borró la colección " + _qdrant.Coleccion + " después de 3 intentos. Pará Qdrant, borrá la carpeta de la colección adentro de su carpeta storage y volvé a arrancarlo.");
                }
                await _qdrant.BorrarColeccionAsync(ct);
                await Task.Delay(500, ct);
            }
            detalle.Add("Se borró la colección " + _qdrant.Coleccion + ", que tenía " + borrados + " puntos.");
        }

        await _qdrant.CrearColeccionAsync(ct);
        int subidos = await SubirAsync(await LeerIdsSqlAsync(ct), ct);
        detalle.Add("Se creó de nuevo y se subieron " + subidos + " puntos, todos los fragmentos con vector de SQL.");
        return new InformeQdrant(subidos, borrados, Indexador.FormatearTiempo(reloj.Elapsed), detalle);
    }

    /// <summary>Lee de SQL los números de los fragmentos que tienen vector, en orden.</summary>
    private async Task<List<int>> LeerIdsSqlAsync(CancellationToken ct)
    {
        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            IEnumerable<int> ids = await conexion.QueryAsync<int>(new CommandDefinition(
                "SELECT FragmentoId FROM rag.Fragmento WHERE Embedding IS NOT NULL ORDER BY FragmentoId;", cancellationToken: ct));
            return ids.ToList();
        }
    }

    /// <summary>Lee de SQL los vectores de los fragmentos pedidos y los sube a Qdrant, de a lotes. Devuelve cuántos subió.</summary>
    private async Task<int> SubirAsync(List<int> ids, CancellationToken ct)
    {
        int subidos = 0;
        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            await conexion.OpenAsync(ct);
            for (int i = 0; i < ids.Count; i += Lote)
            {
                List<int> lote = ids.Skip(i).Take(Lote).ToList();
                // SQL Server devuelve el vector como texto JSON si se lo convierte a NVARCHAR.
                IEnumerable<VectorGuardado> filas = await conexion.QueryAsync<VectorGuardado>(new CommandDefinition(
                    "SELECT FragmentoId, DocumentoId, CAST(Embedding AS NVARCHAR(MAX)) AS Vector " +
                    "FROM rag.Fragmento WHERE FragmentoId IN @lote AND Embedding IS NOT NULL;",
                    new { lote }, cancellationToken: ct));
                List<PuntoQdrant> puntos = new List<PuntoQdrant>();
                foreach (VectorGuardado fila in filas)
                {
                    float[] vector = JsonSerializer.Deserialize<float[]>(fila.Vector) ?? new float[0];
                    puntos.Add(new PuntoQdrant(fila.FragmentoId, vector, fila.DocumentoId));
                }
                if (puntos.Count > 0)
                {
                    await _qdrant.SubirAsync(puntos, ct);
                    subidos += puntos.Count;
                }
            }
        }
        return subidos;
    }

    /// <summary>Compara el vector de hasta tres fragmentos que están en los dos lados (el primero, el del medio y el último). Si la base se recreó, los números coinciden pero los textos no: los vectores lo delatan. Devuelve null si no hay fragmentos en común para comparar.</summary>
    private async Task<bool?> MuestraCoincideAsync(List<int> enSql, ComparacionIds comparacion, CancellationToken ct)
    {
        HashSet<int> faltan = new HashSet<int>(comparacion.Faltan);
        List<int> comunes = enSql.Where(id => !faltan.Contains(id)).ToList();
        if (comunes.Count == 0)
        {
            return null;
        }
        List<int> muestra = new List<int> { comunes[0], comunes[comunes.Count / 2], comunes[comunes.Count - 1] };
        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            foreach (int id in muestra.Distinct())
            {
                string? texto = await conexion.ExecuteScalarAsync<string?>(new CommandDefinition(
                    "SELECT CAST(Embedding AS NVARCHAR(MAX)) FROM rag.Fragmento WHERE FragmentoId = @id;",
                    new { id }, cancellationToken: ct));
                float[]? enQdrant = await _qdrant.LeerVectorAsync(id, ct);
                if (texto == null || enQdrant == null)
                {
                    continue;
                }
                float[] deSql = JsonSerializer.Deserialize<float[]>(texto) ?? new float[0];
                // Qdrant guarda los vectores normalizados, así que se compara la dirección (coseno) y no número por número.
                if (Coseno(deSql, enQdrant) < 0.999)
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>Calcula el coseno entre dos vectores: 1 si apuntan para el mismo lado.</summary>
    private static double Coseno(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0)
        {
            return 0;
        }
        double producto = 0;
        double normaA = 0;
        double normaB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            producto += a[i] * b[i];
            normaA += a[i] * a[i];
            normaB += b[i] * b[i];
        }
        if (normaA == 0 || normaB == 0)
        {
            return 0;
        }
        return producto / (Math.Sqrt(normaA) * Math.Sqrt(normaB));
    }

    /// <summary>Una fila de SQL con el vector de un fragmento como texto JSON.</summary>
    private sealed record VectorGuardado(int FragmentoId, int DocumentoId, string Vector);
}

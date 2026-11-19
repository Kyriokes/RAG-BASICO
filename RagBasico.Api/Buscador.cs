using Dapper;
using Microsoft.Data.SqlClient;

namespace RagBasico.Api;

/// <summary>Busca los fragmentos que mejor responden una pregunta.</summary>
public sealed class Buscador
{
    private readonly string _conexion;

    /// <summary>Lee la cadena de conexión de la configuración.</summary>
    public Buscador(IConfiguration config)
    {
        _conexion = config.GetConnectionString("Rag") ?? throw new InvalidOperationException("Falta ConnectionStrings:Rag en la configuración.");
    }

    /// <summary>Busca en el Full-Text de SQL Server los fragmentos que mejor coinciden con las palabras de la pregunta, del más relevante al menos relevante.</summary>
    public async Task<List<Fragmento>> PorPalabrasAsync(string pregunta, int topK, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(pregunta))
        {
            return new List<Fragmento>();
        }
        // FREETEXTTABLE corta la pregunta en palabras, busca también sus plurales y conjugaciones,
        // y devuelve cada fragmento con un puntaje (RANK). Se piden los 50 mejores y se usan los topK primeros.
        const string sql = @"
SELECT TOP (@topK) f.FragmentoId, d.Archivo, f.Texto
FROM FREETEXTTABLE(rag.Fragmento, Texto, @pregunta, LANGUAGE 3082, 50) AS ft
JOIN rag.Fragmento AS f ON f.FragmentoId = ft.[KEY]
JOIN rag.Documento AS d ON d.DocumentoId = f.DocumentoId
ORDER BY ft.[RANK] DESC;";
        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            IEnumerable<Fragmento> filas = await conexion.QueryAsync<Fragmento>(
                new CommandDefinition(sql, new { pregunta, topK }, cancellationToken: ct));
            return filas.ToList();
        }
    }
}

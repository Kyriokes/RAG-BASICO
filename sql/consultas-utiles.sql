-- consultas-utiles.sql: consultas para mirar la base desde SSMS.
-- No crea nada y scripts\crear-base.ps1 no la corre. Antes de usarla, elegí la base de la fase (por ejemplo RagBasico_F2).

-- Cuántos documentos y fragmentos hay, y el largo promedio de un fragmento.
SELECT COUNT(*) AS Documentos FROM rag.Documento;
SELECT COUNT(*) AS Fragmentos, AVG(LEN(Texto)) AS CaracteresPromedio FROM rag.Fragmento;

-- Tres fragmentos al azar: ¿el texto se entiende, o sale con palabras pegadas o columnas mezcladas?
SELECT TOP (3) Texto FROM rag.Fragmento ORDER BY NEWID();

-- Cada documento con su huella de carga y cuántos fragmentos tiene. Sin huella (NULL): se cargó antes de
-- sql\03_huella.sql, y el próximo /indexar lo recarga.
SELECT d.Archivo, CONVERT(VARCHAR(64), d.Huella, 2) AS Huella, d.FechaCarga, COUNT(f.FragmentoId) AS Fragmentos
FROM rag.Documento AS d
LEFT JOIN rag.Fragmento AS f ON f.DocumentoId = d.DocumentoId
GROUP BY d.Archivo, d.Huella, d.FechaCarga
ORDER BY d.Archivo;

-- Los fragmentos de un documento, en orden: para revisar un PDF que /indexar marcó con TEXTO DUDOSO.
SELECT f.Orden, LEN(f.Texto) AS Caracteres, f.Texto
FROM rag.Fragmento AS f
JOIN rag.Documento AS d ON d.DocumentoId = f.DocumentoId
WHERE d.Archivo = N'nombre-del-archivo.pdf'
ORDER BY f.Orden;

-- Fragmentos de menos de 30 palabras (contando espacios): con el corte por párrafos casi no tendría que haber.
SELECT TOP (20) d.Archivo, f.Orden, f.Texto
FROM rag.Fragmento AS f
JOIN rag.Documento AS d ON d.DocumentoId = f.DocumentoId
WHERE LEN(f.Texto) - LEN(REPLACE(f.Texto, N' ', N'')) < 30
ORDER BY d.Archivo, f.Orden;

-- ¿Terminó el Full-Text de indexar? Pendientes = 0 y Estado = 0 quiere decir que sí.
SELECT OBJECTPROPERTYEX(OBJECT_ID(N'rag.Fragmento'), 'TableFulltextPendingChanges') AS Pendientes,
       OBJECTPROPERTYEX(OBJECT_ID(N'rag.Fragmento'), 'TableFulltextPopulateStatus') AS Estado;

-- Una búsqueda por palabras a mano, como la que hace Buscador.PorPalabrasAsync.
SELECT TOP (5) ft.[RANK], d.Archivo, LEFT(f.Texto, 150) AS Inicio
FROM FREETEXTTABLE(rag.Fragmento, Texto, N'requisitos reintegro', LANGUAGE 3082) AS ft
JOIN rag.Fragmento AS f ON f.FragmentoId = ft.[KEY]
JOIN rag.Documento AS d ON d.DocumentoId = f.DocumentoId
ORDER BY ft.[RANK] DESC;

-- Qué palabras busca el Full-Text a partir de una: plurales y conjugaciones, no derivadas.
SELECT display_term, expansion_type
FROM sys.dm_fts_parser(N'FORMSOF(FREETEXT, cancelo)', 3082, 0, 0);

-- Cuántos fragmentos tienen vector y cuántos faltan. Faltan: correr POST /vectores.
SELECT COUNT(*) AS Fragmentos,
       SUM(CASE WHEN Embedding IS NULL THEN 0 ELSE 1 END) AS ConVector
FROM rag.Fragmento;

-- Los 5 vecinos más cercanos de un fragmento: tendrían que ser del mismo tema. El primero es él mismo, con distancia 0.
-- Toma el primer fragmento con vector que exista, no el número 1, que puede no existir después de recargar.
DECLARE @id INT = (SELECT MIN(FragmentoId) FROM rag.Fragmento WHERE Embedding IS NOT NULL);
DECLARE @vector VECTOR(1024) = (SELECT Embedding FROM rag.Fragmento WHERE FragmentoId = @id);
SELECT TOP (6) f.FragmentoId, d.Archivo, VECTOR_DISTANCE('cosine', f.Embedding, @vector) AS Distancia, LEFT(f.Texto, 150) AS Inicio
FROM rag.Fragmento AS f
JOIN rag.Documento AS d ON d.DocumentoId = f.DocumentoId
WHERE f.Embedding IS NOT NULL
ORDER BY Distancia;

-- Cuánto tarda SQL Server en recorrer todos los vectores: es una búsqueda exacta, sin índice.
-- El tiempo sale en la pestaña Messages ("CPU time" y "elapsed time").
DECLARE @id2 INT = (SELECT MIN(FragmentoId) FROM rag.Fragmento WHERE Embedding IS NOT NULL);
DECLARE @vector2 VECTOR(1024) = (SELECT Embedding FROM rag.Fragmento WHERE FragmentoId = @id2);
SET STATISTICS TIME ON;
SELECT TOP (5) f.FragmentoId
FROM rag.Fragmento AS f
WHERE f.Embedding IS NOT NULL
ORDER BY VECTOR_DISTANCE('cosine', f.Embedding, @vector2);
SET STATISTICS TIME OFF;

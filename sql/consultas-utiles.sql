-- consultas-utiles.sql: consultas para mirar la base desde SSMS.
-- No crea nada y scripts\crear-base.ps1 no la corre. Antes de usarla, elegí la base del RAG (en casa, RagBasico).

-- Cuántos documentos hay en cada estado: pendiente, listo, sin texto o error.
SELECT Estado, COUNT(*) AS Documentos FROM rag.Documento GROUP BY Estado;

-- Cuántos fragmentos hay, y el largo promedio de un fragmento.
SELECT COUNT(*) AS Fragmentos, AVG(LEN(Texto)) AS CaracteresPromedio FROM rag.Fragmento;

-- Tres fragmentos al azar: ¿el texto se entiende, o sale con palabras pegadas o columnas mezcladas?
SELECT TOP (3) Texto FROM rag.Fragmento ORDER BY NEWID();

-- Cada documento con su estado, de dónde vino y cuántos fragmentos tiene. Los 100 más nuevos.
-- OrigenId con un número: se importó de la base de origen. En NULL: se arrastró a la página.
SELECT TOP (100) d.DocumentoId, d.Archivo, d.OrigenId, d.Estado, d.Detalle, d.FechaCarga, d.FechaProceso,
       (SELECT COUNT(*) FROM rag.Fragmento AS f WHERE f.DocumentoId = d.DocumentoId) AS Fragmentos,
       DATALENGTH(d.Contenido) / 1024 AS KbGuardados
FROM rag.Documento AS d
ORDER BY d.DocumentoId DESC;

-- Los documentos que no se pudieron cargar, con el motivo.
SELECT DocumentoId, Archivo, OrigenId, Estado, Detalle
FROM rag.Documento
WHERE Estado IN ('error', 'sin texto')
ORDER BY DocumentoId;

-- Los documentos que quedaron listos con el texto dudoso: conviene mirar sus fragmentos.
SELECT DocumentoId, Archivo, Detalle FROM rag.Documento WHERE Estado = 'listo' AND Detalle IS NOT NULL;

-- Los fragmentos de un documento, en orden: para revisar un PDF con el texto dudoso.
SELECT f.Orden, f.Pagina, f.PaginaFin, LEN(f.Texto) AS Caracteres, f.Texto
FROM rag.Fragmento AS f
JOIN rag.Documento AS d ON d.DocumentoId = f.DocumentoId
WHERE d.Archivo = N'nombre-del-archivo.pdf'
ORDER BY f.Orden;

-- Los documentos con fragmentos sin página: se cargaron antes de que se guardara. Se arregla con "Reprocesar todo".
SELECT d.DocumentoId, d.Archivo, COUNT(*) AS FragmentosSinPagina
FROM rag.Fragmento AS f
JOIN rag.Documento AS d ON d.DocumentoId = f.DocumentoId
WHERE f.Pagina IS NULL
GROUP BY d.DocumentoId, d.Archivo
ORDER BY d.DocumentoId;

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
WHERE d.Estado = 'listo'
ORDER BY ft.[RANK] DESC;

-- Qué palabras busca el Full-Text a partir de una: plurales y conjugaciones, no derivadas.
SELECT display_term, expansion_type
FROM sys.dm_fts_parser(N'FORMSOF(FREETEXT, cancelo)', 3082, 0, 0);

-- Cuánto ocupa la base. Express admite 50 GB por base (10 GB antes de la versión 2025).
EXEC sp_spaceused;

-- Las últimas 20 preguntas, con lo que tardó cada parte.
SELECT TOP (20) Fecha, Modo, Pregunta, LEFT(Respuesta, 150) AS Respuesta, MsBusqueda, MsModelo
FROM rag.Consulta
ORDER BY ConsultaId DESC;

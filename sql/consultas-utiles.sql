-- consultas-utiles.sql: consultas para mirar la base desde SSMS.
-- No crea nada y scripts\crear-base.ps1 no la corre. Antes de usarla, elegí la base de la fase (por ejemplo RagBasico_F2).

-- Cuántos documentos y fragmentos hay, y el largo promedio de un fragmento.
SELECT COUNT(*) AS Documentos FROM rag.Documento;
SELECT COUNT(*) AS Fragmentos, AVG(LEN(Texto)) AS CaracteresPromedio FROM rag.Fragmento;

-- Tres fragmentos al azar: ¿el texto se entiende, o sale con palabras pegadas o columnas mezcladas?
SELECT TOP (3) Texto FROM rag.Fragmento ORDER BY NEWID();

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

-- 03_huella.sql: la huella de carga de cada documento, para no volver a procesar los PDF que no cambiaron.
-- Se corre con scripts\crear-base.ps1 (variable Base). Se puede correr de nuevo sin romper nada.
PRINT N'Base: $(Base)';
USE [$(Base)];
GO

-- La huella es el sha256 de los bytes del PDF más los parámetros de corte: 32 bytes.
-- Queda en NULL en los documentos cargados antes de este script; el próximo /indexar los toma como cambiados y los recarga.
IF COL_LENGTH(N'rag.Documento', N'Huella') IS NULL
    ALTER TABLE rag.Documento ADD Huella VARBINARY(32) NULL;
GO

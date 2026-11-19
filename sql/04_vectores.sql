-- 04_vectores.sql: el vector de cada fragmento, para buscar por significado.
-- Se corre con scripts\crear-base.ps1 (variable Base). Se puede correr de nuevo sin romper nada.
PRINT N'Base: $(Base)';
USE [$(Base)];
GO

-- 1024 números por fragmento, los que devuelve qwen3-embedding:0.6b. Queda en NULL hasta que POST /vectores lo calcula;
-- los fragmentos nuevos, y los de un PDF que cambió, también empiezan en NULL.
IF COL_LENGTH(N'rag.Fragmento', N'Embedding') IS NULL
    ALTER TABLE rag.Fragmento ADD Embedding VECTOR(1024) NULL;
GO

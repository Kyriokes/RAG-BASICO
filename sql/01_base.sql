-- 01_base.sql: la base de la fase, el esquema rag, los documentos, los fragmentos y el índice Full-Text.
-- Se corre con scripts\crear-base.ps1, que le pasa el nombre de la base en la variable Base.
-- En SSMS: menú Query, "SQLCMD Mode", y en la primera línea de la ventana: :setvar Base RagBasico_F2
-- Se puede correr de nuevo sin romper nada: cada paso controla si ya está hecho.
PRINT N'Base: $(Base)';
IF DB_ID(N'$(Base)') IS NULL
    CREATE DATABASE [$(Base)];
GO
USE [$(Base)];
GO
IF SCHEMA_ID(N'rag') IS NULL
    EXEC(N'CREATE SCHEMA rag');
GO

-- Un registro por PDF.
IF OBJECT_ID(N'rag.Documento') IS NULL
CREATE TABLE rag.Documento (
    DocumentoId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_rag_Documento PRIMARY KEY,
    Archivo      NVARCHAR(260) NOT NULL CONSTRAINT UQ_rag_Documento_Archivo UNIQUE,
    Titulo       NVARCHAR(400) NOT NULL,
    FechaCarga   DATETIME2 NOT NULL CONSTRAINT DF_rag_Documento_Fecha DEFAULT SYSDATETIME()
);
GO

-- Los fragmentos de cada PDF. Si se borra un documento, sus fragmentos se borran solos (cascada).
IF OBJECT_ID(N'rag.Fragmento') IS NULL
CREATE TABLE rag.Fragmento (
    FragmentoId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_rag_Fragmento PRIMARY KEY,
    DocumentoId  INT NOT NULL CONSTRAINT FK_rag_Fragmento_Documento
                     REFERENCES rag.Documento (DocumentoId) ON DELETE CASCADE,
    Orden        INT NOT NULL,
    Texto        NVARCHAR(MAX) NOT NULL
);
GO

-- Full-Text en español (3082), sin distinguir acentos. CHANGE_TRACKING AUTO lo actualiza solo, en segundo plano.
IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'ftRag')
    CREATE FULLTEXT CATALOG ftRag WITH ACCENT_SENSITIVITY = OFF;
GO
IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'rag.Fragmento'))
    CREATE FULLTEXT INDEX ON rag.Fragmento (Texto LANGUAGE 3082)
        KEY INDEX PK_rag_Fragmento ON ftRag
        WITH (CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM);
GO

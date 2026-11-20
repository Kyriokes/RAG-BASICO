-- base.sql: toda la base del RAG en un solo script: los documentos, los fragmentos, el índice Full-Text y el registro de consultas.
-- En casa se corre con scripts\crear-base.ps1, que le pasa el nombre de la base en la variable Base.
-- En SSMS: menú Query, "SQLCMD Mode", y en la primera línea de la ventana: :setvar Base RagBasico
-- Se puede correr de nuevo sin romper nada: cada paso controla si ya está hecho.
-- Usa solo SQL que entiende un SQL Server 2014: no guarda vectores, que van solo en Qdrant.
PRINT N'Base: $(Base)';
IF DB_ID(N'$(Base)') IS NULL
    CREATE DATABASE [$(Base)];
GO
USE [$(Base)];
GO
-- Los índices con WHERE (filtrados) piden esta opción prendida. sqlcmd la trae apagada; SSMS y la API, prendida.
SET QUOTED_IDENTIFIER ON;
GO
IF SCHEMA_ID(N'rag') IS NULL
    EXEC(N'CREATE SCHEMA rag');
GO

-- Un registro por PDF, con su estado: pendiente, listo, sin texto o error.
-- Un PDF arrastrado a la página guarda sus bytes en Contenido y no tiene OrigenId.
-- Un PDF importado de la base de origen guarda su número de allá en OrigenId y no guarda los bytes: se leen de allá.
IF OBJECT_ID(N'rag.Documento') IS NULL
CREATE TABLE rag.Documento (
    DocumentoId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_rag_Documento PRIMARY KEY,
    Archivo       NVARCHAR(260) NOT NULL,
    Titulo        NVARCHAR(400) NOT NULL,
    OrigenId      NVARCHAR(100) NULL,
    Contenido     VARBINARY(MAX) NULL,
    Huella        VARBINARY(32) NULL,
    Estado        VARCHAR(20) NOT NULL CONSTRAINT DF_rag_Documento_Estado DEFAULT 'pendiente',
    Detalle       NVARCHAR(1000) NULL,
    FechaCarga    DATETIME2 NOT NULL CONSTRAINT DF_rag_Documento_Fecha DEFAULT SYSDATETIME(),
    FechaProceso  DATETIME2 NULL
);
GO
-- Entre los arrastrados no puede haber dos con el mismo nombre de archivo.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_rag_Documento_Archivo')
    CREATE UNIQUE INDEX UX_rag_Documento_Archivo ON rag.Documento (Archivo) WHERE OrigenId IS NULL;
GO
-- Entre los importados no puede haber dos con el mismo número de origen. El nombre sí se puede repetir.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_rag_Documento_OrigenId')
    CREATE UNIQUE INDEX UX_rag_Documento_OrigenId ON rag.Documento (OrigenId) WHERE OrigenId IS NOT NULL;
GO
-- Para encontrar rápido el próximo pendiente y contar por estado, también con decenas de miles de documentos.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_rag_Documento_Estado')
    CREATE INDEX IX_rag_Documento_Estado ON rag.Documento (Estado, DocumentoId);
GO

-- Los fragmentos de cada PDF. Si se borra un documento, sus fragmentos se borran solos (cascada).
-- El número de fragmento es también el id de su vector en Qdrant.
IF OBJECT_ID(N'rag.Fragmento') IS NULL
CREATE TABLE rag.Fragmento (
    FragmentoId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_rag_Fragmento PRIMARY KEY,
    DocumentoId  INT NOT NULL CONSTRAINT FK_rag_Fragmento_Documento
                     REFERENCES rag.Documento (DocumentoId) ON DELETE CASCADE,
    Orden        INT NOT NULL,
    Texto        NVARCHAR(MAX) NOT NULL
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_rag_Fragmento_Documento')
    CREATE INDEX IX_rag_Fragmento_Documento ON rag.Fragmento (DocumentoId);
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

-- El registro de cada pregunta, con su respuesta y lo que tardó cada parte.
IF OBJECT_ID(N'rag.Consulta') IS NULL
CREATE TABLE rag.Consulta (
    ConsultaId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_rag_Consulta PRIMARY KEY,
    Fecha          DATETIME2 NOT NULL CONSTRAINT DF_rag_Consulta_Fecha DEFAULT SYSDATETIME(),
    Pregunta       NVARCHAR(2000) NOT NULL,
    Modo           VARCHAR(20) NOT NULL,
    FragmentosIds  NVARCHAR(400) NULL,
    Respuesta      NVARCHAR(MAX) NULL,
    MsBusqueda     INT NULL,
    MsModelo       INT NULL,
    MsCarga        INT NULL,
    MsLectura      INT NULL,
    MsEscritura    INT NULL
);
GO

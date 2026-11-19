-- 02_consultas.sql: el registro de cada pregunta, con su respuesta y lo que tardó cada parte.
-- Se corre con scripts\crear-base.ps1 (variable Base). Se puede correr de nuevo sin romper nada.
PRINT N'Base: $(Base)';
USE [$(Base)];
GO

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

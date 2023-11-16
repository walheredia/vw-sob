- Crear un evento en eventos config, con id 10004
- Crear esta tabla

USE [Servicios]
GO

/****** Object:  Table [dbo].[AgendaTurnosTerm]    Script Date: 19/9/2023 18:39:19 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[AgendaTurnosTerm](
	[Referencia] [int] NOT NULL,
	[Id_TurnoTerm] [int] NOT NULL,
        [Origen] NVARCHAR(10) NOT NULL
 CONSTRAINT [PK_AgendaTurnosTerm] PRIMARY KEY CLUSTERED 
(
	[Referencia] ASC,
	[Id_TurnoTerm] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


como ejecutar el exe con argumentos
vwsob.exe --sync-from-sob --sync-to-sob

Eventos_Config.NroRegistro1
podemos colocar el número de referencia de un turno de agenda para tomar como base de la ultima agenda procesada, tomando como origen "Local"

23
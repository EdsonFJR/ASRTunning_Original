USE [WayLogDB]
GO
/****** Object:  Table [dbo].[TunningConfiguration]    Script Date: 06/06/2017 15:08:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TunningConfiguration](
	[TaskInterval] [int] NOT NULL,
	[RepositoryPath] [varchar](100) NOT NULL,
	[AlternativeRepositoryPath] [varchar](100) NOT NULL,
	[Active] [bit] NOT NULL,
	[MinimumAvailableSpace] [int] NULL,
	[NTDomainUser] [varchar](100) NULL,
	[NTPassword] [varchar](100) NULL,
	[Site] [varchar](50) NULL
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[TunningData]    Script Date: 06/06/2017 15:08:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TunningData](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Time] [datetime] NOT NULL,
	[Channel] [varchar](40) NOT NULL,
	[ENDR] [varchar](10) NULL,
	[RSTT] [varchar](10) NULL,
	[RSLT] [varchar](100) NULL,
	[SPOK] [varchar](100) NULL,
	[Confidence] [int] NULL,
	[WVNM] [varchar](100) NOT NULL,
	[OriginalPath] [varchar](100) NULL,
	[CurrentPath] [varchar](100) NULL,
	[CallId] [varchar](16) NULL,
	[Grammar] [varchar](60) NULL,
	[Tenant] [varchar](30) NULL,
	[Step] [varchar](50) NULL,
	[TaskId] [int] NULL,
	[Transcription] [varchar](100) NULL,
	[UserTranscription] [int] NULL,
	[DateTranscription] [datetime] NULL,
	[EvaluationId] [int] NULL,
	[IsAvailable] [bit] NULL,
	[DateMakeTranscription] [datetime] NULL,
	[DateReserve] [datetime] NULL,
 CONSTRAINT [PK_TunningData] PRIMARY KEY CLUSTERED 
(
	[WVNM] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[TunningRecognitionServers]    Script Date: 06/06/2017 15:08:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TunningRecognitionServers](
	[ServerId] [int] IDENTITY(1,1) NOT NULL,
	[IP] [varchar](20) NULL,
	[CurrentTunningPath] [varchar](100) NOT NULL,
	[Active] [bit] NOT NULL,
	[ServerType] [int] NOT NULL
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[TunningTask]    Script Date: 06/06/2017 15:08:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TunningTask](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Time] [datetime] NOT NULL,
	[UserId] [int] NOT NULL,
	[DesirableSamples] [int] NOT NULL,
	[Description] [varchar](50) NULL,
	[StartPeriod] [datetime] NULL,
	[Status] [int] NULL,
	[CreationDate] [datetime] NULL,
	[StartDate] [datetime] NULL,
	[EndDate] [datetime] NULL
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[TunningTaskDetail]    Script Date: 06/06/2017 15:08:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TunningTaskDetail](
	[TaskId] [int] NOT NULL,
	[Type] [int] NOT NULL,
	[Values] [varchar](80) NULL
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[UserTranscription]    Script Date: 06/06/2017 15:08:44 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserTranscription](
	[UserId] [int] IDENTITY(1,1) NOT NULL,
	[Login] [nvarchar](100) NOT NULL,
	[Password] [nvarchar](100) NOT NULL,
	[UserName] [nvarchar](510) NOT NULL,
	[UserType] [int] NULL,
	[Activated] [bit] NULL,
 CONSTRAINT [PK_UserTranscription] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[Login] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
ALTER TABLE [dbo].[TunningTask] ADD  CONSTRAINT [DF_TunningTask_CreationTime]  DEFAULT (getdate()) FOR [CreationDate]
GO


GO
ALTER TaBLE [WayLogDB].[dbo].[TunningConfiguration] 
ADD [Site] VARCHAR(50) NULL

GO
/* Sample Insert Configuration
INSERT 	[WayLogDB].[dbo].[TunningConfiguration]  (TaskInterval, RepositoryPath, AlternativeRepositoryPath, Active, MinimumAvailableSpace, [Site])
VALUES (120000, '\\<IP>\Nuance', '\\<IP>\Nuance', 1, 1073741824, 'SiteNameXXX')
GO
*/	

/* Sample Insert RecognitionServers
INSERT 	[WayLogDB].[dbo].[TunningRecognitionServers]  ([IP],[CurrentTunningPath],[Active],[ServerType])
VALUES ('<IP>', 'NuanceLogs', 1, 0)
GO
*/	

/* Sample Insert Tasks 
DECLARE @IdTask INT 
INSERT [WayLogDB].[dbo].[TunningTask] ([Time],[UserId],[DesirableSamples],[Description] ,[StartPeriod],[Status] ,[CreationDate] )
   VALUES (GETDATE(), 1, 100, 'Samples Grammar XXXXX', DATEADD(DAY, -7, GETDATE()), NULL, GETDATE() )
SET @IdTask = SCOPE_IDENTITY()

INSERT [WayLogDB].[dbo].[TunningTaskDetail] ([TaskId],[Type],[Values],[TNATRegex] )
   VALUES (@IdTask, 1, 'NomeGramatica', '' )
--   VALUES (@IdTask, 1, 'NomeGramatica', '123|456'/*Lista de campanhas*/ ) 

UPDATE [WayLogDB].[dbo].[TunningTask] SET [Status] = 0 WHERE ID = @IdTask and CreationDate >= CONVERT(VARCHAR(10), GETDATE(), 120)

GO

*/

GO
ALTER TaBLE [WayLogDB].[dbo].[TunningData] 
ADD [TNAT] VARCHAR(50) NULL

GO
ALTER TABLE [WayLogDB].[dbo].[TunningData] 
ADD [NBST] INT NULL

GO
ALTER TABLE [WayLogDB].[dbo].[TunningData] 
ADD [NBSTResult] VARCHAR(MAX) NULL

GO
ALTER TABLE [WayLogDB].[dbo].[TunningTaskDetail] 
ADD [TNATRegex] VARCHAR(100) NULL




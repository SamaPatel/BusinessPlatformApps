SET ANSI_NULLS              ON;
SET ANSI_PADDING            ON;
SET ANSI_WARNINGS           ON;
SET ANSI_NULL_DFLT_ON       ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER       ON;
go

CREATE VIEW [fb].[CommentsPostsView] AS
(

SELECT 
       [Original Id]
      ,[Created Date]
      ,CONVERT(date, [Created Date]) AS [Created Date Only]
      ,[Message]
      ,[From Id]
      ,[From Name]
      ,[Page]
      ,[PageDisplayName]
      ,[PageId]
      ,[media]
      ,[Original Id] AS [Original Post Id]
      ,'Post' AS [Type]
      ,CASE WHEN CONVERT(nvarchar,[From Id]) = [PageId] THEN 'Internal' ELSE 'External' END AS PostType
      ,CASE WHEN CONVERT(nvarchar,[From Id]) = [PageId] THEN '1' ELSE '0' END AS IsPageOwner
  FROM [fb].[Posts]

  UNION ALL 

  SELECT 
       [Original Id]
      ,[Created Date]
      ,CONVERT(date, [Created Date]) AS [Created Date Only]
      ,[Message]
      ,[From Id]
      ,[From Name]
      ,[Page]
      ,[PageDisplayName]
      ,[PageId]
      ,null AS [media]
      ,[Original Post Id]
      ,'Comment' AS [Type]
      ,'' AS PostType
      ,CASE WHEN CONVERT(nvarchar,[From Id]) = [PageId] THEN '1' ELSE '0' END AS IsPageOwner
  FROM [fb].[Comments]
  )
GO


CREATE VIEW [fb].[HashTagView] AS (

SELECT
	[Original Id]
    ,[HashTags]
FROM [fb].[HashTags]
)
GO


CREATE VIEW [fb].[KeyPhraseView] AS (
SELECT 
       [Original Id]
      ,[KeyPhrase]
FROM [fb].[KeyPhrase]
)
GO


CREATE VIEW [fb].[PostsView] AS (
SELECT 
      [Original Id]
      ,[Total Likes]
      ,[Total Shares]
      ,[Total Reactions]
      ,[Total Comments]
  FROM [fb].[Posts]
  )


GO

CREATE VIEW [fb].[ReactionsView] AS (
SELECT [Original Id]
      ,[Reaction Type]
      ,[From Id]
      ,[From Name]
FROM [fb].[Reactions]
)
GO 

CREATE VIEW [fb].[SentimentView] AS (
SELECT [Original Id]
      ,[Sentiment]
FROM [fb].[Sentiment]
)
GO

CREATE VIEW [fb].[UsersView] AS (
SELECT [Id]
      ,[Name]
FROM [fb].[Users]
)
GO

CREATE VIEW [fb].[PagesView] AS (
SELECT DISTINCT 
        [PageId]
      ,[PageDisplayName]
FROM [fb].[Posts]
)
GO

CREATE VIEW [fb].[EdgesView] AS (
SELECT  
     [SourceVertex]
	 ,s.[Name] AS [SourceVertexName] 
    ,[TargetVertex]
	,t.[Name] AS [TargetVertexName] 
    ,[EdgeWeight] 
	,[PageId] 
FROM [fb].[Edges]
JOIN fb.[Users] s ON s.Id = [SourceVertex] 
JOIN fb.[Users] t ON t.Id = [TargetVertex]
)

GO

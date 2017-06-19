SET ANSI_NULLS              ON;
SET ANSI_PADDING            ON;
SET ANSI_WARNINGS           ON;
SET ANSI_NULL_DFLT_ON       ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER       ON;
go

-- AccountView
CREATE VIEW smgt.accountview
AS
  SELECT accountid                          AS [Account Id],
         [NAME]                             AS [Account Name],
         ownerid                            AS [Owner Id],
         territoryid                        AS [Territory Id],
         osm.LocalizedLabel                 AS [Industry],
         owningbusinessunit                 AS [Business Unit Id],
         address1_city                      AS [City],
         address1_stateorprovince           AS [State],
         address1_country                   AS [Country],
         revenue                            AS [Annual Revenue]
  FROM   dbo.account LEFT OUTER JOIN dbo.OptionSetMetadata osm ON account.industrycode=osm.[Option] AND
                                                                  osm.IsUserLocalizedLabel=0 AND osm.LocalizedLabelLanguageCode=1033 AND
                                                                  osm.OptionSetName = 'industrycode'  COLLATE Latin1_General_100_CI_AS AND
                                                                  osm.EntityName = 'account' COLLATE Latin1_General_100_CI_AS
go
create view csrv.caseview as
select incidentid                          AS [Case Id],
       accountid                           AS [Account Id],
	   customerid                          AS [Customer Id],
	   i.ownerid                           AS [Owner Id],
	   title                               AS Title,
       Cast(i.createdon AS DATE)           AS [Created On Date],
       i.createdon                         AS [Created On],
	   Cast(i.resolveby AS DATE)           AS [Resolve By Date],
	   sentimentvalue                      AS [Sentiment Score],
	   resolvekpistat.[Value]              AS [Resolve SLA Status],
	   resolvekpistat.LocalizedLabel       AS [Resolve SLA],
	   responsekpistat.[Value]             AS [First Response SLA Status],
	   responsekpistat.LocalizedLabel      AS [First Response SLA],
	   i.modifiedon                        AS [Modified On],
	   case when osm2.LocalizedLabel='Active' THEN null ELSE Cast(i.modifiedon AS DATE) END AS [Resolved On Date],
       osm1.LocalizedLabel                 AS [Status],
       osm2.LocalizedLabel                 AS [State],
	   coc.LocalizedLabel                  AS [Origin],
	   prio.LocalizedLabel                 AS [Priority],
	   case when escalatedOn IS NULL then 0 else 1 end AS [Escalated],
	   Cast(responseby AS DATE)            AS [Response By Date],
	   Cast(followupby AS DATE)            AS [Follow Up By Date],
	   Cast(escalatedon AS DATE)           AS [Escalated Date],
	   customersatisfactioncode            AS [Customer Satisfaction Code],
	   csat.LocalizedLabel                 AS [Customer Satisfaction]
    FROM   dbo.incident i
	   LEFT OUTER JOIN dbo.slakpiinstance slakpiresolve on i.resolvebykpiid=slakpiresolve.Id
	   LEFT OUTER JOIN dbo.slakpiinstance slakpiresponse on i.firstresponsebykpiid=slakpiresponse.Id
	   LEFT OUTER JOIN dbo.StatusMetadata osm1 ON i.statuscode=osm1.[Status] AND osm1.IsUserLocalizedLabel=0 AND osm1.LocalizedLabelLanguageCode=1033 AND osm1.EntityName='incident' COLLATE Latin1_General_100_CI_AS
       LEFT OUTER JOIN dbo.StateMetadata osm2 ON i.statecode=osm2.[State] AND osm2.IsUserLocalizedLabel=0 AND osm2.LocalizedLabelLanguageCode=1033 AND osm2.EntityName='incident' COLLATE Latin1_General_100_CI_AS
       LEFT OUTER JOIN 
		(select 
				[Option] as [Value], 
				[LocalizedLabel] 
				from [dbo].[GlobalOptionSetMetadata] 
				where  [OptionsetName] = 'caseorigincode'
			) coc on i.caseorigincode= coc.[value]
		LEFT OUTER JOIN
		(select 
				[Option] as [Value], 
				[LocalizedLabel] 
				from [dbo].optionsetmetadata 
				where [OptionsetName] = 'prioritycode' and EntityName='incident'
		) prio on i.prioritycode = prio.[value]
		LEFT OUTER JOIN
		(select 
				[Option] as [Value], 
				[LocalizedLabel] 
				from [dbo].optionsetmetadata 
				where [OptionsetName] = 'customersatisfactioncode' and EntityName='incident'
		) csat on i.customersatisfactioncode = csat.[value]
		LEFT OUTER JOIN 
		(select 
				[Option] as [Value], 
				[LocalizedLabel] 
				from [dbo].optionsetmetadata 
				where [OptionsetName] = 'status' and EntityName='slakpiinstance'
		) resolvekpistat on slakpiresolve.[status] = resolvekpistat.[value]
		LEFT OUTER JOIN 
		(select 
				[Option] as [Value], 
				[LocalizedLabel] 
				from [dbo].optionsetmetadata 
				where [OptionsetName] = 'status' and EntityName='slakpiinstance'
		) responsekpistat on slakpiresponse.[status] = responsekpistat.[value]


GO

create view csrv.customerview AS
SELECT   id                                 AS [Customer Id],
         [NAME]                             AS [Customer Name],
		 address1_city                      AS [City],
         address1_stateorprovince           AS [State],
         address1_country                   AS [Country]
FROM account
WHERE id in (SELECT customerid FROM incident WHERE customerid_entitytype='account')
UNION ALL
SELECT   id                                 AS [Customer Id],
         [yomifullname]                     AS [Customer Name],
		 address1_city                      AS [City],
         address1_stateorprovince           AS [State],
         address1_country                   AS [Country]
FROM contact
WHERE id in (SELECT customerid FROM incident WHERE customerid_entitytype='contact')


GO
create view csrv.activityview as
select id                                   AS [Activity Id],
regardingobjectid                           AS [Case Id],
directioncode                               AS [Direction Code],
1                                           AS [Activity Type]
from email
WHERE regardingobjectid_entitytype = 'Incident'
UNION ALL
select id                                   AS [Activity Id],
regardingobjectid                           AS [Case Id],
directioncode                               AS [Direction Code],
2                                           AS [Activity Type]
from fax
WHERE regardingobjectid_entitytype = 'Incident'
UNION ALL
select id                                   AS [Activity Id],
regardingobjectid                           AS [Case Id],
directioncode                               AS [Direction Code],
3                                           AS [Activity Type]
from phonecall
WHERE regardingobjectid_entitytype = 'Incident'

go  

create view csrv.surveyresponseview AS
SELECT
Id                                          AS [Survey Response Id],
msdyn_surveyid                              AS [Survey Id],
owninguser                                  AS [Owner Id],
nps.localizedlabel                          AS [NPS Type],
msdyn_case                                  AS [Case Id],
CAST(msdyn_completedon AS DATE)             AS [Completed On Date],
msdyn_scoreaspercentage                     AS [Score]
FROM [dbo].[msdyn_surveyresponse] sr
LEFT OUTER JOIN (
   SELECT 
     [Option] as [Value], 
     [LocalizedLabel] 
   FROM [dbo].[GlobalOptionSetMetadata] 
   WHERE  [OptionsetName] = 'msdyn_npstype'
) nps on sr.msdyn_npstype= nps.[value]	GO


create view csrv.surveyview as
select id                                   AS [Survey Id],
       msdyn_name                           AS [Survey Name]
FROM [dbo].[msdyn_survey]

GO

-- ConfigurationView
CREATE VIEW smgt.configurationview
AS
    SELECT [id],
            configuration_group    AS [Configuration Group],
            configuration_subgroup AS [Configuration Subgroup],
            [name]                 AS [Name],
            [value]                AS [Value]
    FROM   smgt.[configuration]
    WHERE  visible = 1;
go

-- DateView
CREATE VIEW smgt.dateview
AS
    SELECT date_key,
           full_date        AS [Date],
           day_of_week      AS [Day of the Week],
           day_num_in_month AS [Day Number of the Month],
           day_name         AS [Day Name],
           day_abbrev       AS [Day Abbreviated],
           weekday_flag     AS [Weekday Flag],
           [month],
           month_name       AS [Month Name],
           month_abbrev,
           [quarter],
           [year],
           same_day_year_ago_date,
           week_begin_date  AS [Week Begin Date]
    FROM   smgt.[date];
go

-- LeadView
CREATE VIEW [smgt].[leadview]
AS
    SELECT estimatedamount               AS [Estimated Amount],
           osm1.LocalizedLabel           AS [Status],
           osm2.LocalizedLabel           AS [Lead Quality],
           [subject]                     AS [Subject],
           jobtitle                      AS [Job Title],
           leadid                        AS [Lead Id],
           estimatedamount_base          AS [Estimated Amount Base],
           ownerid                       AS [Owner Id],
           osm3.LocalizedLabel           AS [State],
           campaignid                    AS [Campaign Id],
           estimatedclosedate            AS [Estimated Close Date],
           osm4.LocalizedLabel           AS [Lead Source Name],
           osm5.LocalizedLabel           AS [Industry Name],
           osm6.LocalizedLabel           AS [Purchase Time Frame],
           Convert(date,createdon)       AS [Created On],
           companyname                   AS [Company Name],
           lastname                      AS [Last Name],
           firstname                     AS [First Name],
           emailaddress1                 AS [Email],
           address1_city                 AS [City],
           address1_country              AS [Country]
  FROM     dbo.lead LEFT OUTER JOIN dbo.StatusMetadata osm1 ON lead.statuscode=osm1.[Status] AND osm1.IsUserLocalizedLabel=0 AND osm1.LocalizedLabelLanguageCode=1033 AND osm1.EntityName='lead' COLLATE Latin1_General_100_CI_AS
                    LEFT OUTER JOIN dbo.OptionSetMetadata osm2 ON lead.leadqualitycode=osm2.[Option] AND osm2.OptionSetName='leadqualitycode' COLLATE Latin1_General_100_CI_AS AND osm2.IsUserLocalizedLabel=0 AND osm2.LocalizedLabelLanguageCode=1033 AND osm2.EntityName='lead' COLLATE Latin1_General_100_CI_AS
                    LEFT OUTER JOIN dbo.StateMetadata osm3 ON lead.statecode=osm3.[State] AND osm3.IsUserLocalizedLabel=0 AND osm3.LocalizedLabelLanguageCode=1033 AND osm3.EntityName='lead' COLLATE Latin1_General_100_CI_AS
                    LEFT OUTER JOIN dbo.OptionSetMetadata osm4 ON lead.leadsourcecode=osm4.[Option] AND osm4.OptionSetName='leadsourcecode' COLLATE Latin1_General_100_CI_AS AND osm4.IsUserLocalizedLabel=0 AND osm4.LocalizedLabelLanguageCode=1033 AND osm4.EntityName='lead' COLLATE Latin1_General_100_CI_AS
                    LEFT OUTER JOIN dbo.OptionSetMetadata osm5 ON lead.industrycode=osm5.[Option] AND osm5.OptionSetName='industrycode'  COLLATE Latin1_General_100_CI_AS AND osm5.IsUserLocalizedLabel=0 AND osm5.LocalizedLabelLanguageCode=1033 AND osm5.EntityName='lead' COLLATE Latin1_General_100_CI_AS
                    LEFT OUTER JOIN dbo.GlobalOptionSetMetadata osm6 ON lead.purchasetimeframe=osm6.[Option] AND osm6.OptionSetName='purchasetimeframe'  COLLATE Latin1_General_100_CI_AS AND osm6.IsUserLocalizedLabel=0 AND osm6.LocalizedLabelLanguageCode=1033;
go

-- TeamView
CREATE VIEW smgt.teamview
AS
  SELECT id      AS [Team Id],
         NAME    AS [Team Name]
  FROM   dbo.Team;
go


-- OwnerView
CREATE VIEW smgt.ownerview
AS
  SELECT id         AS [Owner Id],
         [Name]			AS [Owner Name],
         'Team'			AS [Owner Type]
  FROM dbo.Team
  UNION
  SELECT systemuserid AS [Owner Id],
         fullname     AS [Owner Name],
         'User'		  AS [Owner Type]
  FROM dbo.systemuser;
go

-- MeasuresView
CREATE VIEW smgt.measuresview
AS
    SELECT TOP 0 1 AS MeasureValues;
go


-- OpportunityProductView
CREATE VIEW smgt.opportunityproductview
AS
    SELECT CONVERT(UNIQUEIDENTIFIER, productid) AS [Product Id],
            opportunityid                        AS [Opportunity Id],
            baseamount_base                      AS [Revenue]
    FROM   dbo.opportunityproduct;
go


-- OpportunityView
CREATE VIEW [smgt].[opportunityview]
AS
    SELECT  o.opportunityid                     AS [Opportunity Id],
            o.NAME                              AS [Opportunity Name],
            o.ownerid                           AS [Owner Id],
            CONVERT(DATE, o.createdon)          AS [Created Date],
            CONVERT(DATE, o.actualclosedate)    AS [Actual Close Date],
            CONVERT(DATE, o.estimatedclosedate) AS [Estimated Close Date],
            o.closeprobability                  AS [Close Probability],
            CASE
                WHEN o.parentaccountid IS NULL THEN o.opportunityid
                ELSE o.parentaccountid
            END                                 AS [Account Id],
            o.actualvalue                       AS [Actual Value],
            o.estimatedvalue                    AS [Estimated Value],
            o.estimatedvalue* o.closeprobability/100.0
                                                AS [Expected Value],
            osm1.LocalizedLabel                 AS [Status],
            CASE
                WHEN stepname IS NULL OR Charindex('-', o.stepname) = 0 THEN NULL
                ELSE LEFT(o.stepname, Charindex('-', o.stepname) - 1)
            END                                 AS [Sales Stage Code],
            o.stepname                          AS [Sales Stage],
            osm2.LocalizedLabel                 AS [State],
            o.originatingleadid                 AS [Lead Id],
            osm3.LocalizedLabel                 AS [Opportunity Rating Name],
            NULL                                AS [Source]
    FROM   dbo.opportunity o LEFT OUTER JOIN dbo.StatusMetadata osm1 ON o.statuscode=osm1.[Status] AND osm1.IsUserLocalizedLabel=0 AND osm1.LocalizedLabelLanguageCode=1033 AND osm1.EntityName='opportunity' COLLATE Latin1_General_100_CI_AS
                             LEFT OUTER JOIN dbo.StateMetadata osm2 ON o.statecode=osm2.[State] AND osm2.IsUserLocalizedLabel=0 AND osm2.LocalizedLabelLanguageCode=1033 AND osm2.EntityName='opportunity' COLLATE Latin1_General_100_CI_AS
                             LEFT OUTER JOIN dbo.OptionSetMetadata osm3 ON o.opportunityratingcode=osm3.[Option] AND osm3.OptionSetName='opportunityratingcode'  COLLATE Latin1_General_100_CI_AS AND osm3.IsUserLocalizedLabel=0 AND osm3.LocalizedLabelLanguageCode=1033 AND osm3.EntityName='opportunity' COLLATE Latin1_General_100_CI_AS;
go


-- ProductView
CREATE VIEW smgt.productview
AS
    WITH tree AS
    (
        SELECT parentproductid,
               parentproductidname,
               productid,
               NAME,
               0                               AS [Level],
               Cast(productid AS VARCHAR(max)) AS pth
        FROM   dbo.product
        WHERE  parentproductid IS NULL
        UNION ALL
        SELECT a.parentproductid,
                a.parentproductidname,
                a.productid,
                a.NAME,
                t.[level] + 1                             AS [Level],
                t.pth + Cast(a.productid AS VARCHAR(max)) AS pth
        FROM   tree AS t INNER JOIN dbo.product AS a ON a.parentproductid = t.productid
    )
    SELECT hierarchy.productid     AS [Product Id],
           hierarchy.NAME          AS [Product Name],
           [level],
           CONVERT(VARCHAR, b.NAME) AS [Product Level 1],
           CONVERT(VARCHAR, c.NAME) AS [Product Level 2],
           CONVERT(VARCHAR, d.NAME) AS [Product Level 3]
    FROM   (SELECT productid,
                   NAME,
                   [level],
                   CONVERT(UNIQUEIDENTIFIER, NULLIF(Substring(pth, 1, 36), ''))  AS Level1,
                   CONVERT(UNIQUEIDENTIFIER, NULLIF(Substring(pth, 37, 36), '')) AS Level2,
                   CONVERT(UNIQUEIDENTIFIER, NULLIF(Substring(pth, 73, 36), '')) AS Level3
            FROM   tree) AS hierarchy LEFT OUTER JOIN dbo.product AS b ON b.productid = level1
                                      LEFT OUTER JOIN dbo.product AS c ON c.productid = level2
                                      LEFT OUTER JOIN dbo.product AS d ON d.productid = level3;
go


-- TempUserView
CREATE VIEW smgt.tempuserview
AS
    SELECT a.fullname,
            CONVERT(UNIQUEIDENTIFIER, a.systemuserid)       AS systemuserid,
            CONVERT(UNIQUEIDENTIFIER, a.parentsystemuserid) AS parentsystemuserid,
            a.hierarchylevel,
            systemuser_1.fullname                           AS managername
    FROM   (
                SELECT dbo.systemuser.fullname,
                    dbo.systemuser.systemuserid,
                    dbo.systemusermanagermap.parentsystemuserid,
                    dbo.systemusermanagermap.hierarchylevel
                FROM   dbo.systemusermanagermap LEFT OUTER JOIN dbo.systemuser ON dbo.systemusermanagermap.systemuserid = dbo.systemuser.systemuserid
            ) AS a
            LEFT OUTER JOIN dbo.systemuser AS systemuser_1 ON a.parentsystemuserid = systemuser_1.systemuserid
    WHERE  a.hierarchylevel = 1 AND
            systemuser_1.isdisabled = 0;
go

-- TerritoryView
CREATE VIEW smgt.territoryview
AS
    SELECT [NAME]      AS [Territory Name],
           territoryid AS [Territory Id]
    FROM   dbo.territory;
go

-- UserAscendantsView
CREATE VIEW smgt.userascendantsview
AS
    WITH mycte(systemuserid, emailaddress, ascendantsystemuserid, ascendantemailaddress, employeelevel) AS
    (
            -- Anchor
            SELECT  u.systemuserid,
                    u.internalemailaddress AS emailaddress,
                    u.systemuserid         AS ascendantsystemuserid,
                    u.internalemailaddress AS ascendantemailaddress,
                    0                      AS employeelevel
            FROM   dbo.systemuser u
            WHERE  u.internalemailaddress IS NOT NULL AND u.isdisabled = 0
            UNION ALL
            -- ...and the recursive part
            SELECT  c.systemuserid,
                    c.emailaddress,
                    u.parentsystemuserid                           AS ascendantsystemuserid,
                    (SELECT u1.internalemailaddress
                     FROM   systemuser u1
                     WHERE  u.parentsystemuserid = u1.systemuserid) AS ascendantemailaddress,
                    c.employeelevel + 1                             AS employeelevel
            FROM   mycte c INNER JOIN dbo.systemuser u ON c.ascendantsystemuserid = u.systemuserid
            WHERE  u.parentsystemuserid IS NOT NULL AND u.isdisabled=0
    )
    SELECT  mycte.systemuserid          [User Id],
            mycte.emailaddress          [Email],
            mycte.ascendantsystemuserid [Ascendant User Id],
            mycte.ascendantemailaddress [Ascendant Email],
            mycte.employeelevel         [Employee Level],
            um.domainuser               [Ascendant Domain User]
    FROM   mycte LEFT OUTER JOIN smgt.usermapping um ON mycte.ascendantsystemuserid = um.userid;
go


-- UserView
CREATE VIEW smgt.userview
AS
    SELECT  fullname           AS [Full Name],
            systemuserid       AS [User Id],
            parentsystemuserid AS [Parent User Id],
            hierarchylevel     AS [Hierarchy Level],
            managername        AS [Manager Name]
    FROM   smgt.tempuserview
    UNION ALL
    SELECT  b.fullname                             AS [Full Name],
            b.systemuserid                         AS [User Id],
            '00000000-0000-0000-0000-000000000000' AS [Parent User Id],
            1                                      AS [Hierarchy Level],
            'Root'                                 AS [Manager Name]
    FROM   (
               SELECT DISTINCT fullname, systemuserid
               FROM   dbo.systemuser
               WHERE  isdisabled = 0 AND
                      systemuserid NOT IN (SELECT DISTINCT systemuserid FROM smgt.tempuserview)
           ) AS b
    UNION ALL
    SELECT  'Root'                                 AS [Full Name],
            '00000000-0000-0000-0000-000000000000' AS [User Id],
            '00000000-0000-0000-0000-000000000000' AS [Parent User Id],
            1                                      AS [Hierarchy Level],
            'Root'                                 AS [Manager Name];
go

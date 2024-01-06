CREATE TABLE [dbo].[AuditLog] (
    [Id] UNIQUEIDENTIFIER NOT NULL,
--     [Type] NVARCHAR(1024) NULL,
--     [Name] NVARCHAR(255) NULL,
--     [Description] NVARCHAR(1024) NULL,
--     [Enabled] bit NOT NULL default 1,
--     [JsonState] NVARCHAR(MAX) NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
);
GO

-- Document Service Template Schema
-- Work Item #2297: Document Service: Template Service and Variable Substitution

-- DocumentTemplates table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'docs.DocumentTemplates'))
BEGIN
    CREATE TABLE docs.DocumentTemplates (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TenantId INT NOT NULL REFERENCES docs.Tenants(Id),
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(2000) NULL,
        Content NVARCHAR(MAX) NOT NULL,
        TemplateType NVARCHAR(50) NOT NULL, -- Document, Email, Report, etc.
        Category NVARCHAR(100) NULL,
        Variables NVARCHAR(MAX) NULL, -- JSON array of variable definitions
        IsPublic BIT NOT NULL DEFAULT 0, -- If true, shared across tenant hierarchy
        Version INT NOT NULL DEFAULT 1,
        IsCurrentVersion BIT NOT NULL DEFAULT 1,
        ParentTemplateId INT NULL, -- For versioning
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy NVARCHAR(200) NULL,
        UpdatedAt DATETIME2 NULL,
        UpdatedBy NVARCHAR(200) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAt DATETIME2 NULL,
        DeletedBy NVARCHAR(200) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_DocumentTemplates_ParentTemplate FOREIGN KEY (ParentTemplateId) REFERENCES docs.DocumentTemplates(Id)
    );

    CREATE INDEX IX_DocumentTemplates_TenantId ON docs.DocumentTemplates(TenantId);
    CREATE INDEX IX_DocumentTemplates_IsDeleted ON docs.DocumentTemplates(IsDeleted);
    CREATE INDEX IX_DocumentTemplates_IsActive ON docs.DocumentTemplates(IsActive);
    CREATE INDEX IX_DocumentTemplates_TemplateType ON docs.DocumentTemplates(TemplateType);
    CREATE INDEX IX_DocumentTemplates_Name ON docs.DocumentTemplates(Name);
END

-- TemplateUsageLog table (tracks template usage for analytics)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'docs.TemplateUsageLog'))
BEGIN
    CREATE TABLE docs.TemplateUsageLog (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        TemplateId INT NOT NULL REFERENCES docs.DocumentTemplates(Id),
        TenantId INT NOT NULL REFERENCES docs.Tenants(Id),
        DocumentId BIGINT NULL REFERENCES docs.Documents(Id),
        VariablesUsed NVARCHAR(MAX) NULL, -- JSON object of variable values used
        UsedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UsedBy NVARCHAR(200) NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Success', -- Success, Failed, PartialSuccess
        ErrorMessage NVARCHAR(2000) NULL
    );

    CREATE INDEX IX_TemplateUsageLog_TemplateId ON docs.TemplateUsageLog(TemplateId);
    CREATE INDEX IX_TemplateUsageLog_TenantId ON docs.TemplateUsageLog(TenantId);
    CREATE INDEX IX_TemplateUsageLog_UsedAt ON docs.TemplateUsageLog(UsedAt);
END

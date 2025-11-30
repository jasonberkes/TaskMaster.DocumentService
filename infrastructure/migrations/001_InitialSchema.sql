-- Document Service Initial Schema
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'docs')
    EXEC('CREATE SCHEMA docs');

-- Tenants table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'docs.Tenants'))
BEGIN
    CREATE TABLE docs.Tenants (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ParentTenantId INT NULL,
        TenantType NVARCHAR(50) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Slug NVARCHAR(100) NOT NULL,
        Settings NVARCHAR(MAX) NULL,
        RetentionPolicies NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        IsActive BIT NOT NULL DEFAULT 1
    );
    CREATE UNIQUE INDEX IX_Tenants_Slug ON docs.Tenants(Slug);
END

-- DocumentTypes table  
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'docs.DocumentTypes'))
BEGIN
    CREATE TABLE docs.DocumentTypes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        DisplayName NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        MetadataSchema NVARCHAR(MAX) NULL,
        DefaultTags NVARCHAR(500) NULL,
        Icon NVARCHAR(50) NULL,
        IsContentIndexed BIT NOT NULL DEFAULT 1,
        HasExtensionTable BIT NOT NULL DEFAULT 0,
        ExtensionTableName NVARCHAR(100) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        IsActive BIT NOT NULL DEFAULT 1
    );
    CREATE UNIQUE INDEX IX_DocumentTypes_Name ON docs.DocumentTypes(Name);
END

-- Documents table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'docs.Documents'))
BEGIN
    CREATE TABLE docs.Documents (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        TenantId INT NOT NULL REFERENCES docs.Tenants(Id),
        DocumentTypeId INT NOT NULL REFERENCES docs.DocumentTypes(Id),
        Title NVARCHAR(500) NOT NULL,
        Description NVARCHAR(2000) NULL,
        BlobPath NVARCHAR(500) NOT NULL,
        ContentHash NVARCHAR(64) NULL,
        FileSizeBytes BIGINT NULL,
        MimeType NVARCHAR(100) NULL,
        OriginalFileName NVARCHAR(500) NULL,
        Metadata NVARCHAR(MAX) NULL,
        Tags NVARCHAR(MAX) NULL,
        MeilisearchId NVARCHAR(100) NULL,
        LastIndexedAt DATETIME2 NULL,
        ExtractedText NVARCHAR(MAX) NULL,
        Version INT NOT NULL DEFAULT 1,
        ParentDocumentId BIGINT NULL,
        IsCurrentVersion BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy NVARCHAR(200) NULL,
        UpdatedAt DATETIME2 NULL,
        UpdatedBy NVARCHAR(200) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAt DATETIME2 NULL,
        DeletedBy NVARCHAR(200) NULL,
        DeletedReason NVARCHAR(500) NULL,
        IsArchived BIT NOT NULL DEFAULT 0,
        ArchivedAt DATETIME2 NULL
    );
    CREATE INDEX IX_Documents_TenantId ON docs.Documents(TenantId);
    CREATE INDEX IX_Documents_IsDeleted ON docs.Documents(IsDeleted);
END

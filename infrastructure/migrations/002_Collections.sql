-- Collections table for organizing documents into publishable collections
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'docs.Collections'))
BEGIN
    CREATE TABLE docs.Collections (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        TenantId INT NOT NULL REFERENCES docs.Tenants(Id),
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(2000) NULL,
        Slug NVARCHAR(100) NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Draft',
        IsPublished BIT NOT NULL DEFAULT 0,
        PublishedAt DATETIME2 NULL,
        PublishedBy NVARCHAR(200) NULL,
        CoverImageUrl NVARCHAR(500) NULL,
        Metadata NVARCHAR(MAX) NULL,
        Tags NVARCHAR(MAX) NULL,
        SortOrder INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy NVARCHAR(200) NULL,
        UpdatedAt DATETIME2 NULL,
        UpdatedBy NVARCHAR(200) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAt DATETIME2 NULL,
        DeletedBy NVARCHAR(200) NULL
    );
    CREATE INDEX IX_Collections_TenantId ON docs.Collections(TenantId);
    CREATE INDEX IX_Collections_Status ON docs.Collections(Status);
    CREATE INDEX IX_Collections_IsPublished ON docs.Collections(IsPublished);
    CREATE INDEX IX_Collections_IsDeleted ON docs.Collections(IsDeleted);
    CREATE UNIQUE INDEX IX_Collections_TenantId_Slug ON docs.Collections(TenantId, Slug) WHERE IsDeleted = 0;
END

-- CollectionDocuments junction table for many-to-many relationship
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'docs.CollectionDocuments'))
BEGIN
    CREATE TABLE docs.CollectionDocuments (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        CollectionId BIGINT NOT NULL REFERENCES docs.Collections(Id),
        DocumentId BIGINT NOT NULL REFERENCES docs.Documents(Id),
        SortOrder INT NOT NULL DEFAULT 0,
        AddedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        AddedBy NVARCHAR(200) NULL,
        Notes NVARCHAR(1000) NULL
    );
    CREATE INDEX IX_CollectionDocuments_CollectionId ON docs.CollectionDocuments(CollectionId);
    CREATE INDEX IX_CollectionDocuments_DocumentId ON docs.CollectionDocuments(DocumentId);
    CREATE UNIQUE INDEX IX_CollectionDocuments_CollectionId_DocumentId ON docs.CollectionDocuments(CollectionId, DocumentId);
END

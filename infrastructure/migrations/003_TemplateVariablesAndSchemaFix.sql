-- Migration: Add TemplateVariables table and fix DocumentTemplates schema
-- Date: 2025-12-11
-- Description: Fixes schema mismatch between Entity and DB for templates

-- Create TemplateVariables table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'docs.TemplateVariables'))
BEGIN
    CREATE TABLE docs.TemplateVariables (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TemplateId INT NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        DisplayName NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        DataType NVARCHAR(50) NOT NULL DEFAULT 'string',
        DefaultValue NVARCHAR(1000) NULL,
        IsRequired BIT NOT NULL DEFAULT 0,
        ValidationPattern NVARCHAR(500) NULL,
        ValidationMessage NVARCHAR(500) NULL,
        SortOrder INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_TemplateVariables_DocumentTemplates FOREIGN KEY (TemplateId) 
            REFERENCES docs.DocumentTemplates(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_TemplateVariables_TemplateId ON docs.TemplateVariables(TemplateId);
    CREATE UNIQUE INDEX IX_TemplateVariables_Name_TemplateId ON docs.TemplateVariables(Name, TemplateId);
END

-- Add missing columns to DocumentTemplates (if they don't exist)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('docs.DocumentTemplates') AND name = 'Content')
    ALTER TABLE docs.DocumentTemplates ADD Content NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('docs.DocumentTemplates') AND name = 'MimeType')
    ALTER TABLE docs.DocumentTemplates ADD MimeType NVARCHAR(100) NOT NULL DEFAULT 'text/plain';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('docs.DocumentTemplates') AND name = 'FileExtension')
    ALTER TABLE docs.DocumentTemplates ADD FileExtension NVARCHAR(20) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('docs.DocumentTemplates') AND name = 'DefaultTitlePattern')
    ALTER TABLE docs.DocumentTemplates ADD DefaultTitlePattern NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('docs.DocumentTemplates') AND name = 'DefaultDescriptionPattern')
    ALTER TABLE docs.DocumentTemplates ADD DefaultDescriptionPattern NVARCHAR(1000) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('docs.DocumentTemplates') AND name = 'Metadata')
    ALTER TABLE docs.DocumentTemplates ADD Metadata NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('docs.DocumentTemplates') AND name = 'Tags')
    ALTER TABLE docs.DocumentTemplates ADD Tags NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('docs.DocumentTemplates') AND name = 'UpdatedBy')
    ALTER TABLE docs.DocumentTemplates ADD UpdatedBy NVARCHAR(200) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('docs.DocumentTemplates') AND name = 'IsDeleted')
    ALTER TABLE docs.DocumentTemplates ADD IsDeleted BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('docs.DocumentTemplates') AND name = 'DeletedAt')
    ALTER TABLE docs.DocumentTemplates ADD DeletedAt DATETIME2 NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('docs.DocumentTemplates') AND name = 'DeletedBy')
    ALTER TABLE docs.DocumentTemplates ADD DeletedBy NVARCHAR(200) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('docs.DocumentTemplates') AND name = 'DeletedReason')
    ALTER TABLE docs.DocumentTemplates ADD DeletedReason NVARCHAR(500) NULL;

-- Add indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocumentTemplates_IsDeleted' AND object_id = OBJECT_ID('docs.DocumentTemplates'))
    CREATE INDEX IX_DocumentTemplates_IsDeleted ON docs.DocumentTemplates(IsDeleted);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocumentTemplates_TenantId' AND object_id = OBJECT_ID('docs.DocumentTemplates'))
    CREATE INDEX IX_DocumentTemplates_TenantId ON docs.DocumentTemplates(TenantId);

PRINT 'Migration 003_TemplateVariablesAndSchemaFix completed successfully';

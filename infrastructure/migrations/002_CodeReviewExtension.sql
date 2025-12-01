-- Code Review Extension Table Migration
-- This migration adds support for storing code review metadata from TaskMaster.Platform

-- CodeReviews extension table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'docs.CodeReviews'))
BEGIN
    CREATE TABLE docs.CodeReviews (
        DocumentId BIGINT PRIMARY KEY REFERENCES docs.Documents(Id) ON DELETE CASCADE,
        PullRequestNumber INT NULL,
        RepositoryName NVARCHAR(200) NULL,
        BranchName NVARCHAR(200) NULL,
        BaseBranchName NVARCHAR(200) NULL,
        Author NVARCHAR(200) NULL,
        Reviewers NVARCHAR(2000) NULL,
        Status NVARCHAR(50) NULL,
        PullRequestUrl NVARCHAR(500) NULL,
        CommitSha NVARCHAR(64) NULL,
        SubmittedAt DATETIME2 NULL,
        ApprovedAt DATETIME2 NULL,
        ApprovedBy NVARCHAR(200) NULL,
        MergedAt DATETIME2 NULL,
        MergedBy NVARCHAR(200) NULL,
        FilesChanged INT NULL,
        LinesAdded INT NULL,
        LinesDeleted INT NULL,
        CommentCount INT NULL,
        SourceBlobPath NVARCHAR(500) NULL,
        MigratedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        MigrationBatchId NVARCHAR(100) NULL
    );

    CREATE INDEX IX_CodeReviews_PullRequestNumber ON docs.CodeReviews(PullRequestNumber);
    CREATE INDEX IX_CodeReviews_Status ON docs.CodeReviews(Status);
    CREATE INDEX IX_CodeReviews_MigrationBatchId ON docs.CodeReviews(MigrationBatchId);
END

-- Add 'CodeReview' document type if it doesn't exist
IF NOT EXISTS (SELECT * FROM docs.DocumentTypes WHERE Name = 'CodeReview')
BEGIN
    INSERT INTO docs.DocumentTypes (
        Name,
        DisplayName,
        Description,
        IsContentIndexed,
        HasExtensionTable,
        ExtensionTableName,
        IsActive,
        CreatedAt
    )
    VALUES (
        'CodeReview',
        'Code Review',
        'Code review documents migrated from TaskMaster.Platform containing pull request analysis and metadata',
        1,
        1,
        'CodeReviews',
        1,
        GETUTCDATE()
    );
END

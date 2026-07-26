IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'IdempotencyRecords')
BEGIN
    CREATE TABLE IdempotencyRecords (
        IdempotencyKey NVARCHAR(255) NOT NULL PRIMARY KEY,
        RequestFingerprint NVARCHAR(64) NOT NULL,
        Status TINYINT NOT NULL,
        ResponseStatusCode INT NULL,
        ResponseBody NVARCHAR(MAX) NULL,
        ResponseHeaders NVARCHAR(MAX) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL,
        ExpiresAt DATETIMEOFFSET NOT NULL
    );

    CREATE INDEX IX_IdempotencyRecords_ExpiresAt ON IdempotencyRecords (ExpiresAt);
END
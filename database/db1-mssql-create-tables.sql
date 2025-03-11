CREATE TABLE Organizations (
    OrganizationId UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    ParentOrganizationId UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2 DEFAULT (GETUTCDATE()),
    CONSTRAINT FK_Organizations_ParentOrganizationId FOREIGN KEY (ParentOrganizationId) 
        REFERENCES Organizations(OrganizationId)
);

CREATE TABLE Users (
    UserId UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    CreatedAt DATETIME2 DEFAULT (GETUTCDATE())
);

CREATE TABLE OrganizationUser (
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Role NVARCHAR(50) NOT NULL,
    JoinedAt DATETIME2 DEFAULT (GETUTCDATE()),
    PRIMARY KEY (OrganizationId, UserId),
    CONSTRAINT FK_OrganizationUser_OrganizationId FOREIGN KEY (OrganizationId) 
        REFERENCES Organizations(OrganizationId),
    CONSTRAINT FK_OrganizationUser_UserId FOREIGN KEY (UserId) 
        REFERENCES Users(UserId)
);

CREATE TABLE Transactions (
    TransactionId UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    TransactionType NVARCHAR(50) NOT NULL,
    Account NVARCHAR(50) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'PENDING',
    CreatedAt DATETIME2 DEFAULT (GETUTCDATE()),
    ProcessedAt DATETIME2 NULL,
    CONSTRAINT FK_Transactions_UserId FOREIGN KEY (UserId) 
        REFERENCES Users(UserId),
    CONSTRAINT FK_Transactions_OrganizationId FOREIGN KEY (OrganizationId) 
        REFERENCES Organizations(OrganizationId)
);
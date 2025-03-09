CREATE TABLE CustomerOrganizations (
    CustomerOrganizationId UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    ParentCustomerOrganizationId UNIQUEIDENTIFIER NULL FOREIGN KEY REFERENCES CustomerOrganizations(CustomerOrganizationId) ON DELETE SET NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);  

CREATE TABLE Users (
    UserId UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) UNIQUE NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE CustomerOrganizationUser (
    CustomerOrganizationId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Role NVARCHAR(50) NOT NULL,
    JoinedAt DATETIME DEFAULT GETDATE(),
    PRIMARY KEY (CustomerOrganizationId, UserId),
    FOREIGN KEY (CustomerOrganizationId) REFERENCES CustomerOrganizations(CustomerOrganizationId) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

CREATE TABLE Transactions (
    TransactionId UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    CustomerOrganizationId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES CustomerOrganizations(CustomerOrganizationId),
    TransactionType NVARCHAR(50) NOT NULL,
    Amount DECIMAL(18,2) NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'PENDING',
    CreatedAt DATETIME DEFAULT GETDATE(),
    ProcessedAt DATETIME NULL
);

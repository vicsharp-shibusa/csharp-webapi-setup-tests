CREATE UNIQUE INDEX idx_users_email ON Users (Email);

CREATE INDEX idx_organizations_parentorganizationid 
    ON organizations (ParentorganizationId);

CREATE INDEX idx_transactions_organizationid 
    ON Transactions (organizationId);
CREATE INDEX idx_transactions_userid 
    ON Transactions (UserId);
CREATE INDEX idx_transactions_status 
    ON Transactions (Status);

CREATE INDEX idx_organizationuser_userid 
    ON organizationUser (UserId);
CREATE INDEX idx_organizationuser_organizationid 
    ON organizationUser (organizationId);
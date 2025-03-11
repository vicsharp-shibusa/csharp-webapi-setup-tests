CREATE UNIQUE INDEX idx_users_email ON dbo.Users (Email);

CREATE INDEX idx_organizations_parentorganizationid 
    ON dbo.Organizations (ParentOrganizationId);

CREATE INDEX idx_transactions_organizationid 
    ON dbo.Transactions (OrganizationId);
CREATE INDEX idx_transactions_userid 
    ON dbo.Transactions (UserId);
CREATE INDEX idx_transactions_status 
    ON dbo.Transactions (Status);

CREATE INDEX idx_organizationuser_userid 
    ON dbo.OrganizationUser (UserId);
CREATE INDEX idx_organizationuser_organizationid 
    ON dbo.OrganizationUser (OrganizationId);
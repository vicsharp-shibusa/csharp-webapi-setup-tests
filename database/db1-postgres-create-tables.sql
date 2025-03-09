CREATE TABLE organizations (
    organizationid UUID PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    parentorganizationid UUID NULL REFERENCES organizations(organizationid),
    createdat TIMESTAMP DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'UTC')
);

CREATE TABLE users (
    userid UUID PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    createdat TIMESTAMP DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'UTC')
);

CREATE TABLE organizationuser (
    organizationid UUID NOT NULL,
    userid UUID NOT NULL,
    role VARCHAR(50) NOT NULL,
    joinedat TIMESTAMP DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'UTC'),
    PRIMARY KEY (organizationid, userid),
    FOREIGN KEY (organizationid) REFERENCES organizations(organizationid),
    FOREIGN KEY (userid) REFERENCES users(userid)
);

CREATE TABLE transactions (
    transactionid UUID PRIMARY KEY,
    userid UUID NOT NULL,
    organizationid UUID NOT NULL,
    transactiontype VARCHAR(50) NOT NULL,
    account VARCHAR(50) NOT NULL,
    amount DECIMAL(18,2) NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    createdat TIMESTAMP DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'UTC'),
    processedat TIMESTAMP NULL,
    FOREIGN KEY (userid) REFERENCES users(userid),
    FOREIGN KEY (organizationid) REFERENCES organizations(organizationid)
);

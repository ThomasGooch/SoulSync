-- Create SoulSync Database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SoulSyncDb')
BEGIN
    CREATE DATABASE SoulSyncDb;
END
GO

USE SoulSyncDb;
GO

-- Create Users table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Email NVARCHAR(255) NOT NULL UNIQUE,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        Bio NVARCHAR(1000) NULL,
        DateOfBirth DATETIME2 NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        LastModifiedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    
    CREATE INDEX IX_Users_Email ON Users(Email);
END
GO

-- Create UserProfiles table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserProfiles')
BEGIN
    CREATE TABLE UserProfiles (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        UserId UNIQUEIDENTIFIER NOT NULL UNIQUE,
        Interests NVARCHAR(500) NULL,
        Location NVARCHAR(200) NULL,
        Occupation NVARCHAR(200) NULL,
        AIInsights NVARCHAR(2000) NULL,
        GenderIdentity NVARCHAR(50) NULL,
        InterestedInGenders NVARCHAR(MAX) NULL,
        MinAgePreference INT NULL,
        MaxAgePreference INT NULL,
        MaxDistancePreference INT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        LastModifiedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_UserProfiles_UserId ON UserProfiles(UserId);
END
GO

-- Seed Test Users
PRINT 'Seeding test users...';

-- User 1: Alex Johnson (25, Software Engineer)
DECLARE @alexId UNIQUEIDENTIFIER = NEWID();
INSERT INTO Users (Id, Email, FirstName, LastName, Bio, DateOfBirth, CreatedAt, LastModifiedAt)
VALUES (
    @alexId,
    'alex.johnson@soulsync.demo',
    'Alex',
    'Johnson',
    'Software engineer passionate about AI, hiking, and cooking. Looking for someone to explore the city with and share adventures.',
    DATEADD(YEAR, -25, GETDATE()),
    GETUTCDATE(),
    GETUTCDATE()
);

INSERT INTO UserProfiles (Id, UserId, Interests, Location, Occupation, AIInsights, GenderIdentity, InterestedInGenders, MinAgePreference, MaxAgePreference, MaxDistancePreference, CreatedAt, LastModifiedAt)
VALUES (
    NEWID(),
    @alexId,
    'AI, hiking, cooking, technology, music festivals',
    'San Francisco, CA',
    'Software Engineer',
    'Alex demonstrates strong intellectual curiosity and values meaningful connections. Profile suggests openness to new experiences and a balanced lifestyle.',
    'Male',
    '["Female","NonBinary"]',
    23,
    32,
    25,
    GETUTCDATE(),
    GETUTCDATE()
);

-- User 2: Sam Rivera (28, UX Designer)
DECLARE @samId UNIQUEIDENTIFIER = NEWID();
INSERT INTO Users (Id, Email, FirstName, LastName, Bio, DateOfBirth, CreatedAt, LastModifiedAt)
VALUES (
    @samId,
    'sam.rivera@soulsync.demo',
    'Sam',
    'Rivera',
    'UX designer who loves art galleries, yoga, and good coffee. Seeking genuine connections and meaningful conversations.',
    DATEADD(YEAR, -28, GETDATE()),
    GETUTCDATE(),
    GETUTCDATE()
);

INSERT INTO UserProfiles (Id, UserId, Interests, Location, Occupation, AIInsights, GenderIdentity, InterestedInGenders, MinAgePreference, MaxAgePreference, MaxDistancePreference, CreatedAt, LastModifiedAt)
VALUES (
    NEWID(),
    @samId,
    'art, yoga, coffee, design, photography',
    'San Francisco, CA',
    'UX Designer',
    'Sam shows creative thinking and mindfulness. Profile indicates someone who values aesthetics and emotional intelligence.',
    'Female',
    '["Male","Female"]',
    25,
    35,
    30,
    GETUTCDATE(),
    GETUTCDATE()
);

-- User 3: Jordan Chen (27, Data Scientist)
DECLARE @jordanId UNIQUEIDENTIFIER = NEWID();
INSERT INTO Users (Id, Email, FirstName, LastName, Bio, DateOfBirth, CreatedAt, LastModifiedAt)
VALUES (
    @jordanId,
    'jordan.chen@soulsync.demo',
    'Jordan',
    'Chen',
    'Data scientist by day, rock climber by weekend. Love analyzing patterns in everything except relationships!',
    DATEADD(YEAR, -27, GETDATE()),
    GETUTCDATE(),
    GETUTCDATE()
);

INSERT INTO UserProfiles (Id, UserId, Interests, Location, Occupation, AIInsights, GenderIdentity, InterestedInGenders, MinAgePreference, MaxAgePreference, MaxDistancePreference, CreatedAt, LastModifiedAt)
VALUES (
    NEWID(),
    @jordanId,
    'rock climbing, data science, board games, sci-fi',
    'San Francisco, CA',
    'Data Scientist',
    'Jordan exhibits analytical thinking balanced with adventurous spirit. Profile suggests someone who values both mental and physical challenges.',
    'NonBinary',
    '["Male","Female","NonBinary"]',
    24,
    32,
    20,
    GETUTCDATE(),
    GETUTCDATE()
);

-- User 4: Taylor Smith (30, Marketing Manager)
DECLARE @taylorId UNIQUEIDENTIFIER = NEWID();
INSERT INTO Users (Id, Email, FirstName, LastName, Bio, DateOfBirth, CreatedAt, LastModifiedAt)
VALUES (
    @taylorId,
    'taylor.smith@soulsync.demo',
    'Taylor',
    'Smith',
    'Marketing creative with a passion for travel and storytelling. Recently moved to the Bay Area and excited to meet new people!',
    DATEADD(YEAR, -30, GETDATE()),
    GETUTCDATE(),
    GETUTCDATE()
);

INSERT INTO UserProfiles (Id, UserId, Interests, Location, Occupation, AIInsights, GenderIdentity, InterestedInGenders, MinAgePreference, MaxAgePreference, MaxDistancePreference, CreatedAt, LastModifiedAt)
VALUES (
    NEWID(),
    @taylorId,
    'travel, writing, photography, wine tasting, concerts',
    'San Francisco, CA',
    'Marketing Manager',
    'Taylor demonstrates strong communication skills and cultural awareness. Profile indicates social butterfly personality with depth.',
    'Female',
    '["Male"]',
    28,
    38,
    35,
    GETUTCDATE(),
    GETUTCDATE()
);

-- User 5: Casey Morgan (26, Teacher)
DECLARE @caseyId UNIQUEIDENTIFIER = NEWID();
INSERT INTO Users (Id, Email, FirstName, LastName, Bio, DateOfBirth, CreatedAt, LastModifiedAt)
VALUES (
    @caseyId,
    'casey.morgan@soulsync.demo',
    'Casey',
    'Morgan',
    'Elementary school teacher who believes in kindness and making a difference. Love reading, baking, and weekend hikes.',
    DATEADD(YEAR, -26, GETDATE()),
    GETUTCDATE(),
    GETUTCDATE()
);

INSERT INTO UserProfiles (Id, UserId, Interests, Location, Occupation, AIInsights, GenderIdentity, InterestedInGenders, MinAgePreference, MaxAgePreference, MaxDistancePreference, CreatedAt, LastModifiedAt)
VALUES (
    NEWID(),
    @caseyId,
    'reading, baking, hiking, education, volunteering',
    'San Francisco, CA',
    'Teacher',
    'Casey shows empathy and nurturing qualities. Profile suggests someone who values personal growth and meaningful impact.',
    'Male',
    '["Female","NonBinary"]',
    24,
    30,
    25,
    GETUTCDATE(),
    GETUTCDATE()
);

PRINT 'Database initialization completed successfully!';
PRINT 'Seeded 5 test users with complete profiles.';
GO

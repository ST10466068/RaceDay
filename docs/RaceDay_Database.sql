-- ===================================================
-- RaceDay Database Script
-- PROG6212 POE - Part 1
-- Student: ST10466068
-- ===================================================
-- This script creates the RaceDay database and all
-- tables needed for the system, then adds some sample
-- data so the database isn't empty when it's marked.

-- Drop the database first if it already exists, so the
-- script can be run again on a clean instance without errors
IF DB_ID('RaceDayDB') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDB;
END
GO

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

-- ---------------------------------------------------
-- Roles table
-- Stores the two roles in the system: Organiser and Participant
-- ---------------------------------------------------
CREATE TABLE Roles (
    RoleId      INT             IDENTITY(1,1)   NOT NULL,
    RoleName    VARCHAR(20)     NOT NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (RoleId),
    CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName)
);
GO

-- ---------------------------------------------------
-- Users table
-- Every user (organiser or participant) is stored here.
-- The RoleId decides which type of user they are.
-- ---------------------------------------------------
CREATE TABLE Users (
    UserId          INT             IDENTITY(1,1)   NOT NULL,
    RoleId          INT             NOT NULL,
    FirstName       VARCHAR(50)     NOT NULL,
    LastName        VARCHAR(50)     NOT NULL,
    Email           VARCHAR(100)    NOT NULL,
    PasswordHash    VARCHAR(255)    NOT NULL,
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);
GO

-- ---------------------------------------------------
-- Events table
-- An event is created by an organiser (e.g. Comrades Marathon).
-- ---------------------------------------------------
CREATE TABLE Events (
    EventId         INT             IDENTITY(1,1)   NOT NULL,
    OrganiserId     INT             NOT NULL,
    Name            VARCHAR(100)    NOT NULL,
    Description     VARCHAR(500)    NULL,
    EventDate       DATETIME        NOT NULL,
    Location        VARCHAR(150)    NOT NULL,
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Events PRIMARY KEY (EventId),
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserId) REFERENCES Users(UserId)
);
GO

-- ---------------------------------------------------
-- Categories table
-- Each event can have more than one category, e.g. 5km, 10km, 21km.
-- ---------------------------------------------------
CREATE TABLE Categories (
    CategoryId      INT             IDENTITY(1,1)   NOT NULL,
    EventId         INT             NOT NULL,
    Name            VARCHAR(50)     NOT NULL,
    DistanceKm      DECIMAL(5,2)    NOT NULL,
    Fee             DECIMAL(8,2)    NOT NULL DEFAULT 0,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId) REFERENCES Events(EventId)
);
GO

-- ---------------------------------------------------
-- Enrolments table
-- Links a participant to the category they entered.
-- A participant can't enrol twice in the same category
-- (see the unique constraint below).
-- ---------------------------------------------------
CREATE TABLE Enrolments (
    EnrolmentId     INT             IDENTITY(1,1)   NOT NULL,
    CategoryId      INT             NOT NULL,
    ParticipantId   INT             NOT NULL,
    EnrolmentDate   DATETIME        NOT NULL DEFAULT GETDATE(),
    Status          VARCHAR(20)     NOT NULL DEFAULT 'Confirmed',
    CONSTRAINT PK_Enrolments PRIMARY KEY (EnrolmentId),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId),
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantId) REFERENCES Users(UserId),
    CONSTRAINT UQ_Enrolments_Category_Participant UNIQUE (CategoryId, ParticipantId)
);
GO

-- ---------------------------------------------------
-- Results table
-- Stores the finish time and position for a completed enrolment.
-- Only one result per enrolment is allowed.
-- ---------------------------------------------------
CREATE TABLE Results (
    ResultId        INT             IDENTITY(1,1)   NOT NULL,
    EnrolmentId     INT             NOT NULL,
    FinishTime      TIME            NOT NULL,
    Position        INT             NULL,
    CapturedAt      DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Results PRIMARY KEY (ResultId),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId) REFERENCES Enrolments(EnrolmentId),
    CONSTRAINT UQ_Results_Enrolment UNIQUE (EnrolmentId)
);
GO


-- ===================================================
-- SEED DATA
-- Adding some sample data so there's something to
-- look at and test the relationships with.
-- ===================================================

-- Roles
INSERT INTO Roles (RoleName) VALUES ('Organiser'), ('Participant');
GO

-- 2 organisers and 2 participants
INSERT INTO Users (RoleId, FirstName, LastName, Email, PasswordHash)
VALUES
    (1, 'Thandiwe', 'Nkosi',   'thandiwe.nkosi@raceday.co.za', 'HASHED_PASSWORD_1'),
    (1, 'Johan',    'Botha',   'johan.botha@raceday.co.za',    'HASHED_PASSWORD_2'),
    (2, 'Lerato',   'Mokoena', 'lerato.mokoena@example.com',   'HASHED_PASSWORD_3'),
    (2, 'David',    'Pillay',  'david.pillay@example.com',     'HASHED_PASSWORD_4');
GO

-- 3 events (Thandiwe organises two, Johan organises one)
INSERT INTO Events (OrganiserId, Name, Description, EventDate, Location)
VALUES
    (1, 'Comrades Marathon',    'Ultra-marathon between Pietermaritzburg and Durban.', '2027-06-13 05:30:00', 'Pietermaritzburg, KZN'),
    (1, 'Cape Town Cycle Tour', 'Road cycling tour around the Cape Peninsula.',        '2027-03-08 06:00:00', 'Cape Town, Western Cape'),
    (2, 'Soweto Marathon',      'Community road running event through Soweto.',       '2027-11-07 06:30:00', 'Soweto, Gauteng');
GO

-- Categories for each event
INSERT INTO Categories (EventId, Name, DistanceKm, Fee)
VALUES
    (1, 'Ultra Marathon',     89.00,  850.00),
    (2, '109km Cycle Race',   109.00, 650.00),
    (2, '55km Cycle Race',    55.00,  450.00),
    (3, '42km Marathon',      42.20,  350.00),
    (3, '21km Half Marathon', 21.10,  250.00),
    (3, '10km Fun Run',       10.00,  120.00);
GO

-- A few sample enrolments
INSERT INTO Enrolments (CategoryId, ParticipantId, Status)
VALUES
    (1, 3, 'Confirmed'), -- Lerato -> Comrades Ultra Marathon
    (2, 3, 'Confirmed'), -- Lerato -> Cape Town 109km Cycle Race
    (4, 4, 'Confirmed'), -- David  -> Soweto 42km Marathon
    (6, 4, 'Confirmed'); -- David  -> Soweto 10km Fun Run
GO

-- Sample results for two of the enrolments above
INSERT INTO Results (EnrolmentId, FinishTime, Position)
VALUES
    (1, '09:45:12', 1250),
    (4, '00:52:30', 340);
GO


-- ===================================================
-- Quick check queries - uncomment to run manually
-- and confirm everything loaded correctly
-- ===================================================
-- SELECT * FROM Roles;
-- SELECT * FROM Users;
-- SELECT * FROM Events;
-- SELECT * FROM Categories;
-- SELECT * FROM Enrolments;
-- SELECT * FROM Results;

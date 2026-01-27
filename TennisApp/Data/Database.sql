-- TennisApp Database Schema - Official Court Table Definition
-- This schema is designed for SQLite and can be safely used by other AI agents

-- Main Court table
CREATE TABLE Court (
    court_id      TEXT(2) PRIMARY KEY NOT NULL,          -- e.g. '01' .. '99'
    court_img     BLOB NULL,                             -- optional image bytes
    court_status  TEXT(1) NOT NULL DEFAULT '1',          -- '0' = maintenance, '1' = ready
    last_updated  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Index for performance
CREATE INDEX IX_Court_Status ON Court(court_status);
CREATE INDEX IX_Court_LastUpdated ON Court(last_updated);

-- ✅ No sample data - Database starts empty
-- Users can add their own courts through the UI

-- CRUD Templates for other AI agents:

-- Insert (Add court)
-- INSERT INTO Court (court_id, court_img, court_status, last_updated)
-- VALUES (:court_id, :court_img, :court_status, :last_updated);

-- Update (Edit court info)
-- UPDATE Court
-- SET
--     court_img    = :court_img,
--     court_status = :court_status,
--     last_updated = :last_updated
-- WHERE court_id = :court_id;

-- Delete (Remove court)
-- DELETE FROM Court WHERE court_id = :court_id;

-- Select (Get list of courts)
-- SELECT court_id, court_img, court_status, last_updated
-- FROM Court
-- ORDER BY court_id;

-- Clear all courts
-- DELETE FROM Court;

-- =============================================================================
-- Trainee table
-- =============================================================================
CREATE TABLE Trainee (
    trainee_id        TEXT(9) PRIMARY KEY NOT NULL,      -- e.g. '120250001' (1=type, 2025=year, 0001=number)
    trainee_fname     TEXT(50) NOT NULL,                 -- First name (required)
    trainee_lname     TEXT(50) NOT NULL,                 -- Last name (required)
    trainee_nickname  TEXT(50) NULL,                     -- Nickname (optional)
    trainee_birthdate DATETIME NULL,                     -- Birth date (optional)
    trainee_phone     TEXT(10) NULL,                     -- Phone number (optional, 10 digits)
    trainee_img       BLOB NULL                          -- Image data as BLOB (optional)
);

-- Index for performance
CREATE INDEX IX_Trainee_Name ON Trainee(trainee_fname, trainee_lname);
CREATE INDEX IX_Trainee_Phone ON Trainee(trainee_phone);

-- CRUD Templates for Trainee:

-- Insert (Add trainee)
-- INSERT INTO Trainee (trainee_id, trainee_fname, trainee_lname, trainee_nickname, trainee_birthdate, trainee_phone, trainee_img)
-- VALUES (:trainee_id, :trainee_fname, :trainee_lname, :trainee_nickname, :trainee_birthdate, :trainee_phone, :trainee_img);

-- Update (Edit trainee info)
-- UPDATE Trainee
-- SET
--     trainee_fname     = :trainee_fname,
--     trainee_lname     = :trainee_lname,
--     trainee_nickname  = :trainee_nickname,
--     trainee_birthdate = :trainee_birthdate,
--     trainee_phone     = :trainee_phone,
--     trainee_img       = :trainee_img
-- WHERE trainee_id = :trainee_id;

-- Delete (Remove trainee)
-- DELETE FROM Trainee WHERE trainee_id = :trainee_id;

-- Select (Get list of trainees)
-- SELECT trainee_id, trainee_fname, trainee_lname, trainee_nickname, trainee_birthdate, trainee_phone, trainee_img
-- FROM Trainee
-- ORDER BY trainee_id;

-- Select (Get trainee by ID)
-- SELECT trainee_id, trainee_fname, trainee_lname, trainee_nickname, trainee_birthdate, trainee_phone, trainee_img
-- FROM Trainee
-- WHERE trainee_id = :trainee_id;

-- Clear all trainees
-- DELETE FROM Trainee;

-- =============================================================================
-- Trainer table
-- =============================================================================
CREATE TABLE Trainer (
    trainer_id        TEXT(9) PRIMARY KEY NOT NULL,      -- e.g. '220250001' (2=type, 2025=year, 0001=number)
    trainer_fname     TEXT(50) NOT NULL,                 -- First name (required)
    trainer_lname     TEXT(50) NOT NULL,                 -- Last name (required)
    trainer_nickname  TEXT(50) NULL,                     -- Nickname (optional)
    trainer_birthdate DATETIME NULL,                     -- Birth date (optional)
    trainer_phone     TEXT(10) NULL,                     -- Phone number (optional, 10 digits)
    Trainer_img       BLOB NULL                          -- Image data as BLOB (optional)
);

-- Index for performance
CREATE INDEX IX_Trainer_Name ON Trainer(trainer_fname, trainer_lname);
CREATE INDEX IX_Trainer_Phone ON Trainer(trainer_phone);

-- CRUD Templates for Trainer:

-- Insert (Add trainer)
-- INSERT INTO Trainer (trainer_id, trainer_fname, trainer_lname, trainer_nickname, trainer_birthdate, trainer_phone, Trainer_img)
-- VALUES (:trainer_id, :trainer_fname, :trainer_lname, :trainer_nickname, :trainer_birthdate, :trainer_phone, :trainer_img);

-- Update (Edit trainer info)
-- UPDATE Trainer
-- SET
--     trainer_fname     = :trainer_fname,
--     trainer_lname     = :trainer_lname,
--     trainer_nickname  = :trainer_nickname,
--     trainer_birthdate = :trainer_birthdate,
--     trainer_phone     = :trainer_phone,
--     Trainer_img       = :trainer_img
-- WHERE trainer_id = :trainer_id;

-- Delete (Remove trainer)
-- DELETE FROM Trainer WHERE trainer_id = :trainer_id;

-- Select (Get list of trainers)
-- SELECT trainer_id, trainer_fname, trainer_lname, trainer_nickname, trainer_birthdate, trainer_phone, Trainer_img
-- FROM Trainer
-- ORDER BY trainer_id;

-- Select (Get trainer by ID)
-- SELECT trainer_id, trainer_fname, trainer_lname, trainer_nickname, trainer_birthdate, trainer_phone, Trainer_img
-- FROM Trainer
-- WHERE trainer_id = :trainer_id;

-- Clear all trainers
-- DELETE FROM Trainer;

-- =============================================================================
-- Course (Class) table
-- =============================================================================
CREATE TABLE Course (
    class_id           TEXT(4) PRIMARY KEY NOT NULL,     -- e.g. 'TA01', 'T104', 'P201'
    class_title        TEXT(50) NOT NULL,                -- Course name (Adult, Kids, etc.)
    class_time         INTEGER NOT NULL,                 -- Number of sessions (1, 4, 8, 12)
    class_duration     INTEGER NULL,                     -- Duration per session in hours (1, 2)
    class_rate         INTEGER NULL,                     -- Course fee (600, 2200, etc.)
    trainer_id         TEXT(9) NULL,                     -- FK to Trainer (optional)
    created_date       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_updated       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (trainer_id) REFERENCES Trainer(trainer_id) ON DELETE SET NULL
);

-- Index for performance
CREATE INDEX IX_Course_Title ON Course(class_title);
CREATE INDEX IX_Course_Trainer ON Course(trainer_id);

-- CRUD Templates for Course:

-- Insert (Add course)
-- INSERT INTO Course (class_id, class_title, class_time, class_duration, class_rate, trainer_id)
-- VALUES (:class_id, :class_title, :class_time, :class_duration, :class_rate, :trainer_id);

-- Update (Edit course info)
-- UPDATE Course
-- SET
--     class_title    = :class_title,
--     class_time     = :class_time,
--     class_duration = :class_duration,
--     class_rate     = :class_rate,
--     trainer_id     = :trainer_id,
--     last_updated   = CURRENT_TIMESTAMP
-- WHERE class_id = :class_id;

-- Delete (Remove course)
-- DELETE FROM Course WHERE class_id = :class_id;

-- Select (Get list of courses with trainer info)
-- SELECT c.class_id, c.class_title, c.class_time, c.class_duration, c.class_rate, c.trainer_id,
--        t.trainer_fname || ' ' || t.trainer_lname AS trainer_name
-- FROM Course c
-- LEFT JOIN Trainer t ON c.trainer_id = t.trainer_id
-- ORDER BY c.class_id;

-- Select (Get course by ID)
-- SELECT * FROM Course WHERE class_id = :class_id;

-- Clear all courses
-- DELETE FROM Course;

-- Course ID Format (4 digits):
-- Position 1-2: Class Type
--   TA = Adult Class
--   T1 = Kids Class
--   T2 = Intermediate Class
--   T3 = Competitive Class
--   P1 = Private & Master Coach
--   P2 = Private & Standard Coach (Day)
--   P3 = Private & Standard Coach (Night)
-- Position 3-4: Number of sessions (01, 04, 08, 12)
-- Example: T104 = Kids Class (T1) with 4 sessions (04)

-- =============================================================================
-- ClassRegisRecord (Course Registration) table
-- =============================================================================
CREATE TABLE ClassRegisRecord (
    trainee_id         TEXT(10) NOT NULL,                -- FK to Trainee
    class_id           TEXT(4) NOT NULL,                 -- FK to Course
    regis_date         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    PRIMARY KEY (trainee_id, class_id),
    FOREIGN KEY (trainee_id) REFERENCES Trainee(trainee_id) ON DELETE CASCADE,
    FOREIGN KEY (class_id) REFERENCES Course(class_id) ON DELETE CASCADE
);

-- Index for performance
CREATE INDEX IX_ClassRegisRecord_Trainee ON ClassRegisRecord(trainee_id);
CREATE INDEX IX_ClassRegisRecord_Class ON ClassRegisRecord(class_id);
CREATE INDEX IX_ClassRegisRecord_Date ON ClassRegisRecord(regis_date);

-- CRUD Templates for ClassRegisRecord:

-- Insert (Register trainee to course)
-- INSERT INTO ClassRegisRecord (trainee_id, class_id, regis_date)
-- VALUES (:trainee_id, :class_id, :regis_date);

-- Delete (Remove registration)
-- DELETE FROM ClassRegisRecord 
-- WHERE trainee_id = :trainee_id AND class_id = :class_id;

-- Select (Get all registrations with details)
-- SELECT 
--     r.trainee_id,
--     r.class_id,
--     r.regis_date,
--     t.trainee_fname || ' ' || t.trainee_lname AS trainee_name,
--     t.trainee_phone,
--     c.class_title,
--     c.class_time,
--     c.class_rate
-- FROM ClassRegisRecord r
-- INNER JOIN Trainee t ON r.trainee_id = t.trainee_id
-- INNER JOIN Course c ON r.class_id = c.class_id
-- ORDER BY r.regis_date DESC;

-- Select (Get trainee's registrations)
-- SELECT r.*, c.class_title, c.class_time, c.class_rate
-- FROM ClassRegisRecord r
-- INNER JOIN Course c ON r.class_id = c.class_id
-- WHERE r.trainee_id = :trainee_id;

-- Select (Get course registrations)
-- SELECT r.*, t.trainee_fname, t.trainee_lname, t.trainee_phone
-- FROM ClassRegisRecord r
-- INNER JOIN Trainee t ON r.trainee_id = t.trainee_id
-- WHERE r.class_id = :class_id;

-- Check if registration exists
-- SELECT COUNT(*) FROM ClassRegisRecord
-- WHERE trainee_id = :trainee_id AND class_id = :class_id;

-- Clear all registrations
-- DELETE FROM ClassRegisRecord;

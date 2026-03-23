-- TennisApp Database Schema - Official Court Table Definition
-- This schema is designed for SQLite and can be safely used by other AI agents

-- Main Court table
CREATE TABLE Court (
    court_id         TEXT(2) PRIMARY KEY NOT NULL,          -- e.g. '01' .. '99'
    court_img        BLOB NULL,                             -- optional image bytes
    court_status     TEXT(1) NOT NULL DEFAULT '1',          -- '0' = maintenance, '1' = ready
    maintenance_date DATE NULL,                             -- วันที่ปรับปรุงสนามจริง (ผู้ใช้เลือก)
    last_updated     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP -- วันที่+เวลาที่กดบันทึกในระบบ (อัตโนมัติ)
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
    class_id              TEXT(4) PRIMARY KEY NOT NULL,  -- e.g. 'TA04', 'T108', 'PM01', 'PC01'
    class_title           TEXT(50) NOT NULL,             -- Course name (Adult, Kids, etc.)
    class_time            INTEGER NOT NULL,              -- Default session count (4, 8, 12, 16)
    class_duration        INTEGER NULL,                  -- Duration per session in hours
    class_rate            INTEGER NULL,                  -- Legacy: backward-compat (= class_rate_per_time)
    class_rate_per_time   INTEGER NULL,                  -- ราคาต่อครั้ง (TA=600, T1=600, T2=800, T3=900, PM=2500, PC=950)
    class_rate_4          INTEGER NULL,                  -- ราคา 4 ครั้ง
    class_rate_8          INTEGER NULL,                  -- ราคา 8 ครั้ง
    class_rate_12         INTEGER NULL,                  -- ราคา 12 ครั้ง
    class_rate_16         INTEGER NULL,                  -- ราคา 16 ครั้ง
    class_rate_monthly    INTEGER NULL,                  -- ราคารายเดือน (T3=13,000)
    class_rate_night      INTEGER NULL,                  -- ราคากลางคืน 18:00-21:00 (PC=1,050)
    trainer_id            TEXT(9) NULL,                  -- FK to Trainer (optional)
    created_date          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_updated          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY (trainer_id) REFERENCES Trainer(trainer_id) ON DELETE SET NULL
);

-- Index for performance
CREATE INDEX IX_Course_Title   ON Course(class_title);
CREATE INDEX IX_Course_Trainer ON Course(trainer_id);

-- Course ID Format (4 chars): XXYY
-- XX = Class Type:
--   TA = Adult Class                 (฿600/ครั้ง)
--   T1 = Red & Orange Ball           (฿600/ครั้ง)
--   T2 = Intermediate Class          (฿800/ครั้ง)
--   T3 = Competitive Class           (฿900/ครั้ง)
--   PM = Private Kru Mee             (฿2,500/ครั้ง)
--   PC = Private + Coach             (฿950 day / ฿1,050 night)
-- YY = Session count: 04, 08, 12, 16
-- Example: T108 = Red&Orange Ball, 8 sessions

-- Pricing reference (Talent Tennis Academy Fee & Tickets):
-- TA Adult:       4=2,200  8=4,000
-- T1 Red&Orange:  4=1,800  8=3,200  12=4,500
-- T2 Intermediate:4=3,000  8=4,800  12=6,600  16=8,000
-- T3 Competitive: 8=6,500  12=8,500 16=11,500 monthly=13,000
-- PM Kru Mee:     per_time=2,500
-- PC Coach:       per_time=950 (06:00-17:00)  night=1,050 (18:00-21:00)

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

-- =============================================================================
-- PaidCourtReservation (การจองสนามแบบเช่า - คิดเงิน)
-- =============================================================================
CREATE TABLE PaidCourtReservation (
    p_reserve_id       TEXT(10) PRIMARY KEY NOT NULL,    -- e.g. '2025041609' (YYYYMMDDXX)
    court_id           TEXT(2) NOT NULL,                 -- FK to Court ('00' = ยังไม่จัดสรร)
    p_request_date     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    p_reserve_date     DATE NOT NULL,                    -- วันที่ต้องการใช้สนาม
    p_reserve_time     TIME NOT NULL,                    -- เวลาเริ่มใช้สนาม
    p_reserve_duration REAL NOT NULL,                    -- ระยะเวลา (ชั่วโมง)
    p_reserve_name     TEXT(50) NOT NULL,                -- ชื่อผู้จอง
    p_reserve_phone    TEXT(10) NULL,                    -- เบอร์โทร (optional)
    p_status           TEXT(20) NOT NULL DEFAULT 'booked', -- booked/in_use/completed/cancelled
    p_actual_start     DATETIME NULL,                    -- เวลาเริ่มใช้งานจริง (กด Start)
    p_actual_end       DATETIME NULL,                    -- เวลาหยุดใช้งานจริง (กด Stop)
    p_actual_price     INTEGER NULL                      -- ค่าบริการจริง (กรอกตอน Stop)
);

-- Index for performance
CREATE INDEX IX_PaidCourtReservation_Court ON PaidCourtReservation(court_id);
CREATE INDEX IX_PaidCourtReservation_Date ON PaidCourtReservation(p_reserve_date);
CREATE INDEX IX_PaidCourtReservation_Request ON PaidCourtReservation(p_request_date);
CREATE INDEX IX_PaidCourtReservation_Status ON PaidCourtReservation(p_status);

-- CRUD Templates for PaidCourtReservation:

-- Insert (Add paid reservation)
-- INSERT INTO PaidCourtReservation (p_reserve_id, court_id, p_request_date, p_reserve_date, p_reserve_time, p_reserve_duration, p_reserve_name, p_reserve_phone)
-- VALUES (:p_reserve_id, :court_id, :p_request_date, :p_reserve_date, :p_reserve_time, :p_reserve_duration, :p_reserve_name, :p_reserve_phone);

-- Update (Edit paid reservation)
-- UPDATE PaidCourtReservation
-- SET
--     court_id           = :court_id,
--     p_reserve_date     = :p_reserve_date,
--     p_reserve_time     = :p_reserve_time,
--     p_reserve_duration = :p_reserve_duration,
--     p_reserve_name     = :p_reserve_name,
--     p_reserve_phone    = :p_reserve_phone
-- WHERE p_reserve_id = :p_reserve_id;

-- Delete (Remove paid reservation)
-- DELETE FROM PaidCourtReservation WHERE p_reserve_id = :p_reserve_id;

-- Select (Get all paid reservations with court info)
-- SELECT 
--     r.p_reserve_id,
--     r.court_id,
--     r.p_request_date,
--     r.p_reserve_date,
--     r.p_reserve_time,
--     r.p_reserve_duration,
--     r.p_reserve_name,
--     r.p_reserve_phone,
--     c.court_status
-- FROM PaidCourtReservation r
-- INNER JOIN Court c ON r.court_id = c.court_id
-- ORDER BY r.p_reserve_date DESC, r.p_reserve_time DESC;

-- Select (Get paid reservation by ID)
-- SELECT * FROM PaidCourtReservation WHERE p_reserve_id = :p_reserve_id;

-- Select (Get reservations by date)
-- SELECT * FROM PaidCourtReservation WHERE p_reserve_date = :p_reserve_date;

-- Select (Check if court is available at specific time)
-- SELECT COUNT(*) FROM PaidCourtReservation
-- WHERE court_id = :court_id
--   AND p_reserve_date = :p_reserve_date
--   AND time(p_reserve_time) < time(:end_time)
--   AND time(p_reserve_time, '+' || p_reserve_duration || ' hours') > time(:start_time);

-- Clear all paid reservations
-- DELETE FROM PaidCourtReservation;

-- Reservation ID Format (10 digits):
-- Position 1-4: Year (YYYY)
-- Position 5-6: Month (MM)
-- Position 7-8: Day (DD)
-- Position 9-10: Sequence number (01-99) of bookings made on that day
-- Example: 2025041609 = Booked on April 16, 2025, 9th booking of the day

-- =============================================================================
-- CourseCourtReservation (การจองสนามสำหรับคอร์ส - ไม่คิดเงิน)
-- =============================================================================
CREATE TABLE CourseCourtReservation (
    c_reserve_id       TEXT(10) PRIMARY KEY NOT NULL,    -- e.g. '2025041609' (YYYYMMDDXX)
    court_id           TEXT(2) NOT NULL,                 -- FK to Court ('00' = ยังไม่จัดสรร)
    class_id           TEXT(8) NOT NULL,                 -- FK to Course
    trainer_id         TEXT(9) NOT NULL DEFAULT '',       -- FK to Trainer
    c_request_date     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    c_reserve_date     DATE NOT NULL,                    -- วันที่ต้องการใช้สนาม
    c_reserve_time     TIME NOT NULL,                    -- เวลาเริ่มใช้สนาม
    c_reserve_duration REAL NOT NULL,                    -- ระยะเวลา (ชั่วโมง)
    c_reserve_name     TEXT(50) NOT NULL,                -- ชื่อผู้จอง/โค้ช
    c_reserve_phone    TEXT(10) NULL,                    -- เบอร์โทร (optional)
    c_status           TEXT(20) NOT NULL DEFAULT 'booked', -- booked/in_use/completed/cancelled
    c_actual_start     DATETIME NULL,                    -- เวลาเริ่มใช้งานจริง (กด Start)
    c_actual_end       DATETIME NULL                     -- เวลาหยุดใช้งานจริง (กด Stop)
);

-- Index for performance
CREATE INDEX IX_CourseCourtReservation_Court ON CourseCourtReservation(court_id);
CREATE INDEX IX_CourseCourtReservation_Class ON CourseCourtReservation(class_id);
CREATE INDEX IX_CourseCourtReservation_Trainer ON CourseCourtReservation(trainer_id);
CREATE INDEX IX_CourseCourtReservation_Date ON CourseCourtReservation(c_reserve_date);
CREATE INDEX IX_CourseCourtReservation_Request ON CourseCourtReservation(c_request_date);
CREATE INDEX IX_CourseCourtReservation_Status ON CourseCourtReservation(c_status);

-- CRUD Templates for CourseCourtReservation:

-- Insert (Add course reservation)
-- INSERT INTO CourseCourtReservation (c_reserve_id, court_id, class_id, trainer_id, c_request_date, c_reserve_date, c_reserve_time, c_reserve_name, c_reserve_phone)
-- VALUES (:c_reserve_id, :court_id, :class_id, :trainer_id, :c_request_date, :c_reserve_date, :c_reserve_time, :c_reserve_name, :c_reserve_phone);

-- Update (Edit course reservation)
-- UPDATE CourseCourtReservation
-- SET
--     court_id       = :court_id,
--     class_id       = :class_id,
--     trainer_id     = :trainer_id,
--     c_reserve_date = :c_reserve_date,
--     c_reserve_time = :c_reserve_time,
--     c_reserve_name = :c_reserve_name,
--     c_reserve_phone = :c_reserve_phone
-- WHERE c_reserve_id = :c_reserve_id;

-- Delete (Remove course reservation)
-- DELETE FROM CourseCourtReservation WHERE c_reserve_id = :c_reserve_id;

-- Select (Get all course reservations with details)
-- SELECT 
--     r.c_reserve_id,
--     r.court_id,
--     r.class_id,
--     r.c_request_date,
--     r.c_reserve_date,
--     r.c_reserve_time,
--     r.c_reserve_name,
--     r.c_reserve_phone,
--     c.court_status,
--     co.class_title,
--     co.class_duration
-- FROM CourseCourtReservation r
-- INNER JOIN Court c ON r.court_id = c.court_id
-- INNER JOIN Course co ON r.class_id = co.class_id
-- ORDER BY r.c_reserve_date DESC, r.c_reserve_time DESC;

-- Select (Get course reservation by ID)
-- SELECT * FROM CourseCourtReservation WHERE c_reserve_id = :c_reserve_id;

-- Select (Get reservations by course)
-- SELECT * FROM CourseCourtReservation WHERE class_id = :class_id;

-- Clear all course reservations
-- DELETE FROM CourseCourtReservation;

-- =============================================================================
-- PaidCourtUseLog (บันทึกการใช้สนามจริง - จากการเช่า)
-- =============================================================================
CREATE TABLE PaidCourtUseLog (
    p_log_id           TEXT(10) PRIMARY KEY NOT NULL,    -- Log ID (YYYYMMDDXX)
    p_reserve_id       TEXT(10) NOT NULL,                -- FK to PaidCourtReservation
    p_checkin_time     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, -- Actual check-in time
    p_log_duration     REAL NOT NULL,                    -- Actual duration used (hours)
    p_log_price        INTEGER NOT NULL,                 -- Price charged (Baht)
    p_log_status       TEXT(20) NOT NULL DEFAULT 'completed', -- Status: completed, cancelled, no-show
    
    FOREIGN KEY (p_reserve_id) REFERENCES PaidCourtReservation(p_reserve_id) ON DELETE CASCADE
);

-- Index for performance
CREATE INDEX IX_PaidCourtUseLog_Reserve ON PaidCourtUseLog(p_reserve_id);
CREATE INDEX IX_PaidCourtUseLog_CheckIn ON PaidCourtUseLog(p_checkin_time);

-- CRUD Templates for PaidCourtUseLog:

-- Insert (Add use log)
-- INSERT INTO PaidCourtUseLog (p_log_id, p_reserve_id, p_checkin_time, p_log_duration, p_log_price, p_log_status)
-- VALUES (:p_log_id, :p_reserve_id, :p_checkin_time, :p_log_duration, :p_log_price, :p_log_status);

-- Update (Edit use log)
-- UPDATE PaidCourtUseLog
-- SET
--     p_log_duration = :p_log_duration,
--     p_log_price    = :p_log_price,
--     p_log_status   = :p_log_status
-- WHERE p_log_id = :p_log_id;

-- Select (Get all use logs with reservation details)
-- SELECT 
--     l.p_log_id,
--     l.p_reserve_id,
--     l.p_checkin_time,
--     l.p_log_duration,
--     l.p_log_price,
--     l.p_log_status,
--     r.court_id,
--     r.p_reserve_name,
--     r.p_reserve_phone
-- FROM PaidCourtUseLog l
-- INNER JOIN PaidCourtReservation r ON l.p_reserve_id = r.p_reserve_id;

-- Clear all paid use logs
-- DELETE FROM PaidCourtUseLog;

-- Log ID Format (10 digits):
-- Position 1-4: Year (YYYY)
-- Position 5-6: Month (MM)
-- Position 7-8: Day (DD)
-- Position 9-10: Sequence number (01-99) of actual uses on that day
-- Example: 2025051711 = Used on May 17, 2025, 11th use of the day

-- =============================================================================
-- CourseCourtUseLog (บันทึกการใช้สนามจริง - จากคอร์สเรียน)
-- =============================================================================
CREATE TABLE CourseCourtUseLog (
    c_log_id           TEXT(10) PRIMARY KEY NOT NULL,    -- Log ID (YYYYMMDDXX)
    c_reserve_id       TEXT(10) NOT NULL,                -- FK to CourseCourtReservation
    c_checkin_time     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, -- Actual check-in time
    c_log_duration     REAL NOT NULL,                    -- Actual duration used (hours)
    c_log_status       TEXT(20) NOT NULL DEFAULT 'completed', -- Status: completed, cancelled, no-show
    
    FOREIGN KEY (c_reserve_id) REFERENCES CourseCourtReservation(c_reserve_id) ON DELETE CASCADE
);

-- Index for performance
CREATE INDEX IX_CourseCourtUseLog_Reserve ON CourseCourtUseLog(c_reserve_id);
CREATE INDEX IX_CourseCourtUseLog_CheckIn ON CourseCourtUseLog(c_checkin_time);

-- CRUD Templates for CourseCourtUseLog:

-- Insert (Add course use log)
-- INSERT INTO CourseCourtUseLog (c_log_id, c_reserve_id, c_checkin_time, c_log_duration, c_log_status)
-- VALUES (:c_log_id, :c_reserve_id, :c_checkin_time, :c_log_duration, :c_log_status);

-- Update (Edit course use log)
-- UPDATE CourseCourtUseLog
-- SET
--     c_log_duration = :c_log_duration,
--     c_log_status   = :c_log_status
-- WHERE c_log_id = :c_log_id;

-- Select (Get all course use logs with details)
-- SELECT 
--     l.c_log_id,
--     l.c_reserve_id,
--     l.c_checkin_time,
--     l.c_log_duration,
--     l.c_log_status,
--     r.court_id,
--     r.class_id,
--     r.c_reserve_name,
--     co.class_title
-- FROM CourseCourtUseLog l
-- INNER JOIN CourseCourtReservation r ON l.c_reserve_id = r.c_reserve_id
-- INNER JOIN Course co ON r.class_id = co.class_id;

-- Clear all course use logs
-- DELETE FROM CourseCourtUseLog;

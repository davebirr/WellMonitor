-- SQLite schema for Well Monitoring .NET Application

-- 1. High-frequency readings
CREATE TABLE IF NOT EXISTS Readings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TimestampUtc DATETIME NOT NULL,
    CurrentAmps REAL NOT NULL,
    Status TEXT NOT NULL,
    Synced BOOLEAN NOT NULL DEFAULT 0,
    Error TEXT
);

-- 2. Hourly summary
CREATE TABLE IF NOT EXISTS HourlySummary (
    DateHour TEXT NOT NULL, -- Format: YYYY-MM-DD HH
    TotalKwh REAL NOT NULL,
    PumpCycles INTEGER NOT NULL,
    Synced BOOLEAN NOT NULL DEFAULT 0,
    PRIMARY KEY (DateHour)
);

-- 3. Daily summary
CREATE TABLE IF NOT EXISTS DailySummary (
    Date DATE NOT NULL, -- Format: YYYY-MM-DD
    TotalKwh REAL NOT NULL,
    PumpCycles INTEGER NOT NULL,
    Synced BOOLEAN NOT NULL DEFAULT 0,
    PRIMARY KEY (Date)
);

-- 4. Monthly summary
CREATE TABLE IF NOT EXISTS MonthlySummary (
    Month TEXT NOT NULL, -- Format: YYYY-MM
    TotalKwh REAL NOT NULL,
    Synced BOOLEAN NOT NULL DEFAULT 0,
    PRIMARY KEY (Month)
);

-- 5. Relay action log
CREATE TABLE IF NOT EXISTS RelayActionLog (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TimestampUtc DATETIME NOT NULL,
    Action TEXT NOT NULL,
    Reason TEXT
);

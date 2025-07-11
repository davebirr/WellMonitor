# Data Model

This document defines the local data model for the Well Monitoring .NET Application. It supports high-frequency readings, hourly/daily/monthly summaries, and relay action logging for robust monitoring, graphing, and billing.

---

## 1. Readings Table
Stores each high-frequency measurement.

| Column Name   | Type      | Description                              |
|--------------|-----------|------------------------------------------|
| Id           | INTEGER   | Primary key (auto-increment)             |
| TimestampUtc | DATETIME  | UTC timestamp of reading                 |
| CurrentAmps  | REAL      | Current draw in amps                     |
| Status       | TEXT      | Status string (e.g., Normal, Dry)        |
| Synced       | BOOLEAN   | Has this record been synced to Azure?    |
| Error        | TEXT      | Optional error/status message            |

---

## 2. HourlySummary Table
Aggregates kWh and pump cycles per hour for daily pattern analysis.

| Column Name   | Type      | Description                              |
|--------------|-----------|------------------------------------------|
| DateHour     | TEXT      | Hour (YYYY-MM-DD HH, e.g., 2025-07-10 14)|
| TotalKwh     | REAL      | Total kWh used in this hour              |
| PumpCycles   | INTEGER   | Number of pump cycles in this hour       |
| Synced       | BOOLEAN   | Has this summary been synced?            |

---

## 3. DailySummary Table
Aggregates kWh and pump cycles per day.

| Column Name   | Type      | Description                              |
|--------------|-----------|------------------------------------------|
| Date         | DATE      | Date (YYYY-MM-DD)                        |
| TotalKwh     | REAL      | Total kWh used that day                  |
| PumpCycles   | INTEGER   | Number of pump cycles                    |
| Synced       | BOOLEAN   | Has this summary been synced?            |

---

## 4. MonthlySummary Table
Aggregates kWh per month for billing.

| Column Name   | Type      | Description                              |
|--------------|-----------|------------------------------------------|
| Month        | TEXT      | Month (YYYY-MM, e.g., 2025-07)           |
| TotalKwh     | REAL      | Total kWh used that month                |
| Synced       | BOOLEAN   | Has this summary been synced?            |

---

## 5. RelayActionLog Table
Logs all relay actions and manual overrides.

| Column Name   | Type      | Description                              |
|--------------|-----------|------------------------------------------|
| Id           | INTEGER   | Primary key                              |
| TimestampUtc | DATETIME  | When the action occurred                 |
| Action       | TEXT      | e.g., Cycle, ManualOverride              |
| Reason       | TEXT      | Why the action was taken                 |

---

This model supports:
- High-resolution short-term analysis
- Hourly/daily/monthly summaries for graphs and billing
- Reliable Azure sync and audit logging

Update as needed to fit evolving requirements.

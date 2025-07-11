## 16. Extensibility & Maintenance

- Use interfaces and dependency injection for all hardware, cloud, and service integrations (e.g., sensors, storage, IoT providers).
- Organize code by feature/service (e.g., `Services/`, `Models/`, `Controllers/`), not by technology, to make it easy to add new features.
- Isolate cloud-specific logic (e.g., Azure IoT, storage) behind abstractions so you can swap providers or add support for others in the future.
- Use DTOs and mapping layers to decouple internal models from external APIs.
- Write clear documentation and onboarding guides for contributors.
- Maintain a changelog and use semantic versioning for releases.
## 15. Hardware Abstraction

- Abstract GPIO and camera access behind interfaces (e.g., `IGpioService`, `ICameraService`) to enable easy mocking and testing.
- Use dependency injection to provide real or mock implementations as needed.
- Ensure all hardware interactions are wrapped in try/catch blocks to detect and handle hardware errors (e.g., camera or relay failure).
- Log hardware errors with error level and relevant context (e.g., which device, operation, and exception details).
- Provide fallback or recovery logic where possible (e.g., retry, alert, or switch to degraded mode if hardware is unavailable).
## 14. Testing & Validation

### Unit Tests

- **Database Service:**
  - Test CRUD operations for each table (Readings, Summaries, RelayActionLog).
  - Test data retention logic (purge after 30 days).
- **Sync Service:**
  - Test queuing, retry, and marking records as synced.
  - Test error handling for failed uploads.
- **Relay Control Service:**
  - Test debounce logic and relay state transitions.
  - Test logging of relay actions.
- **OCR/Image Service:**
  - Test image preprocessing and text extraction logic with sample images.
- **Telemetry Service:**
  - Test message formatting and serialization.

### Integration Tests

- **End-to-End Data Flow:**
  - Simulate a reading, sync to Azure, and verify data is marked as synced.
- **Manual Override:**
  - Simulate a PowerApp/manual relay cycle and verify audit log entry.
- **Device Twin/Config:**
  - Simulate config update via Device Twin and verify local config is updated.
- **Error/Alerting:**
  - Simulate error conditions (e.g., repeated sync failure) and verify alerting/logging.

### Simulation for Device & Cloud Interactions

- **Mock Services:**
  - Use dependency injection to inject mock implementations of hardware (GPIO, camera) and cloud (IoT Hub, Azure Functions) services.
- **Test Harness:**
  - Create a test harness or console app that can simulate device events, cloud commands, and network conditions.
- **Emulators:**
  - Use Azure IoT Explorer or IoT Hub Device Simulation for cloud-side testing.
- **Sample Data:**
  - Use pre-recorded images and telemetry data for repeatable tests.

---

### Implementation To-Do

1. Write unit tests for each service using xUnit or NUnit.
2. Use Moq or similar library for mocking dependencies.
3. Create integration tests for end-to-end scenarios.
4. Document how to run tests and simulate device/cloud interactions.
## 13. PowerApp & API Design

### Endpoints / Azure Functions for PowerApp

- **Get Device Status**
  - `GET /api/status`
  - Returns current status, last reading, relay state, and recent alerts.
- **Get Usage Summary**
  - `GET /api/usage?period=month|day|hour`
  - Returns kWh and pump cycles for the requested period.
- **Cycle Power (Manual Override)**
  - `POST /api/cycle-relay`
  - Triggers a relay cycle; logs the action and who requested it.
- **Get Billing Summary**
  - `GET /api/billing`
  - Returns current month’s kWh, cost, and billing rate.
- **Get Graph Data**
  - `GET /api/graph?period=day|hour`
  - Returns data points for graphing in PowerApp.

---

### Authentication & Authorization (Simple Scheme)

- Use Azure AD (M365) accounts for tenants.
- Protect Azure Functions with Azure AD authentication (Easy Auth).
- Assign tenants to a security group in Azure AD (e.g., “Well Tenants”).
- Only allow access to API endpoints for users in this group.
- PowerApp can use the user’s M365 login for SSO—no extra passwords needed.

---

### Logging & Auditing Manual Overrides

- Every manual override (e.g., relay cycle) should log:
  - Timestamp
  - User (from Azure AD)
  - Action (e.g., “CycleRelay”)
  - Reason (if provided)
- Store these logs in the `RelayActionLog` table and optionally sync to Azure for centralized audit.

---

### Implementation To-Do

1. Create Azure Functions for each endpoint above.
2. Enable Azure AD authentication for the API.
3. Restrict access to the “Well Tenants” group.
4. Log all manual override actions with user info and timestamps.
5. Expose logs to admins via a secure endpoint or dashboard.
## 9. Security

- Store device credentials and connection strings securely on the device (e.g., in a protected config file or environment variables).
- Secure the device with SSH and public/private keys for remote access; disable password login.
- Use device-level authentication for Azure IoT Hub (connection string or X.509 certificates).
- Never hard-code secrets in source code or commit them to version control.

---

## 10. Device Management

- Use CI/CD (e.g., GitHub Actions, Azure Pipelines) to build, test, and deploy application updates to the Raspberry Pi.
- Support remote diagnostics and configuration via:
  - Azure IoT Hub Device Twins for configuration/state.
  - Direct Methods for remote commands (e.g., restart, update, diagnostics).
  - Logging and health status reporting to Azure for monitoring.

---

## 11. Azure Integration

- Use the latest stable Azure IoT SDK for C#.
- Use Device Twins for:
  - Storing and syncing device configuration (e.g., billing rate, thresholds).
  - Reporting device state (e.g., firmware version, last sync time).
- Use Direct Methods for:
  - Remote commands (e.g., cycle relay, trigger diagnostics, update config).

### Telemetry Message Structure (Recommended)

```
{
  "timestamp": "2025-07-10T14:00:00Z",
  "currentDrawAmps": 5.2,
  "status": "Normal",
  "energyKWh": {
    "hour": 0.52,
    "day": 3.8,
    "month": 112.4
  },
  "pumpCycles": 3,
  "deviceId": "rpi4b-well01",
  "relayState": "On",
  "error": null
}
```

### Command Structure (Direct Methods)

**Example: Cycle Relay Command**
```
{
  "methodName": "CycleRelay",
  "payload": {
    "reason": "ManualOverride",
    "requestedBy": "tenant@example.com"
  }
}
```

**Example: Update Config Command**
```
{
  "methodName": "UpdateConfig",
  "payload": {
    "billingRate": 0.175,
    "alertThresholds": {
      "dry": 1.0,
      "rcyc": 10
    }
  }
}
```
# Data Logging & Sync Strategy

This document describes the approach for local data logging, retention, and synchronization with Azure for the Well Monitoring .NET Application.

## 1. Local Data Logging

- **High-Resolution Short-Term Storage:**
  - Log current draw, status, and timestamp at high frequency (e.g., every 1–5 seconds) to a local SQLite database.
  - Retain detailed data for a rolling window (e.g., 7–30 days) to support daily/weekly graphing and troubleshooting.

- **Long-Term Summarization:**
  - Aggregate and store daily and monthly kWh totals in separate tables.
  - After the rolling window expires, purge or downsample high-resolution data, keeping only daily/monthly summaries.


## 2. Data Model

The system uses the following tables:

- **Readings**: High-frequency measurements (current, status, timestamp, error, synced)
- **HourlySummary**: Aggregated kWh and pump cycles per hour (for daily pattern analysis)
- **DailySummary**: Aggregated kWh and pump cycles per day
- **MonthlySummary**: Aggregated kWh per month (for billing)
- **RelayActionLog**: All relay actions and manual overrides

See `docs/DataModel.md` for full schema and field descriptions.

---


## 3. Syncing to Azure & Retention Policy

**Telemetry Upload:**
- Upload telemetry (current, status, etc.) to Azure IoT Hub every 15 minutes.
- Use a local queue/flag to ensure unsent data is retried until successful.

**Data Retention:**
- Retain high-frequency readings for a rolling 30-day window.
- Aggregate and store hourly, daily, and monthly kWh totals in their respective tables for long-term use.
- After 30 days, purge high-frequency readings, but keep hourly, daily, and monthly summaries.

**Long-Term Summarization:**
- Hourly, daily, and monthly summaries are never purged (unless you decide otherwise for storage reasons).

---

## 4. Implementation To-Do List

1. Implement a background sync service that:
   - Uploads unsynced telemetry every 15 minutes.
   - Marks records as synced after successful upload.
   - Retries failed uploads.
2. Implement a retention/cleanup service that:
   - Runs daily to purge high-frequency readings older than 30 days.
   - Ensures summaries are preserved.
3. Ensure all sync and retention actions are logged for audit and troubleshooting.
4. Test offline/online transitions and data recovery.
5. Document configuration options for sync interval and retention window.


## 5. Data for Graphs and Billing

### Graphs

- Use high-resolution local data (from the `Readings` and `HourlySummary` tables) to generate hourly and daily usage graphs (current draw, pump cycles, etc.).
- Generate graphs locally for immediate access and troubleshooting.
- Upload graphs (as images or data points) and their underlying data (e.g., hourly summaries) to Azure for remote visualization in dashboards or PowerApps.
- Retain high-resolution data for 30 days to support detailed graphing; after that, rely on hourly/daily summaries for longer-term trends.

### Billing

- Use the `MonthlySummary` table (total kWh per month) for tenant billing.
- Store monthly summaries both locally and in Azure to ensure data is not lost and is available for billing even if the device is offline for a period.
- Ensure that billing data is protected both in transit (when syncing to Azure) and at rest (local database and Azure storage).
- Track the current electricity billing rate (e.g., $0.175 per kWh for APCO in Roanoke, VA). Update this rate periodically by dividing the total bill amount by the total kWh used.

---

## 6. Implementation To-Do List (Graphs & Billing)

1. Implement local graph generation using high-resolution and summary data.
2. Implement upload of graphs and/or underlying summary data to Azure for remote visualization.
3. Ensure monthly summaries are synced to Azure for billing and stored securely.
4. Track and update the current billing rate (e.g., $0.175 per kWh for APCO in Roanoke, VA) in configuration or a dedicated table.
5. Document the format and location of uploaded graphs/data for easy integration with PowerApps or dashboards.
6. Add tests to verify graph accuracy and billing data integrity.

## 4. Data Retention Policy

- **Detailed readings:** Keep 7–30 days.
- **Daily summaries:** Keep 1–2 years.
- **Monthly summaries:** Keep indefinitely.



## 8. Error Handling & Logging

- Use three logging levels: info, warning, and error.
  - `Info`: Normal operations and successful syncs.
  - `Warning`: Recoverable issues (e.g., pump in 'Dry' status, temporary network loss).
  - `Error`: Critical failures (e.g., 'rcyc' detected, no display, repeated sync failures, device offline).
- Store logs both locally (file-based) and in Azure for redundancy and remote diagnostics.
- Implement alerting for critical failures:
  - Trigger alerts on repeated sync failures or when the device is offline for a defined period.
  - Consider integration with Azure Monitor, email, or PowerApp notifications for real-time alerts.
- Ensure all log entries include timestamps and relevant context (e.g., status, error details).

- Use a local SQLite database with tables for:
  - `Readings` (timestamp, current, status, etc.)
  - `HourlySummary` (date-hour, total_kWh, pump cycles)
  - `DailySummary` (date, total_kWh, pump cycles)
  - `MonthlySummary` (month, total_kWh)
  - `SyncStatus` (to track what’s been uploaded)
- Use async/await for all I/O operations for scalability and responsiveness.
- Use dependency injection for database and sync services to improve testability and maintainability.
- Log all sync actions and errors with timestamps for audit and troubleshooting.
- Use DTOs for data transfer to Azure, keeping internal models and transfer objects separate.
- Use DTOs for data transfer to Azure.

## Example SQLite Schema

- `Readings` (timestamp, current, status, ...)
- `DailySummary` (date, total_kWh)
- `MonthlySummary` (month, total_kWh)
- `SyncStatus` (record_id, synced)

---

This strategy ensures accurate, granular data for short-term analysis and efficient, reliable long-term storage and billing.

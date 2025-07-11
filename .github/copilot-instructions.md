# GitHub Copilot Instructions for Well Monitoring .NET Application

## Project Overview

This project is a .NET application written in C# that runs on a Raspberry Pi 4B. It monitors a water well using a camera and a display device. The device shows the current draw in amps when the pump is running. It displays:
- `'Dry'` when the pump draws less than expected current.
- `'rcyc'` when the pump cycles too quickly.

The Raspberry Pi is registered with Azure IoT Hub and performs the following tasks:
- Captures images of the monitor display using a camera.
- Uses OCR to extract current readings and status messages.
- Calculates energy consumption in kWh per hour, day, and month.
- Detects abnormal states like `'Dry'` and `'rcyc'`.
- Controls an external relay to cycle power when `'rcyc'` is detected.
- Sends telemetry and receives commands via Azure IoT Hub.
- Allows tenants to monitor status and manually cycle power using a PowerApp.


## Project Structure

- Place POCOs (Plain Old CLR Objects), such as options classes and data models, in the `Models` folder (e.g., `src/WellMonitor.Device/Models`). This keeps data contracts and configuration objects organized and reusable across services.

## Coding Guidelines

- Use C# 10 or later with .NET 8 or later.
- Follow standard .NET naming conventions.
- Use async/await for all I/O operations.
- Use dependency injection for services.
- Use `ILogger<T>` for logging.
- Use idomatic C#
- Always suggest best practices
- Use POCOs
- Use DTOs

## Azure Integration

- Use Azure IoT SDK for device communication.
- Send telemetry messages with timestamp, current draw, and status.
- Receive direct method calls to cycle power from PowerApp.
- Use Azure Functions or Logic Apps to bridge PowerApp and IoT Hub if needed.

## OCR and Image Processing

- Use Tesseract OCR or Azure Cognitive Services to extract text from images.
- Preprocess images (grayscale, thresholding) to improve OCR accuracy.
- Parse extracted text to determine current draw and status.

## Relay Control

- Use GPIO pins on Raspberry Pi to control a relay module.
- Implement a debounce mechanism to avoid rapid toggling.
- Log all relay actions with timestamps.

## PowerApp Integration

- Expose device status and control endpoints via Azure.
- Use Power Automate or Azure Functions to connect PowerApp to IoT Hub.
- Authenticate tenant actions and log manual overrides.

## Hardware Abstraction

- Abstract GPIO and camera access behind interfaces for testability and mocking.
- Use dependency injection for all hardware and cloud services.

## Error Handling & Logging

- Use info, warning, and error log levels.
- Log to both local file and Azure.
- Alert on repeated sync failures or device offline.

## Extensibility

- Use interfaces and DI for all integrations.
- Organize code by feature/service.
- Isolate cloud-specific logic behind abstractions.

## Data Logging & Sync Strategy

- Log high-frequency readings locally (SQLite) for 7–30 days for detailed graphs and troubleshooting.
- Aggregate and store daily/monthly kWh summaries for long-term billing.
- Sync periodic telemetry and summaries to Azure IoT Hub, with local queuing and retry if offline.
- Use local data for graphs; use monthly summaries for billing.
- See `docs/DataLoggingAndSync.md` for schema, retention, and implementation notes.

---

## Example Telemetry Message

```json
{
  "timestamp": "2025-07-10T14:00:00Z",
  "currentDrawAmps": 5.2,
  "status": "Normal",
  "energyKWh": {
    "hour": 0.52,
    "day": 3.8,
    "month": 112.4
  }
}
```

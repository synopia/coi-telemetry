# Captain of Industry Telemetry Mod

This project provides a telemetry system for the game **Captain of Industry (CoI)**. It consists of a game mod that collects real-time data from your factory and a web-based dashboard to visualize it.

## Features

- **Product Flow Tracking**: Monitor production, consumption, imports, exports, and storage levels for all products.
- **Machine Metrics**: Track machine efficiency and status.
- **Vehicle Monitoring**: Keep an eye on your vehicle fleet's activity.
- **Real-time API**: Built-in webserver in the mod serves data via a JSON API.
- **Live Dashboard**: A modern React-based dashboard to visualize your factory's performance.

## Project Structure

- `src/RealMod`: The main C# mod logic that integrates with the Captain of Industry simulation.
- `src/Abstractions`: Shared interfaces and contracts used by the mod and loader.
- `src/PluginLoader`: A loader component to handle mod initialization.
- `src/Dashboard`: A React/Vite-based frontend for visualizing the telemetry data.

## Getting Started

### The Mod

The mod is designed to be compiled as a DLL and loaded into Captain of Industry.

1.  Open `coi-telemetry.sln` in Visual Studio or Rider.
2.  Build the solution (ensure you have the necessary game references if building locally).
3.  The mod starts a local webserver on `http://localhost:17891` when the game is running.

### The Dashboard

The dashboard is a React application located in `src/Dashboard`.

1.  Navigate to `src/Dashboard`.
2.  Install dependencies:
    ```bash
    pnpm install
    ```
3.  Start the development server:
    ```bash
    pnpm dev
    ```
4.  Open your browser to the provided Vite URL (usually `http://localhost:5173`). The dashboard will attempt to connect to the mod running on port 17891.

## Technical Details

### API Endpoints

Once the mod is running, the following endpoints are available:

- `GET /api/health`: Returns `{"ok":true}` if the server is running.
- `GET /api/latest`: Returns the most recent telemetry snapshot in JSON format.

### Metrics Collected

- **Product Flow**: Amount produced, consumed, mined, dumped, imported, exported, and lost, along with estimated time until storage is empty or full.
- **Machines**: Operational state and performance.
- **Vehicles**: Current status and utilization.

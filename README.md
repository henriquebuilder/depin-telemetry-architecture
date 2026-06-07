# DePIN Telemetry Architecture

A reference architecture for real-time monitoring of DePIN (Decentralized Physical Infrastructure) nodes — built to explore how distributed systems handle high-frequency telemetry ingestion across multiple runtimes.

The stack is Go + .NET 9 + Angular 19. Each layer was chosen for a specific reason, and those reasons are documented below — not as marketing, but because the tradeoffs actually matter when you're designing something like this.

---

## What this solves

DePIN networks generate continuous telemetry from thousands of nodes: CPU load, memory, disk, network I/O, block height, peer count. The challenge isn't storing this data — it's ingesting it fast enough that you're not losing events during spikes, processing it with enough business logic to make health decisions, and reflecting the state in a UI that doesn't choke on real-time updates.

This system separates those three concerns into independent layers that communicate through a message broker. The result is that each layer can fail, scale, or be replaced without breaking the others.

---

## Architecture

```
graph TB
    subgraph "Edge Layer"
        A[Node Emulator<br/>Telemetry Generator]
    end

    subgraph "Ingestion Layer - Go"
        B[HTTP Ingestion Server<br/>Worker Pool Pattern]
        C[Buffered Channel<br/>Capacity: 1000]
        D[5 Goroutine Workers<br/>Async Processing]
        E[RabbitMQ Publisher<br/>Topic Exchange]
    end

    subgraph "Message Broker"
        F[RabbitMQ<br/>telemetry_queue]
    end

    subgraph "Processing Layer - .NET 9"
        G[BackgroundService<br/>TelemetryConsumer]
        H[NodeHealthService<br/>Business Logic]
        I[NodeHealthValidator<br/>Domain Rules]
        J[EF Core + PostgreSQL<br/>Persistence]
    end

    subgraph "Real-time Layer"
        K[SignalR Hub<br/>WebSocket Broadcast]
        L[NodeHealthAlert<br/>Event Stream]
    end

    subgraph "Presentation Layer - Angular 19"
        M[TelemetryService<br/>SignalR Client]
        N[NodeMonitorComponent<br/>OnPush + Signals]
        O[Metrics Dashboard<br/>Reactive UI]
    end

    A -->|POST /ingest| B
    B -->|Validate & Enqueue| C
    C --> D
    D -->|Publish| E
    E --> F
    F --> G
    G --> H
    H --> I
    H --> J
    I -->|Unhealthy Detected| K
    K --> L
    L --> M
    M --> N
    N --> O
```

---

## Components

### Telemetry Injector — Go

The ingestion layer needs to accept bursts of telemetry without blocking. Go fits this well: goroutines are cheap, the channel-based worker pool gives you natural backpressure, and `sync/atomic` lets you track metrics without locking the hot path.

The setup is 5 workers reading from a buffered channel with capacity 1000. When the channel fills up, the HTTP handler returns 429 instead of queuing indefinitely — an explicit rejection is better than silent degradation. Events go to RabbitMQ with routing keys by device type, so downstream consumers can subscribe selectively.

One thing worth noting: the automatic reconnection with exponential backoff isn't just a nice-to-have here. RabbitMQ connections drop under load, and a Go service that crashes on reconnect failure isn't suitable for infrastructure monitoring.

### Core Processor — .NET 9

The processing layer has the most business logic, which is why it's in .NET with Clean Architecture. The domain layer holds the health validation rules (CPU > 80%, Memory > 85%, Disk > 90%, silence > 5min), and those rules don't know anything about RabbitMQ or SignalR. That separation makes the validation logic independently testable and easy to adjust without touching the infrastructure.

The `BackgroundService` consuming from RabbitMQ handles its own fault tolerance — if the connection drops, the service restarts the consumer without taking down the whole API. The SignalR hub broadcasts to the dashboard the moment a node is flagged unhealthy, via `IHubContext` injected into the background processing path.

Consecutive unhealthy check tracking is there because a single spike shouldn't trigger an alert. Three consecutive unhealthy readings does.

### Admin Dashboard — Angular 19

The dashboard gets hit with real-time updates over SignalR WebSockets. The main performance decision here was OnPush change detection — Angular's default strategy checks every component on every event, which becomes a problem when you're streaming telemetry continuously. OnPush restricts checks to when inputs actually change or events originate from the component itself.

Angular Signals handle the reactive state. They're simpler than RxJS for this use case: the telemetry comes in, the signal updates, computed signals recalculate what's needed, the view reflects it. No manual subscription management, no unsubscribe bookkeeping.

---

## Why this stack combination

The honest reason: each piece is doing what it's best at.

Go handles the ingestion layer because it's genuinely fast to spin up goroutines under load, and the simplicity of the concurrency model makes the ingestion path easy to reason about. It doesn't have the ecosystem for domain-heavy business logic, which is fine — that's not its job here.

.NET handles the processing layer because Clean Architecture patterns are well-established in the .NET ecosystem, EF Core + PostgreSQL is a mature combination, and SignalR is a first-class citizen rather than an afterthought.

Angular handles the dashboard because the combination of Signals and OnPush gives you fine-grained reactivity without the performance overhead of checking everything on every tick. For a monitoring dashboard with continuous updates, that matters.

The tradeoff is operational complexity — three runtimes, three build pipelines, more surface area for configuration. For a production system at small scale, this would be over-engineered. For a reference architecture demonstrating how each layer would work in a larger distributed system, it's the point.

---

## Quick Start

### Prerequisites

- Docker and Docker Compose
- .NET 9 SDK (local dev)
- Go 1.22+ (local dev)
- Node.js 20+ (local dev)

### Run with Docker Compose

```bash
docker-compose up -d
docker-compose logs -f
docker-compose down
```

### Service endpoints

| Service | URL | Notes |
| :--- | :--- | :--- |
| Telemetry Injector | http://localhost:8080 | POST /ingest, GET /metrics |
| Core Processor API | http://localhost:5000 | GET /api/nodes/unhealthy |
| Admin Dashboard | http://localhost:4200 | Real-time monitoring UI |
| RabbitMQ Management | http://localhost:15672 | guest / guest |
| PostgreSQL | localhost:5432 | depin_telemetry / postgres |

---

## Local Development

**Go:**
```bash
cd telemetry-injector-go
go mod download
go run main.go
```

**.NET:**
```bash
cd core-processor-net
dotnet restore && dotnet build
dotnet run --project src/DePinCore.API
```

**Angular:**
```bash
cd admin-dashboard-angular
npm install
npm start
```

---

## API

### Submit telemetry

```bash
curl -X POST http://localhost:8080/ingest \
  -H "Content-Type: application/json" \
  -d '{
    "device_id": "node-001",
    "device_type": "validator",
    "location": "us-east-1",
    "cpu_usage": 85.5,
    "memory_usage": 78.2,
    "disk_usage": 65.0,
    "network_in": 1024000,
    "network_out": 512000,
    "metrics": {
      "block_height": 12345,
      "peers_connected": 42
    }
  }'
```

### Get unhealthy nodes

```bash
curl http://localhost:5000/api/nodes/unhealthy
```

---

## Health thresholds

| Metric | Threshold |
| :--- | :--- |
| CPU Usage | > 80% |
| Memory Usage | > 85% |
| Disk Usage | > 90% |
| Telemetry Silence | > 5 minutes |
| Consecutive unhealthy | ≥ 3 → alert escalation |

---

## What's missing for production

Authentication on the ingestion endpoint, TLS everywhere, RabbitMQ auth hardening, rate limiting, input sanitization. This architecture is a reference implementation, not a production deployment — those concerns are real and would need to be addressed before this handles actual infrastructure data.

---

> Reference architecture for DePIN monitoring — Go ingestion, .NET processing, Angular dashboard. Fork and adapt.

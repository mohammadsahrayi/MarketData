 Project Summary: Real-Time Market Data Processor
 Goal

To build a high-performance, scalable, and concurrent system that:

    Simulates real-time market data feeds

    Calculates moving averages per symbol

    Detects price spikes over 2% in a 1-second interval

    Handles 10,000+ updates per second

    Applies Clean Architecture principles and concurrent programming patterns

 Architecture Overview

The project follows Clean Architecture and separation of concerns:
Layer	Description
Application	Interfaces like IPriceUpdateProcessor, validators (e.g. PriceUpdateValidator)
Infrastructure	Channel-based queue (InMemoryPriceQueue), hosted services
Domain	PriceUpdate model
Presentation (Console)	The main Host-based console application with dependency injection

The system uses:

    .NET Generic Host to bootstrap services

    Channel<T> for thread-safe message passing

    BackgroundService for both generating and processing updates

    FluentValidation for input validation

    ILogger for structured logging

    IOptions<T> for app configuration

 Key Components
 PriceUpdateGeneratorService

    Generates randomized price updates for a fixed set of symbols (e.g., "shasta", "fameli", "folad")

    Produces 10,000 price updates per batch

    Validates each update before sending it into the queue

 InMemoryPriceQueue

    In-memory unbounded Channel<PriceUpdate> for decoupling producer and consumer

    Acts as the backbone for passing data safely across threads

 PriceUpdateBackgroundService

    Consumes price updates from the channel

    Stores a moving window of latest N prices per symbol using ConcurrentQueue

    Computes moving average

    Detects price anomalies (spikes) and logs alerts if a price change exceeds the defined threshold in the past second

    Uses a SemaphoreSlim to throttle processing for performance

 PriceUpdateValidator

    Ensures each price update has:

        A non-empty, max-length symbol

        A price > 0

 Configurable Settings (via appsettings.json)

{
  "MarketDataSettings": {
    "MovingAverageLength": 50,
    "SpikeThresholdPercent": 2.0
  }
}

These values are injected using IOptions<MarketDataSettings>.
 Performance & Concurrency

    Handles high-throughput load with efficient producer-consumer pattern

    Uses parallelism with safety:

        Channel<T> for async queues

        ConcurrentDictionary and ConcurrentQueue for symbol-specific state

        SemaphoreSlim to limit concurrency and avoid thread starvation

 Logging and Monitoring

    Real-time log of:

        Number of requests sent per second

        Number of requests processed per second

        Any detected price spikes

 Why Console App?

You chose Console + Background Services because:

    It is lightweight, fast to start, and perfect for simulation/testing

    Clean separation of concerns using hosted services

    It can easily evolve into a Web API, gRPC or WebSocket interface in the future without breaking core logic

 Clean Architecture Advantages

    Each component (simulation, processing, queueing, validation) is:

        Loosely coupled

        Testable independently

        Replaceable (e.g., switch InMemoryPriceQueue to Kafka or RabbitMQ)

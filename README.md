# Handly

![NuGet](https://img.shields.io/nuget/v/Handly?label=Handly&style=flat-square)
![.NET](https://img.shields.io/badge/.NET-6%2B-blue?style=flat-square)
![CI](https://github.com/yourusername/Handly/actions/workflows/ci.yml/badge.svg)

**Handly** is a lightweight and transparent mediator-style dispatcher for .NET.
Inspired by [MediatR](https://github.com/jbogard/MediatR), it aims to be faster, leaner, and open-source.

## 🚀 Benchmarks

> Performed on a MacBook M1 Pro (.NET 9, BenchmarkDotNet, Release build)

| Method                             |      Mean |  StdDev |    Ratio | Allocated |
| ---------------------------------- | --------: | ------: | -------: | --------: |
| MediatR.Send (no behaviors)        |  77.24 ns | 0.65 ns | baseline |     288 B |
| **Handly.Dispatch (no behaviors)** | 103.92 ns | 1.28 ns |     +35% | **264 B** |
| MediatR.Send (with behaviors)      | 270.31 ns | 4.53 ns |    +250% |    1072 B |
| Handly.Dispatch (with behaviors)   | 635.12 ns | 7.28 ns |    +722% |     864 B |

## ✅ Why Handly?

-   **Open-source and Free** — no license cost, forever.
-   **Transparent and Hackable** — know what runs and why.
-   **Performant** — especially with no behaviors.
-   **No Reflection at Runtime** — compiled, cached, and minimal allocations.

## ✨ Features

-   Lightweight `Dispatcher` for `IRequest<TResponse>`
-   Pipeline behaviors (logging, validation, metrics...)
-   Assembly scanning for handler auto-registration
-   Plug-and-play DI integration
-   Dead simple API and structure

---

## 📦 Installation

```bash
dotnet add package Handly
```

Or via NuGet:

> https://www.nuget.org/packages/Handly

---

## 👋 Getting Started

### 1. Define a Request

```csharp
public class Ping : IRequest<string> { }
```

### 2. Create a Handler

```csharp
public class PingHandler : IRequestHandler<Ping, string>
{
    public Task<string> Handle(Ping request, CancellationToken cancellationToken)
    {
        return Task.FromResult("Pong");
    }
}
```

### 3. (Optional) Add a Behavior

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Handling {typeof(TRequest).Name}");
        var response = await next();
        _logger.LogInformation($"Handled {typeof(TRequest).Name}");
        return response;
    }
}
```

### 4. Register with DI

```csharp
builder.Services.AddHandly(cfg =>
{
    cfg.RegisterHandlerFromAssembly(Assembly.GetExecutingAssembly());
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});
```

### 5. Dispatch a request

```csharp
public class MyService
{
    private readonly IDispatcher _dispatcher;

    public MyService(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task ExecuteAsync()
    {
        var request = new Ping();
        var response = await _dispatcher.Dispatch(request);
        Console.WriteLine(response); // Pong
    }
}
```

---

## ❤️ Contributing

Pull requests are welcome! For major changes, please open an issue first.

## 🔒 License

MIT. Free as in freedom.

> Made with ❤️ by developer who like things fast and clean.

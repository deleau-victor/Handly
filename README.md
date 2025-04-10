# Handly

**Handly** is a lightweight and flexible mediator-style dispatcher for .NET.
It allows you to separate responsibilities using request handlers and pipeline behaviors, inspired by [MediatR](https://github.com/jbogard/MediatR), but with full control and transparency.

## ✨ Features

-   Generic `Dispatcher` for any `IRequest<TResponse>`
-   Clean pipeline behavior system (logging, validation, etc.)
-   Assembly scanning and auto-registration
-   Modular and testable architecture
-   No runtime magic — fully DI-friendly

## 📦 Installation

```bash
dotnet add package Handly
```

Or via NuGet:

> https://www.nuget.org/packages/Handly

## 🚀 Getting Started

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

	public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
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
	})
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

## 🔒 License

Handly is released under the [MIT License](LICENSE).

## ❤️ Contributing

Pull requests are welcome. If you plan to contribute something major, feel free to open an issue first.

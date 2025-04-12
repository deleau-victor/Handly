using System.Collections.Concurrent;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Handly.Benchmark.Internal;

public class DummyRequest : IRequest<string> { }

public class DummyHandler : IRequestHandler<DummyRequest, string>
{
	public ValueTask<string> Handle(DummyRequest request, CancellationToken cancellationToken)
		=> ValueTask.FromResult("HotHandled");
}

public class DummyRequestCold : IRequest<string> { }

public class DummyHandlerCold : IRequestHandler<DummyRequestCold, string>
{
	public ValueTask<string> Handle(DummyRequestCold request, CancellationToken cancellationToken)
		=> ValueTask.FromResult("ColdHandled");
}

public class DummyBehavior : IPipelineBehavior<DummyRequest, string>
{
	public ValueTask<string> Handle(DummyRequest request, RequestHandlerDelegate<DummyRequest, string> next, CancellationToken cancellationToken)
		=> next();
}

[MemoryDiagnoser]
public class InternalDispatcherBenchmarks
{
	private ServiceProvider _serviceProvider = null!;
	private IDispatcher _dispatcher = null!;

	[GlobalSetup]
	public void GlobalSetup()
	{
		var services = new ServiceCollection();

		services.AddTransient<IRequestHandler<DummyRequest, string>, DummyHandler>();

		services.AddTransient<IRequestHandler<DummyRequestCold, string>, DummyHandlerCold>();

		services.AddTransient<IPipelineBehavior<DummyRequest, string>, DummyBehavior>();

		services.AddHandly(cfg =>
		{
			cfg.RegisterHandlerFromAssemblyContaining<DummyHandler>();
		});

		_serviceProvider = services.BuildServiceProvider();
		_dispatcher = _serviceProvider.GetRequiredService<IDispatcher>();

		_ = _dispatcher.Dispatch(new DummyRequest());
	}

	private void ClearDispatcherDelegateCache()
	{
		var cacheField = typeof(Dispatcher).GetField("_delegateCache", BindingFlags.Static | BindingFlags.NonPublic);
		if (cacheField != null)
		{
			var dict = (ConcurrentDictionary<Type, Func<object, IServiceProvider, CancellationToken, ValueTask<object>>>)cacheField.GetValue(null)!;
			dict.Clear();
		}
	}

	[IterationSetup(Target = nameof(ColdDispatchCall))]
	public void SetupColdCall()
	{
		ClearDispatcherDelegateCache();
	}

	[Benchmark]
	public ValueTask<string> ColdDispatchCall()
	{
		var request = new DummyRequestCold();
		return _dispatcher.Dispatch(request);
	}

	[Benchmark]
	public ValueTask<string> HotDispatchCall()
	{
		var request = new DummyRequest();
		return _dispatcher.Dispatch(request);
	}

	[Benchmark]
	public object HandlerResolution()
	{
		return _serviceProvider.GetRequiredService<IRequestHandler<DummyRequest, string>>();
	}

	[Benchmark]
	public ValueTask<string> SimulatePipelineComposition()
	{
		var behavior = new DummyBehavior();
		RequestHandlerDelegate<DummyRequest, string> handler = () => ValueTask.FromResult("PipelineResult");
		RequestHandlerDelegate<DummyRequest, string> pipeline = () =>
			behavior.Handle(new DummyRequest(), handler, CancellationToken.None);

		return pipeline();
	}
}


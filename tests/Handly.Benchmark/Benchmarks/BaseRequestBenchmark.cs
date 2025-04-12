using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Order;
using BenchmarkTests.Behaviours;
using BenchmarkTests.Config;
using BenchmarkTests.Handlers;
using Handly;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace BenchmarkTests.Benchmarks;

[Config(typeof(BenchmarkConfig))]
[HideColumns(Column.Job, Column.RatioSD, Column.AllocRatio, Column.Gen0, Column.Gen1)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[MemoryDiagnoser]
public class BaseRequestBenchmark
{
	private IMediator _mediator = null!;
	private IMediator _mediatorWithBehaviours = null!;

	private IDispatcher _dispatcher = null!;
	private IDispatcher _dispatcherWithBehaviours = null!;

	[GlobalSetup]
	public void Setup()
	{
		// Without behaviours
		var services = new ServiceCollection();
		services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<MediatRPingHandler>());
		services.AddHandly(cfg => cfg.RegisterHandlerFromAssemblyContaining<HandlyPingHandler>());
		var sp = services.BuildServiceProvider();
		_mediator = sp.GetRequiredService<IMediator>();
		_dispatcher = sp.GetRequiredService<IDispatcher>();

		// With behaviours
		var servicesWithBehaviors = new ServiceCollection();
		servicesWithBehaviors.AddMediatR(cfg =>
		{
			cfg.RegisterServicesFromAssemblyContaining<MediatRPingHandler>();
			cfg.AddBehavior(typeof(MediatR.IPipelineBehavior<,>), typeof(MediatRLoggingBehaviour<,>));
			cfg.AddBehavior(typeof(MediatR.IPipelineBehavior<,>), typeof(MediatRExceptionBehaviour<,>));
			cfg.AddBehavior(typeof(MediatR.IPipelineBehavior<,>), typeof(MediatRTimerBehaviour<,>));
		});

		servicesWithBehaviors.AddHandly(cfg =>
		{
			cfg.RegisterHandlerFromAssemblyContaining<HandlyPingHandler>();
			cfg.AddBehaviour(typeof(Handly.IPipelineBehavior<,>), typeof(HandlyLoggingBehaviour<,>));
			cfg.AddBehaviour(typeof(Handly.IPipelineBehavior<,>), typeof(HandlyExceptionBehaviour<,>));
			cfg.AddBehaviour(typeof(Handly.IPipelineBehavior<,>), typeof(HandlyTimerBehaviour<,>));
		});

		var spWithBehaviors = servicesWithBehaviors.BuildServiceProvider();
		_mediatorWithBehaviours = spWithBehaviors.GetRequiredService<IMediator>();
		_dispatcherWithBehaviours = spWithBehaviors.GetRequiredService<IDispatcher>();
	}

	[Benchmark(Baseline = true)]
	public Task<string> MediatR_Send_WithoutBehaviors()
		=> _mediator.Send(new MediatRPing());

	[Benchmark]
	public ValueTask<string> Handly_Dispatch_WithoutBehaviors()
		=> _dispatcher.Dispatch(new HandlyPing());

	[Benchmark]
	public Task<string> MediatR_Send_WithBehaviors()
		=> _mediatorWithBehaviours.Send(new MediatRPing());

	[Benchmark]
	public ValueTask<string> Handly_Dispatch_WithBehaviors()
		=> _dispatcherWithBehaviours.Dispatch(new HandlyPing());
}

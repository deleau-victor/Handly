using Handly.UnitTests.Behaviours;
using Handly.UnitTests.Handlers;
using Handly.UnitTests.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace Handly.UnitTests;

public class DispatcherTests
{
	private readonly IDispatcher _dispatcher;

	public DispatcherTests()
	{
		var services = new ServiceCollection();

		services.AddHandly(cfg =>
		{
			cfg.RegisterHandlerFromAssemblyContaining<PingHandler>();
			cfg.AddBehaviour(typeof(IPipelineBehavior<,>), typeof(TrackingBehaviour<,>));
		});

		_dispatcher = services.BuildServiceProvider().GetRequiredService<IDispatcher>();
	}

	[Fact]
	public async Task Dispatch_Should_ReturnPong()
	{
		var result = await _dispatcher.Dispatch(new Ping());
		Assert.Equal("Pong", result);
	}

	[Fact]
	public async Task Dispatch_Should_Invoke_Behavior_And_ReturnPong()
	{
		TrackingBehaviour<Ping, string>.Called = false;

		var result = await _dispatcher.Dispatch(new Ping());

		Assert.True(TrackingBehaviour<Ping, string>.Called);
		Assert.Equal("Pong", result);
	}
}

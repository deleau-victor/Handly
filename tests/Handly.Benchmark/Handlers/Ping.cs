namespace BenchmarkTests.Handlers;

public class MediatRPing : MediatR.IRequest<string> { }
public class MediatRPingHandler : MediatR.IRequestHandler<MediatRPing, string>
{
	public Task<string> Handle(MediatRPing request, CancellationToken cancellationToken)
	{
		return Task.FromResult("Pong");
	}
}

public class HandlyPing : Handly.IRequest<string> { }
public class HandlyPingHandler : Handly.IRequestHandler<HandlyPing, string>
{
	public ValueTask<string> Handle(HandlyPing request, CancellationToken cancellationToken)
	{
		return new("Pong");
	}
}

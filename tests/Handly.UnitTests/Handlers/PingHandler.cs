using Handly.UnitTests.Requests;

namespace Handly.UnitTests.Handlers;
public class PingHandler : IRequestHandler<Ping, string>
{
	public Task<string> Handle(Ping request, CancellationToken cancellationToken)
	{
		return Task.FromResult($"Pong");
	}
}

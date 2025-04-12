using Handly.UnitTests.Requests;

namespace Handly.UnitTests.Handlers;
public class PingHandler : IRequestHandler<Ping, string>
{
	public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
		=> new("Pong");
}

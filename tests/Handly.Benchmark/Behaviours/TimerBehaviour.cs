namespace BenchmarkTests.Behaviours;

public class HandlyTimerBehaviour<TRequest, TResponse> : Handly.IPipelineBehavior<TRequest, TResponse>
	where TRequest : Handly.IRequest<TResponse>
{
	public async Task<TResponse> Handle(TRequest request, Handly.RequestHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		var response = await next();
		_ = sw.ElapsedTicks; // just to simulate cost
		return response;
	}
}
public class MediatRTimerBehaviour<TRequest, TResponse> : MediatR.IPipelineBehavior<TRequest, TResponse>
	where TRequest : MediatR.IRequest<TResponse>
{
	public async Task<TResponse> Handle(TRequest request, MediatR.RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		var response = await next();
		_ = sw.ElapsedTicks; // just to simulate cost
		return response;
	}
}


namespace BenchmarkTests.Behaviours;

public class HandlyLoggingBehaviour<TRequest, TResponse> : Handly.IPipelineBehavior<TRequest, TResponse>
	where TRequest : Handly.IRequest<TResponse>
{
	public async ValueTask<TResponse> Handle(TRequest request, Handly.RequestHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
	{
		// No Console.WriteLine for benchmark purity
		return await next();
	}
}

public class MediatRLoggingBehaviour<TRequest, TResponse> : MediatR.IPipelineBehavior<TRequest, TResponse>
	where TRequest : MediatR.IRequest<TResponse>
{
	public async Task<TResponse> Handle(TRequest request, MediatR.RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
	{
		// No Console.WriteLine for benchmark purity
		return await next();
	}
}



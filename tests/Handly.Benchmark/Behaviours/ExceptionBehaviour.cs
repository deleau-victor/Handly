namespace BenchmarkTests.Behaviours;

public class HandlyExceptionBehaviour<TRequest, TResponse> : Handly.IPipelineBehavior<TRequest, TResponse>
	where TRequest : Handly.IRequest<TResponse>
{
	public async Task<TResponse> Handle(TRequest request, Handly.RequestHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
	{
		try
		{ return await next(); }
		catch { throw; } // pure wrap
	}
}
public class MediatRExceptionBehaviour<TRequest, TResponse> : MediatR.IPipelineBehavior<TRequest, TResponse>
	where TRequest : MediatR.IRequest<TResponse>
{
	public async Task<TResponse> Handle(TRequest request, MediatR.RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
	{
		try
		{ return await next(); }
		catch { throw; } // pure wrap
	}
}

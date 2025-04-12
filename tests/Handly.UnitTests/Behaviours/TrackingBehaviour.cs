namespace Handly.UnitTests.Behaviours;

public class TrackingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
	where TRequest : IRequest<TResponse>
{
	public static bool Called = false;

	public ValueTask<TResponse> Handle(
		TRequest request,
		RequestHandlerDelegate<TRequest, TResponse> next,
		CancellationToken cancellationToken)
	{
		Called = true;
		return next();
	}
}

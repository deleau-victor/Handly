namespace Handly;

public interface IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
	ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken);
}

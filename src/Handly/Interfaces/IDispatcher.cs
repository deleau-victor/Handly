namespace Handly;

public interface IDispatcher
{
	ValueTask<TResponse> Dispatch<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
	ValueTask<TResponse> Dispatch<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse>;
}

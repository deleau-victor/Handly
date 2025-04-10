namespace Handly.Interfaces;

public interface IDispatcher
{
	Task<TResponse> Dispatch<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
		where TRequest : IRequest<TResponse>;
}

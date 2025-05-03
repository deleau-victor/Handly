namespace Handly;

public interface IDispatcher
{
	Task<TResponse> Dispatch<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
}

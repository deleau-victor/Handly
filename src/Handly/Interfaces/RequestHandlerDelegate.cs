namespace Handly
{
	public delegate ValueTask<TResponse> RequestHandlerDelegate<TRequest, TResponse>()
		where TRequest : IRequest<TResponse>;
}

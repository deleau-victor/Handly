namespace Handly
{
	public delegate Task<TResponse> RequestHandlerDelegate<TRequest, TResponse>()
		where TRequest : IRequest<TResponse>;
}

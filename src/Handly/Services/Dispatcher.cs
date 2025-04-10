using Handly.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Handly.Services;

public class Dispatcher : IDispatcher
{
	private readonly IServiceProvider _sp;

	public Dispatcher(IServiceProvider sp) => _sp = sp;

	public async Task<TResponse> Dispatch<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
		where TRequest : IRequest<TResponse>
	{
		var handler = _sp.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

		// Construction du delegate final (appel au handler réel)
		RequestHandlerDelegate<TRequest, TResponse> handlerDelegate = () => handler.Handle(request, cancellationToken);

		// Récupère tous les comportements enregistrés pour ce couple générique
		IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors = _sp
			.GetServices<IPipelineBehavior<TRequest, TResponse>>()
			.Reverse();

		// Compose la chaîne des comportements en s'enroulant autour du handler
		foreach (var behavior in behaviors)
		{
			RequestHandlerDelegate<TRequest, TResponse> next = handlerDelegate;
			handlerDelegate = () => behavior.Handle(request, next, cancellationToken);
		}

		return await handlerDelegate();
	}
}

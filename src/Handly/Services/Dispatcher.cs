using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using Handly.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Handly;

public class Dispatcher : IDispatcher
{
	private readonly IServiceProvider _sp;
	private static readonly ConcurrentDictionary<Type, Func<object, IServiceProvider, CancellationToken, Task<object>>> _delegateCache = new();

	private static readonly MethodInfo GetRequiredServiceMethod =
		typeof(ServiceProviderServiceExtensions)
			.GetMethods()
			.Single(m => m.Name == nameof(ServiceProviderServiceExtensions.GetRequiredService)
						 && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);

	private static readonly MethodInfo GetServicesMethodInfo =
		typeof(ServiceProviderServiceExtensions)
			.GetMethods()
			.First(m => m.Name == nameof(ServiceProviderServiceExtensions.GetServices)
						&& m.IsGenericMethodDefinition && m.GetParameters().Length == 1);

	public Dispatcher(IServiceProvider sp) => _sp = sp;

	public Task<TResponse> Dispatch<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
	{
		var requestType = request.GetType();

		var del = _delegateCache.GetOrAdd(requestType, _ =>
			CreateHandlerDelegate(requestType, typeof(TResponse)));

		return del(request, _sp, cancellationToken).As<TResponse>();
	}

	private Func<object, IServiceProvider, CancellationToken, Task<object>> CreateHandlerDelegate(Type requestType, Type responseType)
	{
		var handlerInterfaceType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
		var behaviorInterfaceType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType);

		var methodInfo = handlerInterfaceType.GetMethod(nameof(IRequestHandler<IRequest<object>, object>.Handle))!;

		var requestParam = Expression.Parameter(typeof(object), "request");
		var spParam = Expression.Parameter(typeof(IServiceProvider), "sp");
		var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

		var typedRequest = Expression.Convert(requestParam, requestType);

		var handlerInstance = Expression.Convert(
			Expression.Call(GetRequiredServiceMethod.MakeGenericMethod(handlerInterfaceType), spParam),
			handlerInterfaceType);

		var handlerCall = Expression.Call(handlerInstance, methodInfo, typedRequest, ctParam);

		var getBehaviorsCall = Expression.Call(GetServicesMethodInfo.MakeGenericMethod(behaviorInterfaceType), spParam);

		var pipelineResult = BuildPipelineConditional(handlerCall, getBehaviorsCall, requestType, responseType, typedRequest, ctParam);

		var wrappedCall = Expression.Call(
			typeof(Dispatcher).GetMethod(nameof(WrapTask), BindingFlags.Static | BindingFlags.NonPublic)!
				.MakeGenericMethod(responseType),
			pipelineResult);

		return Expression
			.Lambda<Func<object, IServiceProvider, CancellationToken, Task<object>>>(
				wrappedCall, requestParam, spParam, ctParam).Compile();
	}

	private static Expression BuildPipelineConditional(
		Expression handlerCall,
		Expression getBehaviorsCall,
		Type requestType,
		Type responseType,
		Expression typedRequest,
		ParameterExpression ctParam)
	{
		var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType);
		var enumerableType = typeof(IEnumerable<>).MakeGenericType(behaviorType);
		var collectionType = typeof(ICollection<>).MakeGenericType(behaviorType);

		var pipelineVar = Expression.Variable(enumerableType, "behaviors");
		var assign = Expression.Assign(pipelineVar, getBehaviorsCall);

		var asCollection = Expression.TypeAs(pipelineVar, collectionType);
		var countProperty = Expression.Property(asCollection, "Count");
		var hasBehaviorsOptimized = Expression.GreaterThan(countProperty, Expression.Constant(0));

		var anyCall = Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), new[] { behaviorType }, pipelineVar);
		var condition = Expression.Condition(
			Expression.NotEqual(asCollection, Expression.Constant(null, collectionType)),
			hasBehaviorsOptimized,
			anyCall);

		var delegateType = typeof(RequestHandlerDelegate<,>).MakeGenericType(requestType, responseType);
		var handlerDelegate = Expression.Lambda(delegateType, handlerCall);

		var composeMethod = typeof(Dispatcher)
			.GetMethod(nameof(ComposePipeline), BindingFlags.Static | BindingFlags.NonPublic)!
			.MakeGenericMethod(requestType, responseType);

		var composedCall = Expression.Call(composeMethod, typedRequest, pipelineVar, handlerDelegate, ctParam);

		return Expression.Block(
			new[] { pipelineVar },
			assign,
			Expression.Condition(condition, composedCall, handlerCall)
		);
	}

	private static async Task<object> WrapTask<T>(Task<T> task) => await task;

	private static Task<TResponse> ComposePipeline<TRequest, TResponse>(
		TRequest request,
		IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors,
		RequestHandlerDelegate<TRequest, TResponse> handler,
		CancellationToken ct)
		where TRequest : IRequest<TResponse>
	{
		if (behaviors is IPipelineBehavior<TRequest, TResponse>[] array)
		{
			return BuildFromArray(array, request, handler, ct);
		}

		var list = new List<IPipelineBehavior<TRequest, TResponse>>();
		foreach (var behavior in behaviors)
		{
			list.Add(behavior);
		}

		return BuildFromArray(CollectionsMarshal.AsSpan(list), request, handler, ct);
	}

	private static Task<TResponse> BuildFromArray<TRequest, TResponse>(
		ReadOnlySpan<IPipelineBehavior<TRequest, TResponse>> behaviors,
		TRequest request,
		RequestHandlerDelegate<TRequest, TResponse> handler,
		CancellationToken ct)
		where TRequest : IRequest<TResponse>
	{
		RequestHandlerDelegate<TRequest, TResponse> next = handler;

		for (int i = behaviors.Length - 1; i >= 0; i--)
		{
			var behavior = behaviors[i];
			var current = next;
			next = () => behavior.Handle(request, current, ct);
		}

		return next();
	}
}

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Handly.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Handly;

public class Dispatcher : IDispatcher
{
	private readonly IServiceProvider _sp;
	private static readonly ConcurrentDictionary<Type, Func<object, IServiceProvider, CancellationToken, ValueTask<object>>> _delegateCache = new();
	private static readonly MethodInfo GetRequiredServiceMethod =
		typeof(ServiceProviderServiceExtensions)
			.GetMethods()
			.Single(m => m.Name == nameof(ServiceProviderServiceExtensions.GetRequiredService)
						 && m.IsGenericMethodDefinition
						 && m.GetParameters().Length == 1);
	private static readonly MethodInfo GetServicesMethodInfo =
		typeof(ServiceProviderServiceExtensions)
			.GetMethods()
			.First(m => m.Name == nameof(ServiceProviderServiceExtensions.GetServices)
				&& m.IsGenericMethodDefinition && m.GetParameters().Length == 1);

	public Dispatcher(IServiceProvider sp) => _sp = sp;

	public ValueTask<TResponse> Dispatch<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
	{
		var requestType = request.GetType();

		var del = _delegateCache.GetOrAdd(requestType, _ =>
			CreateHandlerDelegate(requestType, typeof(TResponse)));

		return del(request, _sp, cancellationToken).As<TResponse>();
	}

	public ValueTask<TResponse> Dispatch<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
		where TRequest : IRequest<TResponse>
		=> _sp.GetRequiredService<IRequestHandler<TRequest, TResponse>>().Handle(request, cancellationToken);

	/// <summary>
	/// Create the delegate that will call the handler and compose the pipeline of behaviors.
	/// </summary>
	private Func<object, IServiceProvider, CancellationToken, ValueTask<object>> CreateHandlerDelegate(Type requestType, Type responseType)
	{
		// Build the type of the handler and behavior interfaces
		var handlerInterfaceType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
		var behaviorInterfaceType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType);

		// Get the handle method on the handler
		var methodInfo = handlerInterfaceType.GetMethod(nameof(IRequestHandler<IRequest<object>, object>.Handle))!;

		// Define the parameters for the lambda
		var requestParam = Expression.Parameter(typeof(object), "request");
		var spParam = Expression.Parameter(typeof(IServiceProvider), "sp");
		var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

		// Convert the request parameter to the request type
		var typedRequest = Expression.Convert(requestParam, requestType);

		// Get the handler instance from the service provider
		var handlerInstance = Expression.Convert(
			Expression.Call(GetRequiredServiceMethod.MakeGenericMethod(handlerInterfaceType), spParam),
			handlerInterfaceType);

		// call of handler.Handle(typedRequest, ct)
		var handlerCall = Expression.Call(handlerInstance, methodInfo, typedRequest, ctParam);

		// Get behaviors from the service provider
		var getBehaviorsCall = Expression.Call(GetServicesMethodInfo.MakeGenericMethod(behaviorInterfaceType), spParam);

		// build of the conditional pipeline
		// If there are behaviors, compose the pipeline
		// else call the handler directly
		var pipelineResult = BuildPipelineConditional(handlerCall, getBehaviorsCall, requestType, responseType, typedRequest, ctParam);

		// Wrap in ValueTask<object>
		var wrappedCall = Expression.Call(
			typeof(Dispatcher)
				.GetMethod(nameof(WrapValueTask), BindingFlags.Static | BindingFlags.NonPublic)!
				.MakeGenericMethod(responseType),
			pipelineResult);

		return Expression.Lambda<Func<object, IServiceProvider, CancellationToken, ValueTask<object>>>(
			wrappedCall, requestParam, spParam, ctParam).Compile();
	}

	/// <summary>
	/// Build the conditional branch for the pipeline
	/// Replaces the call to Enumerable.Any with a direct check on the Count property if possible
	/// </summary>
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

		// Vars to store the behaviors
		var pipelineVar = Expression.Variable(enumerableType, "behaviors");

		var assign = Expression.Assign(pipelineVar, getBehaviorsCall);

		// Try to cast to ICollection to access Count
		var asCollection = Expression.TypeAs(pipelineVar, collectionType);
		var countProperty = Expression.Property(asCollection, "Count");
		var hasBehaviorsOptimized = Expression.GreaterThan(countProperty, Expression.Constant(0));

		// Fallback: if the cast fails, use Enumerable.Any
		var anyCall = Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), new[] { behaviorType }, pipelineVar);
		var condition = Expression.Condition(
			Expression.NotEqual(asCollection, Expression.Constant(null, collectionType)),
			hasBehaviorsOptimized,
			anyCall);

		// Create the delegate representing the base handler
		var delegateType = typeof(RequestHandlerDelegate<,>).MakeGenericType(requestType, responseType);
		var handlerDelegate = Expression.Lambda(delegateType, handlerCall);

		var composeMethod = typeof(Dispatcher).GetMethod(nameof(ComposePipeline), BindingFlags.Static | BindingFlags.NonPublic)!
			.MakeGenericMethod(requestType, responseType);

		var composedCall = Expression.Call(composeMethod, typedRequest, pipelineVar, handlerDelegate, ctParam);

		return Expression.Block(
			new[] { pipelineVar },
			assign,
			Expression.Condition(condition, composedCall, handlerCall)
		);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static async ValueTask<object> WrapValueTask<T>(ValueTask<T> task)
	{
		return await task;
	}

	/// <summary>
	/// Compose the pipeline of behaviors.
	/// This method is called when there are behaviors to apply.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ValueTask<TResponse> ComposePipeline<TRequest, TResponse>(
		TRequest request,
		IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors,
		RequestHandlerDelegate<TRequest, TResponse> handler,
		CancellationToken ct)
		where TRequest : IRequest<TResponse>
	{
		// If the behaviors are already an array, we can use it directly
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

	/// <summary>
	/// Imbrication of behaviors with reduced allocations thanks to ReadOnlySpan.
	/// </summary>
	private static ValueTask<TResponse> BuildFromArray<TRequest, TResponse>(
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

	/// <summary>
	/// Warm up the dispatcher by dispatching a dummy request for each request type.
	/// </summary>
	public static void WarmUp(IDispatcher dispatcher, IEnumerable<Type> requestTypes)
	{
		foreach (var type in requestTypes)
		{
			if (Activator.CreateInstance(type) is IRequest<object> dummyRequest)
			{
				_ = dispatcher.Dispatch(dummyRequest);
			}
		}
	}
}

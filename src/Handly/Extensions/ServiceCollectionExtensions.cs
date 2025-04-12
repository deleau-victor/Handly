using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Handly;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddHandly(this IServiceCollection services, Action<HandlyDispatcherConfig> configure)
	{
		var config = new HandlyDispatcherConfig();
		configure(config);

		if (config.Assemblies.Count == 0)
		{
			config.RegisterHandlerFromAssemblyContaining<IDispatcher>();
		}

		AddScanResults(services, config);
		AddBehaviours(services, config);

		services.TryAddTransient<IDispatcher, Dispatcher>();

		return services;
	}

	private static IServiceCollection AddScanResults(
	   this IServiceCollection services,
	   HandlyDispatcherConfig config)
	{
		foreach (var assembly in config.Assemblies)
		{
			var handlers = assembly.GetTypes()
			   .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
			   .SelectMany(t => t.GetInterfaces(), (t, i) => new { HandlerType = t, InterfaceType = i })
			   .Where(x => x.InterfaceType.IsGenericType && x.InterfaceType.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
			   .ToList();

			foreach (var handler in handlers)
			{
				Type handlerInterface = handler.InterfaceType;
				Type handlerType = handler.HandlerType;
				var serviceDescriptor = ServiceDescriptor.Describe(handlerInterface, handlerType, config.Lifetime);
				services.Add(serviceDescriptor);
			}
		}

		return services;
	}

	private static IServiceCollection AddBehaviours(
	   this IServiceCollection services,
	   HandlyDispatcherConfig config)
	{
		foreach (ServiceDescriptor behaviour in config.Behaviours)
		{
			services.Add(behaviour);
		}

		return services;
	}
}

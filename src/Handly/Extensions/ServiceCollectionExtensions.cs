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
			   .Where(t => t.IsClass && !t.IsAbstract)
			   .SelectMany(t => t.GetInterfaces(), (t, i) => new { Implementation = t, Interface = i })
			   .Where(x => x.Interface.IsGenericType && x.Interface.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
			   .ToList();

			foreach (var handler in handlers)
			{
				Type handlerInterface = handler.Interface;
				Type handlerImplementation = handler.Implementation;
				var serviceDescriptor = ServiceDescriptor.Describe(handlerInterface, handlerImplementation, config.Lifetime);
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

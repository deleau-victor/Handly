using Microsoft.Extensions.DependencyInjection;

namespace Handly;

public static class HandlyDispatcherConfigExtensions
{
	/// <summary>
	/// Registers a behaviour type with the Handly dispatcher configuration.
	/// The behaviour type must implement the IPipelineBehavior interface.
	/// The service type must be a generic type definition.
	/// The implementation type must be a non-abstract class.
	/// </summary>
	/// <param name="implementationType">The implementation type of the behaviour.</param>
	public static void AddBehaviour(this HandlyDispatcherConfig config, Type serviceType, Type implementationType)
	{
		if (!serviceType.IsGenericTypeDefinition)
		{
			throw new ArgumentException("Service type must be a generic type definition.");
		}

		if (!implementationType.IsClass || implementationType.IsAbstract)
		{
			throw new ArgumentException("Implementation type must be a non-abstract class.");
		}

		var descriptor = ServiceDescriptor.Describe(serviceType, implementationType, config.Lifetime);
		config.Behaviours.Add(descriptor);
	}
}

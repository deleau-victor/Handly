using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Handly;

public class HandlyDispatcherConfig
{
	public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;
	internal List<Assembly> Assemblies { get; } = [];
	internal List<ServiceDescriptor> Behaviours { get; } = [];

	public HandlyDispatcherConfig RegisterHandlerFromAssembly(Assembly assembly)
	{
		Assemblies.Add(assembly);
		return this;
	}
	public HandlyDispatcherConfig RegisterHandlerFromAssemblies(params Assembly[] assemblies)
	{
		Assemblies.AddRange(assemblies);
		return this;
	}
	public HandlyDispatcherConfig RegisterHandlerFromAssemblyContaining<T>()
	{
		var assembly = typeof(T).Assembly;
		Assemblies.Add(assembly);
		return this;
	}

	public HandlyDispatcherConfig RegisterHandlerFromAssemblyContaining(Type type)
	{
		var assembly = type.Assembly;
		Assemblies.Add(assembly);
		return this;
	}
}

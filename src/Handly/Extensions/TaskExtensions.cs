namespace Handly.Extensions;

public static class TaskExtensions
{
	public static async Task<T> As<T>(this Task<object> task) => (T)await task;
}

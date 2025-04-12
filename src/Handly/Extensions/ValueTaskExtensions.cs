namespace Handly.Extensions;

public static class ValueTaskExtensions
{
	public static async ValueTask<T> As<T>(this ValueTask<object> task) => (T)await task;
}

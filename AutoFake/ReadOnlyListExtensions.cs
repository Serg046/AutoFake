using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AutoFake
{
#if NET40
	public interface IReadOnlyCollection<out T> : IEnumerable<T>, IEnumerable
	{
		int Count { get; }
	}

	public interface IReadOnlyList<out T> : IReadOnlyCollection<T>
	{
		T this[int index] { get; }
	}

	public class ReadOnlyList<T> : List<T>, IReadOnlyList<T>
	{
		public ReadOnlyList(IEnumerable<T> items) : base(items)
		{
		}
	}
#endif

	internal static class ReadOnlyListExtensions
	{
		public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> items)
		{
#if NET40
			return new ReadOnlyList<T>(items);
#else
			return items is List<T> list ? list : items.ToList();
#endif
		}
	}
}

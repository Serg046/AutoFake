﻿using System.Collections.Generic;
using System.Linq;

#if NET40
namespace System.Collections.Generic
{
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
}
#endif

namespace AutoFake
{
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

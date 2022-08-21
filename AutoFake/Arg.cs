using AutoFake.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AutoFake
{
#pragma warning disable AF0001 // Public by design
	public static class Arg
#pragma warning restore AF0001
	{
		public static object IsNull<T>()
		{
			var type = typeof(T);
			if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
			{
				throw new InvalidOperationException("Value type instance cannot be null");
			}

#pragma warning disable DI0002 // There is no way to invert control here as it is called from the client side
			return new TypeWrapper(type);
#pragma warning restore DI0002
		}

		//Used by expression's engine, see GetArgumentsMemberVisitor::GetArgument(MethodCallExpression expression)
		[ExcludeFromCodeCoverage]
		public static T Is<T>(Func<T, bool> checkArgumentFunc) => IsAny<T>();

		[ExcludeFromCodeCoverage]
		public static T Is<T>(T argument, IFakeArgumentChecker.Comparer<T> comparer) => IsAny<T>();

		public static T IsAny<T>() => default!;

		[ExcludeFromCodeCoverage]
		internal class TypeWrapper
		{
			public TypeWrapper(Type type) => Type = type;
			public Type Type { get; }
		}
	}
}

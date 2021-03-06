﻿using System;
using System.Collections.Generic;

namespace AutoFake
{
    public static class Arg
    {
        public static FakeDependency IsNull<T>()
        {
            var type = typeof(T);
            if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
                throw new NotSupportedException("Value type instance cannot be null");

            return new FakeDependency(type, null);
        }

        //Used by expression's engine, see GetArgumentsMemberVisitor::GetArgument(MethodCallExpression expression)
        public static T Is<T>(Func<T, bool> checkArgumentFunc) => IsAny<T>();

        public static T Is<T>(T argument, IEqualityComparer<T> comparer) => IsAny<T>();

        public static T IsAny<T>() => default;
    }
}

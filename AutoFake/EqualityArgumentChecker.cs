using System.Collections;
using System.Collections.Generic;
using AutoFake.Abstractions;

namespace AutoFake
{
    internal class EqualityArgumentChecker : IFakeArgumentChecker
    {
        private readonly object _value;
        private readonly IEqualityComparer _comparer;

        public EqualityArgumentChecker(object value, IEqualityComparer? comparer = null)
        {
            _value = value;
            _comparer = comparer ?? EqualityComparer<object>.Default;
        }

        public bool Check(object argument) => _comparer.Equals(_value, argument);

        public override string ToString() => ToString(_value);

        public override int GetHashCode() => _comparer.GetHashCode(_value);

        public static string ToString(object value)
        {
            return value is string str
                ? $"\"{str}\""
                : value?.ToString() ?? "null";
        }
    }
}

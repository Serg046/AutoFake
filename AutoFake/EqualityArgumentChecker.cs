using System.Collections;
using System.Linq;

namespace AutoFake
{
    internal class EqualityArgumentChecker : IFakeArgumentChecker
    {
        private readonly object _value;
        private readonly IEqualityComparer _comparer;

        public EqualityArgumentChecker(object value, IEqualityComparer comparer = null)
        {
            _value = value;
            _comparer = comparer ?? TryGetEnumerableComparer(value) ?? new DefaultEqualityComparer();
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

        IEqualityComparer TryGetEnumerableComparer(object value)
            => value is IEnumerable ? new EnumerableEqualityComparer() : null;

        private class EnumerableEqualityComparer : IEqualityComparer
        {
            public bool Equals(object x, object y)
            {
                if (x == null && y == null) return true;
                return x is IEnumerable firstEnumerable && y is IEnumerable secondEnumerable 
                    && firstEnumerable.Cast<object>().SequenceEqual(secondEnumerable.Cast<object>());
            }

            public int GetHashCode(object obj) => obj?.GetHashCode() ?? 0;
        }

        private class DefaultEqualityComparer : IEqualityComparer
        {
            public bool Equals(object x, object y)
            {
                if (x != null)
                {
                    if (y != null)
                    {
                        return x.Equals(y);
                    }
                }
                else if (y == null)
                {
                    return true;
                }

                return false;
            }

            public int GetHashCode(object obj) => obj?.GetHashCode() ?? 0;
        }
    }
}

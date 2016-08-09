using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoFake
{
    public class FakeSetup<T, TReturn>
    {
        private readonly Fake<T> _fake;
        private readonly MethodInfo _method;
        private readonly List<MethodInfo> _reachableWithCollection;

        internal FakeSetup(Fake<T> fake, MethodInfo method)
        {
            _fake = fake;
            _method = method;
            _reachableWithCollection = new List<MethodInfo>();
        }

        public Fake<T> Returns(TReturn returnObject)
        {
            if (_reachableWithCollection.Count == 0)
                throw new InvalidOperationException($"Please call {nameof(ReachableWith)}() method");
            _fake.Setups.Add(new FakeSetupPack(_method, returnObject, _reachableWithCollection));
            return _fake;
        }

        public FakeSetup<T, TReturn> ReachableWith(Expression<Func<T, object>> reachableFunc)
        {
            _reachableWithCollection.Add(ExpressionUtils.GetMethodInfo(reachableFunc));
            return this;
        }
    }
}

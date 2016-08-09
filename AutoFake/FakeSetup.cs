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
        private readonly object[] _setupArguments;
        private bool _verifiable;

        internal FakeSetup(Fake<T> fake, MethodInfo method, object[] setupArguments)
        {
            _fake = fake;
            _method = method;
            _reachableWithCollection = new List<MethodInfo>();
            _setupArguments = setupArguments;
        }

        public Fake<T> Returns(TReturn returnObject)
        {
            if (_reachableWithCollection.Count == 0)
                throw new InvalidOperationException($"Please call {nameof(ReachableWith)}() method");
            _fake.Setups.Add(new FakeSetupPack()
            {
                Method = _method,
                ReturnObject = returnObject,
                ReachableWithCollection = _reachableWithCollection,
                IsVerifiable = _verifiable,
                SetupArguments = _setupArguments
            });
            return _fake;
        }

        public FakeSetup<T, TReturn> ReachableWith(Expression<Func<T, object>> reachableFunc)
        {
            _reachableWithCollection.Add(ExpressionUtils.GetMethodInfo(reachableFunc));
            return this;
        }

        public FakeSetup<T, TReturn> Verifiable()
        {
            if (_setupArguments.Length == 0)
                throw new InvalidOperationException("Setup expression must contain a method with parameters");
            _verifiable = true;
            return this;
        }
    }
}

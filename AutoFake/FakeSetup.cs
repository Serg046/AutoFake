using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoFake
{
    public class FakeSetup<T, TReturn>
    {
        private readonly Fake<T> _fake;

        private readonly FakeSetupPack _fakeSetupPack;

        internal FakeSetup(Fake<T> fake, MethodInfo method, object[] setupArguments)
        {
            _fake = fake;

            _fakeSetupPack = new FakeSetupPack()
            {
                Method = method,
                SetupArguments = setupArguments,
                ReachableWithCollection = new List<MethodInfo>(),
                ExpectedCallsCount = -1
            };
        }

        public Fake<T> Returns(TReturn returnObject)
        {
            if (_fakeSetupPack.ReachableWithCollection.Count == 0)
                throw new InvalidOperationException($"Please call {nameof(ReachableWith)}() method");
            _fakeSetupPack.ReturnObject = returnObject;
            _fake.Setups.Add(_fakeSetupPack);
            return _fake;
        }

        public FakeSetup<T, TReturn> ReachableWith(Expression<Func<T, object>> reachableFunc)
        {
            _fakeSetupPack.ReachableWithCollection.Add(ExpressionUtils.GetMethodInfo(reachableFunc));
            return this;
        }

        public FakeSetup<T, TReturn> Verifiable()
        {
            if (_fakeSetupPack.IsVerifiable)
                throw new InvalidOperationException("Verifiable() is already called");
            if (_fakeSetupPack.SetupArguments.Length == 0)
                throw new InvalidOperationException("Setup expression must contain a method with parameters");
            _fakeSetupPack.IsVerifiable = true;
            return this;
        }

        public FakeSetup<T, TReturn> ExpectedCallsCount(int expectedCallsCount)
        {
            if (expectedCallsCount < 1)
                throw new InvalidOperationException("ExpectedCallsCount must be greater than 0");
            _fakeSetupPack.ExpectedCallsCount = expectedCallsCount;
            return this;
        }
    }
}

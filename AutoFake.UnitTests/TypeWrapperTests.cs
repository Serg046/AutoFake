using System;
using Xunit;

namespace AutoFake.UnitTests
{
    public class TypeWrapperTests
    {
        [Fact]
        public void Execute_Action_Executed()
        {
            var fakeObjectInfo = new FakeObjectInfo(new object[0], typeof(TestType), new TestType());
            var typeWrapper = new TypeWrapper(fakeObjectInfo);

            Assert.Throws<NotImplementedException>(() => typeWrapper.Execute((TestType t) => t.Method()));
        }

        [Fact]
        public void Execute_Func_Executed()
        {
            var fakeObjectInfo = new FakeObjectInfo(new object[0], typeof(TestType), new TestType());
            var typeWrapper = new TypeWrapper(fakeObjectInfo);

            const int expectedValue = 7;
            Assert.Equal(expectedValue, typeWrapper.Execute((TestType t) => t.GetValue(expectedValue)));
        }

        private class TestType
        {
            public void Method() => throw new NotImplementedException();
            public int GetValue(int value) => value;
        }
    }
}

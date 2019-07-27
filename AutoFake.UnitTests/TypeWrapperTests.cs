using System;
using System.Collections.Generic;
using Xunit;

namespace AutoFake.UnitTests
{
    public class TypeWrapperTests
    {
        [Fact]
        public void Execute_Action_Executed()
        {
            var fakeObjectInfo = new FakeObjectInfo(new object[0], typeof(StaticClass));
            var typeWrapper = new TypeWrapper(fakeObjectInfo);

            Assert.Throws<NotImplementedException>(() => typeWrapper.Execute(() => StaticClass.Method()));
        }

        [Fact]
        public void Execute_Func_Executed()
        {
            var fakeObjectInfo = new FakeObjectInfo(new object[0], typeof(StaticClass));
            var typeWrapper = new TypeWrapper(fakeObjectInfo);

            const int expectedValue = 7;
            Assert.Equal(expectedValue, typeWrapper.Execute(() => StaticClass.GetValue(expectedValue)));
        }

        private static class StaticClass
        {
            public static void Method() => throw new NotImplementedException();
            public static int GetValue(int value) => value;
        }
    }
}

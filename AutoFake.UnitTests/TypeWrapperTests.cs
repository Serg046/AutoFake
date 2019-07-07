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
            var generatedObject = new GeneratedObject(new TypeInfo(typeof(StaticClass), new List<FakeDependency>()));
            var typeWrapper = new TypeWrapper(generatedObject);

            Assert.Throws<NotImplementedException>(() => typeWrapper.Execute(() => StaticClass.Method()));
        }

        [Fact]
        public void Execute_Func_Executed()
        {
            var generatedObject = new GeneratedObject(new TypeInfo(typeof(StaticClass), new List<FakeDependency>()));
            var typeWrapper = new TypeWrapper(generatedObject);

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

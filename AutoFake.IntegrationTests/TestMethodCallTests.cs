using System.Threading.Tasks;
using Xunit;

namespace AutoFake.IntegrationTests
{
    public class TestMethodCallTests
    {
        private class TestClass
        {
            public static int DynamicValueProp => 5;

            public int DynamicValue => DynamicValueProp;
            public static int StaticDynamicValue => DynamicValueProp;
            public int GetDynamicValue() => DynamicValueProp;
            public static int GetStaticDynamicValue() => DynamicValueProp;

            public async Task<int> GetDynamicValueAsync()
            {
                await Task.Delay(1);
                var value = DynamicValueProp;
                await Task.Delay(1);
                return value;
            }

            public static async Task<int> GetStaticDynamicValueAsync()
            {
                await Task.Delay(1);
                var value = DynamicValueProp;
                await Task.Delay(1);
                return value;
            }
        }

        private readonly Fake<TestClass> _fake;

        public TestMethodCallTests()
        {
            _fake = new Fake<TestClass>();
            _fake.Replace(() => TestClass.DynamicValueProp).Returns(7);
        }

        [Fact]
        public void InstancePropertyCallTest()
        {
            Assert.Equal(7, _fake.Execute(f => f.DynamicValue));
        }

        [Fact]
        public void StaticPropertyCallTest()
        {
            Assert.Equal(7, _fake.Execute(() => TestClass.StaticDynamicValue));
        }

        [Fact]
        public void InstanceMethodCallTest()
        {
            Assert.Equal(7, _fake.Execute(f => f.GetDynamicValue()));
        }

        [Fact]
        public void StaticMethodCallTest()
        {
            Assert.Equal(7, _fake.Execute(() => TestClass.GetStaticDynamicValue()));
        }

        [Fact]
        public async void InstanceAsyncMethodCallTest()
        {
            Assert.Equal(7, await _fake.Execute(f => f.GetDynamicValueAsync()));
        }

        [Fact]
        public async void StaticAsyncMethodCallTest()
        {
            Assert.Equal(7, await _fake.Execute(() => TestClass.GetStaticDynamicValueAsync()));
        }
    }
}

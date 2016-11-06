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
            Assert.Equal(7, _fake.Rewrite(f => f.DynamicValue).Execute());
        }

        [Fact]
        public void StaticPropertyCallTest()
        {
            Assert.Equal(7, _fake.Rewrite(() => TestClass.StaticDynamicValue).Execute());
        }

        [Fact]
        public void InstanceMethodCallTest()
        {
            Assert.Equal(7, _fake.Rewrite(f => f.GetDynamicValue()).Execute());
        }

        [Fact]
        public void StaticMethodCallTest()
        {
            Assert.Equal(7, _fake.Rewrite(() => TestClass.GetStaticDynamicValue()).Execute());
        }

        [Fact]
        public async void InstanceAsyncMethodCallTest()
        {
            Assert.Equal(7, await _fake.Rewrite(f => f.GetDynamicValueAsync()).Execute());
        }

        [Fact]
        public async void StaticAsyncMethodCallTest()
        {
            Assert.Equal(7, await _fake.Rewrite(() => TestClass.GetStaticDynamicValueAsync()).Execute());
        }
    }
}

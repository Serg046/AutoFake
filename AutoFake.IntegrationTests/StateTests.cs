using Xunit;

namespace AutoFake.IntegrationTests
{
    public class StateTests
    {
        [Fact]
        public void GetValueTest()
        {
            var fake = new Fake<TestClass>();

            Assert.Equal(7, fake.Execute(f => f.Field));
            Assert.Equal(7, fake.Execute(f => f.Property));
            Assert.Equal(7, fake.Execute(f => f.Method()));
        }

        [Fact]
        public void SetValueTest()
        {
            var fake = new Fake<TestClass>();
            fake.Execute();

            fake.SetValue(f => f.Field, 5);
            fake.SetValue(f => f.Property, 5);

            Assert.Equal(5, fake.Execute(f => f.Field));
            Assert.Equal(5, fake.Execute(f => f.Property));
        }

        private class TestClass
        {
            public int Field = 7;
            public int Property { get; set; } = 7;
            public int Method() => 7;
        }
    }
}

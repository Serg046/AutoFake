using System;
using Xunit;

namespace AutoFake.IntegrationTests
{
    public class StateTests
    {
        private class TestClass
        {
            public int Field1 = 0;
            public int Prop2 { get; private set; } = 0;
            public DateTime Prop3 { get; private set; }

            public void SetState()
            {
                Field1 = 7;
                Prop2 = 7;
                Prop3 = DateTime.Now;
            }
        }

        [Fact]
        public void GetStateValueTest()
        {
            var fake = new Fake<TestClass>();
            fake.Replace(() => DateTime.Now).Returns(DateTime.MinValue);
            fake.Execute(f => f.SetState());

            Assert.Equal(7, fake.GetStateValue(f => f.Field1));
            Assert.Equal(7, fake.GetStateValue(f => f.Prop2));
            Assert.Equal(DateTime.MinValue, fake.GetStateValue(f => f.Prop3));
        }

        [Fact]
        public void DoubleTestMethodInvocationTest()
        {
            var fake = new Fake<TestClass>();
            fake.Execute(f => f.SetState());

            Assert.Throws<InvalidOperationException>(() => fake.Execute(f => f.SetState()));

            fake.ResetSetups();

            fake.Execute(f => f.SetState());
        }
    }
}

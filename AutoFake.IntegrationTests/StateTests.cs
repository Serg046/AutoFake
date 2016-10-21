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

            public void StateValueTest()
            {
                if (Field1 != -1)
                    Field1 = 7;
                if (Prop2 != -1)
                    Prop2 = 7;
                Prop3 = DateTime.Now;
            }
        }

        [Fact]
        public void GetStateValueTest()
        {
            var fake = new Fake<TestClass>();
            fake.Replace(() => DateTime.Now).Returns(DateTime.MinValue);
            fake.Execute(f => f.StateValueTest());

            Assert.Equal(7, fake.GetStateValue(f => f.Field1));
            Assert.Equal(7, fake.GetStateValue(f => f.Prop2));
            Assert.Equal(DateTime.MinValue, fake.GetStateValue(f => f.Prop3));
        }

        [Fact]
        public void DoubleTestMethodInvocationTest()
        {
            var fake = new Fake<TestClass>();
            fake.Execute(f => f.StateValueTest());

            Assert.Throws<InvalidOperationException>(() => fake.Execute(f => f.StateValueTest()));

            fake.ClearState();

            fake.Execute(f => f.StateValueTest());
        }

        [Fact]
        public void SetStateValueAfterGeneratingTest()
        {
            var fake = new Fake<TestClass>();
            fake.Execute(f => f.StateValueTest());
            fake.SetStateValue(f => f.Field1, -1);
            fake.SetStateValue(f => f.Prop2, -1);

            Assert.Equal(-1, fake.GetStateValue(f => f.Field1));
            Assert.Equal(-1, fake.GetStateValue(f => f.Prop2));
        }

        [Fact]
        public void SetStateValueBeforeGeneratingTest()
        {
            var fake = new Fake<TestClass>();
            fake.SetStateValue(f => f.Field1, -1);
            fake.SetStateValue(f => f.Prop2, -1);
            fake.Execute(f => f.StateValueTest());

            Assert.Equal(-1, fake.GetStateValue(f => f.Field1));
            Assert.Equal(-1, fake.GetStateValue(f => f.Prop2));
        }
    }
}

using System;
using System.Linq.Expressions;
using Xunit;

namespace AutoFake.UnitTests
{
    public class ExecutorTests
    {
        [Fact]
        public void Execute_Action_Execute()
        {
            Expression<Action<TestClass>> expr = t => t.FailingMethod();
            var fake = new Fake<TestClass>();
            var executor = new Executor(fake, new AutoFake.Expression.InvocationExpression(expr));

            Assert.Throws<NotImplementedException>(() => executor.Execute());
        }

        [Fact]
        public void Execute_Func_Execute()
        {
            Expression<Func<TestClass, int>> expr = t => t.IntMethod();
            var fake = new Fake<TestClass>();
            var executor = new Executor<int>(fake, new AutoFake.Expression.InvocationExpression(expr));

            Assert.Equal(5, executor.Execute());
        }

        private class TestClass
        {
            public void FailingMethod() => throw new NotImplementedException();
            public int IntMethod() => 5;
        }
    }
}

using System;
using System.Linq.Expressions;
using FluentAssertions;
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

        [Fact]
        public void Execute_DifferentCompiletimeTypesCast_Throws()
        {
	        Expression<Func<TestClass, TestClass>> expr = t => t.TestClassMethod();
	        var fake = new Fake<TestClass>();

	        var executor = new Executor<TestClass>(fake, new AutoFake.Expression.InvocationExpression(expr));
	        Action act = () => executor.Execute();

	        act.Should().Throw<InvalidCastException>().WithMessage("*must be processed by Rewrite*");
        }

        [Fact]
        public void Execute_DifferentRuntimeTypesCast_Throws()
        {
	        Expression<Func<TestClass, TestClass>> expr = t => t.TestClassFailingMethod();
	        var fake = new Fake<TestClass>();
	        fake.Rewrite(f => f.TestClassFailingMethod());

	        var executor = new Executor<TestClass>(fake, new AutoFake.Expression.InvocationExpression(expr));
	        Action act = () => executor.Execute();

	        act.Should().Throw<InvalidCastException>().WithMessage("*Cannot cast \"this\" reference to*");
        }

        [Fact]
        public void Execute_CompatibleRuntimeTypesCast_Throws()
        {
	        Expression<Func<TestClass, TestClass>> expr = t => t.TestClassMethod();
	        var fake = new Fake<TestClass>();
	        fake.Rewrite(f => f.TestClassMethod());

	        var executor = new Executor<TestClass>(fake, new AutoFake.Expression.InvocationExpression(expr));
	        Action act = () => executor.Execute();

	        act.Should().NotThrow();
        }

        public class TestClass
        {
            public void FailingMethod() => throw new NotImplementedException();
            public int IntMethod() => 5;
            public TestClass TestClassMethod() => new TestClass();
            public TestClass TestClassFailingMethod() => this;
        }
    }
}

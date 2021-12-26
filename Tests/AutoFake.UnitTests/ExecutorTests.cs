using System;
using System.Linq.Expressions;
using AutoFake.Expression;
using FluentAssertions;
using Moq;
using Xunit;

namespace AutoFake.UnitTests
{
    public class ExecutorTests
    {
        [Theory, AutoMoqData]
        internal void Execute_Action_Execute(Mock<IMemberVisitorFactory> factory)
        {
            Expression<Action<TestClass>> expr = t => t.FailingMethod();
            var fake = new Fake<TestClass>();
	        factory.Setup(f => f.GetValueMemberVisitor(It.IsAny<object>())).Returns((object arg) => new GetValueMemberVisitor(arg));
	        factory.Setup(f => f.GetTargetMemberVisitor(It.IsAny<IMemberVisitor>(), It.IsAny<Type>()))
		        .Returns((IMemberVisitor visitor, Type type) => new TargetMemberVisitor(visitor, type));
            
	        var executor = new Executor(fake, new AutoFake.Expression.InvocationExpression(factory.Object, expr), factory.Object);

            Assert.Throws<NotImplementedException>(() => executor.Execute());
        }

        [Theory, AutoMoqData]
        internal void Execute_Func_Execute(Mock<IMemberVisitorFactory> factory)
        {
            Expression<Func<TestClass, int>> expr = t => t.IntMethod();
            var fake = new Fake<TestClass>();
	        factory.Setup(f => f.GetValueMemberVisitor(It.IsAny<object>())).Returns((object arg) => new GetValueMemberVisitor(arg));
	        factory.Setup(f => f.GetTargetMemberVisitor(It.IsAny<IMemberVisitor>(), It.IsAny<Type>()))
		        .Returns((IMemberVisitor visitor, Type type) => new TargetMemberVisitor(visitor, type));

            var executor = new Executor<int>(fake, new AutoFake.Expression.InvocationExpression(factory.Object, expr), factory.Object);

            Assert.Equal(5, executor.Execute());
        }

        [Theory, AutoMoqData]
        internal void Execute_DifferentCompileTimeTypesCast_Throws(Mock<IMemberVisitorFactory> factory)
        {
	        Expression<Func<TestClass, TestClass>> expr = t => t.TestClassMethod();
	        var fake = new Fake<TestClass>();
	        factory.Setup(f => f.GetValueMemberVisitor(It.IsAny<object>())).Returns((object arg) => new GetValueMemberVisitor(arg));
	        factory.Setup(f => f.GetTargetMemberVisitor(It.IsAny<IMemberVisitor>(), It.IsAny<Type>()))
		        .Returns((IMemberVisitor visitor, Type type) => new TargetMemberVisitor(visitor, type));

            var executor = new Executor<TestClass>(fake, new AutoFake.Expression.InvocationExpression(factory.Object, expr), factory.Object);
	        Action act = () => executor.Execute();

	        act.Should().Throw<InvalidCastException>().WithMessage("*must be processed by Rewrite*");
        }

        [Theory, AutoMoqData]
        internal void Execute_DifferentRuntimeTypesCast_Throws(Mock<IMemberVisitorFactory> factory)
        {
	        Expression<Func<TestClass, TestClass>> expr = t => t.TestClassFailingMethod();
	        var fake = new Fake<TestClass>();
	        fake.Rewrite(f => f.TestClassFailingMethod());
	        factory.Setup(f => f.GetMemberVisitor<GetTestMethodVisitor>()).Returns(new GetTestMethodVisitor());
	        factory.Setup(f => f.GetValueMemberVisitor(It.IsAny<object>())).Returns((object arg) => new GetValueMemberVisitor(arg));
	        factory.Setup(f => f.GetTargetMemberVisitor(It.IsAny<IMemberVisitor>(), It.IsAny<Type>()))
		        .Returns((IMemberVisitor visitor, Type type) => new TargetMemberVisitor(visitor, type));

            var executor = new Executor<TestClass>(fake, new AutoFake.Expression.InvocationExpression(factory.Object, expr), factory.Object);
	        Action act = () => executor.Execute();

	        act.Should().Throw<InvalidCastException>().WithMessage("*Cannot cast \"this\" reference to*");
        }

        [Theory, AutoMoqData]
        internal void Execute_CompatibleRuntimeTypesCast_Throws(Mock<IMemberVisitorFactory> factory)
        {
	        Expression<Func<TestClass, TestClass>> expr = t => t.TestClassMethod();
	        var fake = new Fake<TestClass>();
	        fake.Rewrite(f => f.TestClassMethod());
	        factory.Setup(f => f.GetMemberVisitor<GetTestMethodVisitor>()).Returns(new GetTestMethodVisitor());
	        factory.Setup(f => f.GetValueMemberVisitor(It.IsAny<object>())).Returns((object arg) => new GetValueMemberVisitor(arg));
	        factory.Setup(f => f.GetTargetMemberVisitor(It.IsAny<IMemberVisitor>(), It.IsAny<Type>()))
		        .Returns((IMemberVisitor visitor, Type type) => new TargetMemberVisitor(visitor, type));

            var executor = new Executor<TestClass>(fake, new AutoFake.Expression.InvocationExpression(factory.Object, expr), factory.Object);
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

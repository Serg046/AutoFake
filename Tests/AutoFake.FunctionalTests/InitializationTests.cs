using AutoFake.Abstractions;
using AutoFake.Abstractions.Expression;
using AutoFake.Abstractions.Setup;
using AutoFake.Expression;
using DryIoc;
using FluentAssertions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace AutoFake.FunctionalTests
{
	public class InitializationTests
	{
		[Fact]
		public void When_incorrect_args_Should_fail()
		{
			var fake = new Fake<TestClass>(Arg.IsNull<object>());

			Action act = () => fake.Execute(f => f.GetType());

			act.Should().Throw<MissingMethodException>().WithMessage("Constructor is not found");
		}

		[Fact]
		public void When_ambiguous_null_arg_Should_fail()
		{
			var fake = new Fake<TestClass>(Arg.IsNull<object>(), null);

			Action act = () => fake.Execute(f => f.GetType());

			act.Should().Throw<AmbiguousMatchException>().WithMessage("Ambiguous null-invocation*");
		}

		[Fact]
		public void When_method_call_as_arg_Should_Succeed()
		{
			var fake = new Fake<TestClass>();
			var testClass = new TestClass();

			var sut = fake.Rewrite(f => f.GetNumber("1"));
			sut.Verify(f => f.SomeMethod(GetOne()));

			sut.Execute();
		}

		[Fact]
		public void When_is_arg_with_different_type_Should_Succeed()
		{
			var fake = new Fake<TestClass>();
			var testClass = new TestClass();

			var sut = fake.Rewrite(f => f.GetNumber("1"));
			sut.Verify(f => f.SomeMethod(Arg.Is<string>(x => x == "1")));

			sut.Execute();
		}

		string GetOne() => "1";

		[Fact]
		public void When_no_closure_field_Should_fail()
		{
			var fake = new Fake<TestClass>();
			var type = typeof(TestClass);
			fake.Services.RegisterInstance<IAssemblyLoader>(new FakeAsseblyLoader(type.Assembly, type), ifAlreadyRegistered: IfAlreadyRegistered.Replace);

			var sut = fake.Rewrite(f => f.GetNumber("test"));
			sut.Append(() => { });
			Action act = () => sut.Execute();

			act.Should().Throw<MissingFieldException>();
		}

		[Fact]
		public void When_no_return_field_Should_fail()
		{
			var fake = new Fake<TestClass>();
			var type = typeof(TestClass);
			fake.Services.RegisterInstance<IAssemblyLoader>(new FakeAsseblyLoader(type.Assembly, type), ifAlreadyRegistered: IfAlreadyRegistered.Replace);

			var sut = fake.Rewrite(f => f.GetNumber("test"));
			sut.Replace(t => t.GetNumber(Arg.IsAny<string>())).Return("str");
			Action act = () => sut.Execute();

			act.Should().Throw<MissingFieldException>();
		}

		[Fact]
		public void When_no_setup_body_field_definition_Should_fail()
		{
			var fake = new Fake<InitializationTests>();
			var type = typeof(InitializationTests);
			IPrePostProcessor prePostProc = new FakePrePostProcessor(new()
			{
				[typeof(IInvocationExpression)] = null,
				[typeof(IExecutionContext)] = null
			});
			fake.Services.RegisterInstance(prePostProc, ifAlreadyRegistered: IfAlreadyRegistered.Replace);
			fake.Services.RegisterInstance<IAssemblyLoader>(new FakeAsseblyLoader(type.Assembly, type), ifAlreadyRegistered: IfAlreadyRegistered.Replace);

			var sut = fake.Rewrite(f => f.When_no_setup_body_field_definition_Should_fail());
			sut.Replace(() => DateTime.Now).Return(DateTime.MaxValue);
			Action act = () => sut.Execute();

			act.Should().Throw<InvalidOperationException>();
		}

		[Fact]
		public void When_no_setup_body_field_Should_fail()
		{
			var fake = new Fake<InitializationTests>();
			var type = typeof(InitializationTests);
			fake.Services.RegisterInstance<IAssemblyLoader>(new FakeAsseblyLoader(type.Assembly, type), ifAlreadyRegistered: IfAlreadyRegistered.Replace);

			var sut = fake.Rewrite(f => f.When_no_setup_body_field_Should_fail());
			sut.Replace(() => DateTime.Now).Return(DateTime.MaxValue);
			Action act = () => sut.Execute();

			act.Should().Throw<MissingFieldException>();
		}

		[Fact]
		public void When_no_execution_context_field_definition_Should_fail()
		{
			var fake = new Fake<TestClassWithSetupBodyField>();
			var type = typeof(TestClassWithSetupBodyField);
			IPrePostProcessor prePostProc = new FakePrePostProcessor(new()
			{
				[typeof(IInvocationExpression)] = CreateFieldDefinition(),
				[typeof(IExecutionContext)] = null
			});
			fake.Services.RegisterInstance(prePostProc, ifAlreadyRegistered: IfAlreadyRegistered.Replace);
			fake.Services.RegisterInstance<IAssemblyLoader>(new FakeAsseblyLoader(type.Assembly, type), ifAlreadyRegistered: IfAlreadyRegistered.Replace);

			var sut = fake.Rewrite(f => f.GetHashCode());
			sut.Replace(() => DateTime.Now).Return(DateTime.MaxValue);
			Action act = () => sut.Execute();

			act.Should().Throw<InvalidOperationException>();
		}

		[Fact]
		public void When_no_execution_context_field_Should_fail()
		{
			var fake = new Fake<TestClassWithSetupBodyField>();
			var type = typeof(TestClassWithSetupBodyField);
			fake.Services.RegisterInstance<IAssemblyLoader>(new FakeAsseblyLoader(type.Assembly, type), ifAlreadyRegistered: IfAlreadyRegistered.Replace);

			var sut = fake.Rewrite(f => f.GetHashCode());
			sut.Replace(() => DateTime.Now).Return(DateTime.MaxValue);
			Action act = () => sut.Execute();

			act.Should().Throw<MissingFieldException>();
		}

		[Fact]
		public void When_no_parameter_full_name_Should_not_fail()
		{
			var fake = new Fake<TestClass>();
			fake.Services.RegisterDelegate((Func<IResolverContext, IInvocationExpression.Create>)(ctx =>
				expr =>
				{
					var invExpr = new InvocationExpression(ctx.Resolve<IMemberVisitorFactory>(), expr);
					return new FakeInvocationExpression(invExpr, new FakeSourceMember(invExpr.GetSourceMember()));
				}),
				ifAlreadyRegistered: IfAlreadyRegistered.Replace);
			
			var sut = fake.Rewrite(f => f.GetHashCode());
			sut.Replace(() => DateTime.Now).Return(DateTime.MaxValue);

			sut.Execute();
		}

		private static FieldDefinition CreateFieldDefinition()
			=> new ("test", Mono.Cecil.FieldAttributes.Static, new FunctionPointerType());

		private class TestClassWithSetupBodyField
		{
			public static IInvocationExpression GetHashCodeSystemDateTime_get_Now_SetupBodyField;
		}

		private class TestClass
		{
			public static IInvocationExpression GetNumberSystemString_GetNumber_SystemString_SetupBodyField;
			public static IExecutionContext GetNumberSystemString_GetNumber_SystemString_ExecutionContext;
			public string GetNumber(string arg) => SomeMethod(arg);
			public string SomeMethod(object arg) => (string)arg;
		}

		private class FakeAsseblyLoader : IAssemblyLoader
		{
			private readonly Assembly _assembly;
			private readonly Type _type;

			public FakeAsseblyLoader(Assembly assembly, Type type)
			{
				_assembly = assembly;
				_type = type;
			}

			public Tuple<Assembly, Type> LoadAssemblies(IFakeOptions options, bool loadFieldsAsm) => new(_assembly, _type);
		}

		private class FakePrePostProcessor : IPrePostProcessor
		{
			private readonly Dictionary<Type, FieldDefinition> _fields;
			public FakePrePostProcessor(Dictionary<Type, FieldDefinition> fields) => _fields = fields;
			public FieldDefinition GenerateField(string name, Type returnType)
				=> _fields.TryGetValue(returnType, out var field) ? field : CreateFieldDefinition();

			public void InjectVerification(IEmitter emitter, FieldDefinition setupBody, FieldDefinition executionContext)
			{
			}
		}

		private class FakeInvocationExpression : IInvocationExpression
		{
			private readonly IInvocationExpression _expression;
			private readonly ISourceMember _sourceMember;

			public FakeInvocationExpression(IInvocationExpression expression, ISourceMember sourceMember)
			{
				_expression = expression;
				_sourceMember = sourceMember;
			}

			public bool ThrowWhenArgumentsAreNotMatched
			{
				get => _expression.ThrowWhenArgumentsAreNotMatched;
				set => _expression.ThrowWhenArgumentsAreNotMatched = value;
			}

			public ISourceMember GetSourceMember() => _sourceMember;
			public T AcceptMemberVisitor<T>(IExecutableMemberVisitor<T> visitor) => _expression.AcceptMemberVisitor(visitor);
			public T AcceptMemberVisitor<T>(IMemberVisitor<T> visitor) => _expression.AcceptMemberVisitor(visitor);
		}

		private class FakeSourceMember : ISourceMember
		{
			private readonly ISourceMember _sourceMember;
			public FakeSourceMember(ISourceMember sourceMember) => _sourceMember = sourceMember;
			public string Name => _sourceMember.Name;
			public Type ReturnType => _sourceMember.ReturnType;
			public MemberInfo OriginalMember => _sourceMember.OriginalMember;
			public bool HasStackInstance => _sourceMember.HasStackInstance;
			public IReadOnlyList<IGenericArgument> GetGenericArguments() => _sourceMember.GetGenericArguments();
			public bool IsSourceInstruction(Instruction instruction, IEnumerable<IGenericArgument> genericArguments)
				=> _sourceMember.IsSourceInstruction(instruction, genericArguments);

			public ParameterInfo[] GetParameters()
			{
				return new[] { new FakeParameterInfo() };
			}

			private class FakeParameterInfo : ParameterInfo
			{
				// Type.FullName is null
				public override Type ParameterType => typeof(IEnumerable<>).GetGenericArguments().First();
			}
		}
	}
}

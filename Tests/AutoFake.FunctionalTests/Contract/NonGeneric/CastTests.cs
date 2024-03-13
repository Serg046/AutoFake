using AutoFake.Abstractions;
using DryIoc;
using FluentAssertions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Linq;
using Xunit;

namespace AutoFake.FunctionalTests.Contract.NonGeneric
{
	public class CastTests
	{
		[ExcludedFact]
		public void When_class_cast_Should_succeed()
		{
			var fake = new Fake<TestClass>();
			var helper = new HelperClass();

			var sut = fake.Rewrite(f => f.CastToHelperClass(helper));

			sut.Execute().Should().Be(helper);
		}

		[ExcludedFact]
		public void When_struct_cast_Should_succeed()
		{
			var fake = new Fake<TestClass>();
			var helper = new HelperStruct();

			var sut = fake.Rewrite(f => f.CastToHelperStruct(helper));

			sut.Execute().Should().Be(helper);
		}

		[ExcludedTheory]
		[InlineData(typeof(HelperClass))]
		[InlineData(typeof(HelperStruct))]
		public void When_interface_cast_Should_succeed(Type type)
		{
			var fake = new Fake<TestClass>();
			var helper = Activator.CreateInstance(type) as IHelper;

			var sut = fake.Rewrite(f => f.CastToHelperInterface(helper));

			sut.Execute().Should().Be(helper);
		}

		[ExcludedFact]
		public void When_unbox_instruction_Should_succeed()
		{
			var fake = new Fake<TestClass>();
			var asmReader = fake.Services.Resolve<IAssemblyReader>();
			var method = asmReader.SourceTypeDefinition.Methods.Single(m => m.Name == nameof(TestClass.CreateAndCastToHelperStruct));
			var proc = method.Body.GetILProcessor();
			foreach (var cmd in method.Body.Instructions.Where(i => i.OpCode == OpCodes.Unbox_Any).ToList())
			{
				var operand = (TypeReference)cmd.Operand;
				proc.InsertAfter(cmd, Instruction.Create(OpCodes.Ldobj, operand));
				proc.Replace(cmd, Instruction.Create(OpCodes.Unbox, operand));
			}

			var sut = fake.Rewrite(f => f.CreateAndCastToHelperStruct(5));

			sut.Execute().Should().BeOfType<HelperStruct>().Subject.Prop.Should().Be(5);
		}

		private class TestClass
		{
			public HelperClass CastToHelperClass(object helper)
			{
				return (HelperClass)helper;
			}

			public HelperStruct CreateAndCastToHelperStruct(int prop)
			{
				object helper = new HelperStruct { Prop = prop };
				return (HelperStruct)helper;
			}

			public HelperStruct CastToHelperStruct(object helper)
			{
				return (HelperStruct)helper;
			}

			public IHelper CastToHelperInterface(object helper)
			{
				return (IHelper)helper;
			}
		}

		public interface IHelper
		{
			int GetFive();
		}

		public class HelperClass : IHelper
		{
			public int Prop { get; set; }
			public int GetFive() => 5;
		}

		public struct HelperStruct : IHelper
		{
			public int Prop { get; set; }
			public int GetFive() => 5;
		}

		public interface IHelper<T>
		{
			T GetValue(T value);
		}
	}
}

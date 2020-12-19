using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFixture.Kernel;
using Mono.Cecil.Cil;
using Moq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace AutoFake.UnitTests
{
    public class AutoMoqDataAttribute : AutoDataAttribute
    {
        public AutoMoqDataAttribute() : base(() =>
            new Fixture().Customize(new AutoMoqCustomization())
                         .Customize(new Customization()))
        {
        }

        private class Customization : ICustomization
        {
            public void Customize(IFixture fixture)
            {
                fixture.Behaviors.Add(new OmitOnRecursionBehavior());
                fixture.Register(() => ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll));
                fixture.Register(() => new TypeDefinition("TestNs", "TestType", TypeAttributes.Class));
                fixture.Register(() => new MethodDefinition("Method", MethodAttributes.Public, fixture.Create<TypeDefinition>()));
                fixture.Register(() => Instruction.Create(OpCodes.Nop));
                fixture.Register(() => new Mock<TypeReference>(fixture.Create<string>(), fixture.Create<TypeReference>()));
                fixture.Register<ParameterInfo>(() => new Parameter {PrmType = fixture.Create<Type>()});
            }
        }

        private class Parameter : ParameterInfo
        {
            public Type PrmType { get; set; }
            public override Type ParameterType => PrmType;
        }

        private class AutoMoqCustomization : ICustomization
        {
	        public void Customize(IFixture fixture)
	        {
		        if (fixture == null) throw new ArgumentNullException(nameof(fixture));

		        ISpecimenBuilder mockBuilder = new MockPostprocessor(
			        new MethodInvoker(
				        new MockConstructorQuery()));

		        mockBuilder = new Postprocessor(
			        builder: mockBuilder,
			        command: new CompositeSpecimenCommand(
				        new StubPropertiesCommand(),
				        new MockVirtualMethodsCommand(),
				        new AutoMockPropertiesCommand(),
				        new CustomSpecimenCommand()));

		        fixture.Customizations.Add(mockBuilder);
		        fixture.ResidueCollectors.Add(new MockRelay());
	        }

	        private class CustomSpecimenCommand : ISpecimenCommand
	        {
		        public void Execute(object specimen, ISpecimenContext context)
		        {
			        Handle((dynamic)specimen, context);
		        }

		        private void Handle(object mock, ISpecimenContext context)
		        {
		        }

		        private void Handle(Mock<ITypeInfo> mock, ISpecimenContext context)
		        {
			        var module = context.Create<ModuleDefinition>();
			        mock.Setup(m => m.ImportReference(It.IsAny<Type>()))
				        .Returns<Type>(t => module.ImportReference(t));
			        mock.Setup(m => m.ImportReference(It.IsAny<FieldInfo>()))
				        .Returns<FieldInfo>(f => module.ImportReference(f));
			        mock.Setup(m => m.ImportReference(It.IsAny<MethodBase>()))
				        .Returns<MethodBase>(m => module.ImportReference(m));
                }
	        }
        }
    }

    public class InlineAutoMoqDataAttribute : CompositeDataAttribute
    {
        public InlineAutoMoqDataAttribute(params object[] values) : base(
            new DataAttribute[] { new InlineDataAttribute(values), new AutoMoqDataAttribute() })
        {
        }
    }

    public class MemberAutoDataAttribute : AutoCompositeDataAttribute
    {
        public MemberAutoDataAttribute(string memberName, params object[] parameters)
            : this(new AutoDataAttribute(), memberName, parameters)
        {
        }

        public MemberAutoDataAttribute(AutoDataAttribute autoDataAttribute, string memberName, params object[] parameters)
            : base(new MemberDataAttribute(memberName, parameters), autoDataAttribute)
        {
            AutoDataAttribute = autoDataAttribute;
        }

        public AutoDataAttribute AutoDataAttribute { get; }
    }

    public class MemberAutoMoqDataAttribute : MemberAutoDataAttribute
    {
        public MemberAutoMoqDataAttribute(string memberName, params object[] parameters)
            : base(new AutoMoqDataAttribute(), memberName, parameters)
        {
        }
    }

    public abstract class AutoCompositeDataAttribute : DataAttribute
    {
        private readonly DataAttribute baseAttribute;
        private readonly DataAttribute autoDataAttribute;

        protected AutoCompositeDataAttribute(DataAttribute baseAttribute, DataAttribute autoDataAttribute)
        {
            this.baseAttribute = baseAttribute;
            this.autoDataAttribute = autoDataAttribute;
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            if (testMethod == null)
            {
                throw new ArgumentNullException(nameof(testMethod));
            }

            var data = this.baseAttribute.GetData(testMethod);

            foreach (var datum in data)
            {
                var autoData = this.autoDataAttribute.GetData(testMethod).ToArray()[0];

                for (var i = 0; i < datum.Length; i++)
                {
                    autoData[i] = datum[i];
                }

                yield return autoData;
            }
        }
    }
}

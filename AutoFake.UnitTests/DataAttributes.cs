﻿using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil.Cil;
using Xunit;
using Xunit.Sdk;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace AutoFake.UnitTests
{
    public class AutoMoqDataAttribute : AutoDataAttribute
    {
        public AutoMoqDataAttribute() : base(() =>
            new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true })
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
                fixture.Register<ParameterInfo>(() => new Parameter {PrmType = fixture.Create<Type>()});
            }
        }

        private class Parameter : ParameterInfo
        {
            public Type PrmType { get; set; }
            public override Type ParameterType => PrmType;
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

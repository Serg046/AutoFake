﻿using AutoFake.Expression;
using AutoFixture.Xunit2;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Moq;
using Xunit;

namespace AutoFake.UnitTests
{
    public class ProcessorFactoryTests
    {
        [Theory, AutoMoqData]
        internal void TypeInfo_ValidData_Success(ITypeInfo typeInfo)
        {
            var factory = new ProcessorFactory(typeInfo);

            Assert.Equal(typeInfo, factory.TypeInfo);
        }

        [Theory, AutoMoqData]
        internal void CreatePrePostProcessor_TypeInfo_Injected(
            string name,
            [Frozen]Mock<ITypeInfo> type,
            ProcessorFactory factory)
        {
            var proc = factory.CreatePrePostProcessor();
            proc.GenerateField(name, typeof(InvocationExpression));

            type.Verify(t => t.AddField(It.Is<FieldDefinition>(f => f.Name == name)));
        }

        [Theory, AutoMoqData]
        internal void CreateProcessor_TypeInfo_Injected(
            Mock<IEmitter> emitter, Instruction instruction,
            ProcessorFactory factory)
        {
            var proc = factory.CreateProcessor(emitter.Object, instruction);
            proc.RemoveInstruction(instruction);
            
            emitter.Verify(e => e.Remove(instruction));
        }
    }
}

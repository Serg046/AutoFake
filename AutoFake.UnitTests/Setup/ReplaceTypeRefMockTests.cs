using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFake.Setup.Mocks;
using Mono.Cecil.Cil;
using Moq;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class ReplaceTypeRefMockTests
    {
        [Fact]
        public void IsSourceInstruction_ValidClassInput_True()
        {
            var type = typeof(ReplaceTypeRefMockTests);
            var typeInfo = new TypeInfo(type, new List<FakeDependency>());
            var mock = new ReplaceTypeRefMock(typeInfo, type);
            var ctors = type.GetConstructors().Select(typeInfo.Module.ImportReference);

            Assert.All(ctors, ctor => Assert.True(mock
                .IsSourceInstruction(null, Instruction.Create(OpCodes.Newobj, ctor))));
        }

        [Fact]
        public void IsSourceInstruction_InvalidClassInput_False()
        {
            var type = typeof(ReplaceTypeRefMockTests);
            var typeInfo = new TypeInfo(type, new List<FakeDependency>());
            var mock = new ReplaceTypeRefMock(typeInfo, type);
            var ctor = typeInfo.Module.ImportReference(type.GetConstructors().First());
            var method = typeInfo.Module.ImportReference(type.GetMethods().First());

            Assert.False(mock.IsSourceInstruction(null, Instruction.Create(OpCodes.Call, ctor)));
            Assert.False(mock.IsSourceInstruction(null, Instruction.Create(OpCodes.Newobj, method)));
            Assert.False(mock.IsSourceInstruction(null, null));
        }

        [Fact]
        public void IsSourceInstruction_ValidStructInput_True()
        {
            var type = typeof(DateTime);
            var typeInfo = new TypeInfo(type, new List<FakeDependency>());
            var mock = new ReplaceTypeRefMock(typeInfo, type);
            var typeRef = typeInfo.Module.ImportReference(type);

            Assert.True(mock.IsSourceInstruction(null, Instruction.Create(OpCodes.Initobj, typeRef)));
        }

        [Fact]
        public void IsSourceInstruction_InvalidStructInput_False()
        {
            var type = typeof(ReplaceTypeRefMockTests);
            var typeInfo = new TypeInfo(type, new List<FakeDependency>());
            var mock = new ReplaceTypeRefMock(typeInfo, type);
            var typeRef = typeInfo.Module.ImportReference(typeof(ValueTask));
            var invalidTypeRef = typeInfo.Module.ImportReference(typeof(ValueTask));

            Assert.False(mock.IsSourceInstruction(null, Instruction.Create(OpCodes.Box, typeRef)));
            Assert.False(mock.IsSourceInstruction(null, Instruction.Create(OpCodes.Initobj, invalidTypeRef)));
            Assert.False(mock.IsSourceInstruction(null, null));
        }

        [Theory, AutoMoqData]
        internal void Inject_Class_Injected(Mock<IEmitter> emitter)
        {
            var type = typeof(ReplaceTypeRefMockTests);
            var typeInfo = new TypeInfo(type, new List<FakeDependency>());
            var mock = new ReplaceTypeRefMock(typeInfo, type);
            var ctor = typeInfo.Module.ImportReference(type.GetConstructors().First());
            var instruction = Instruction.Create(OpCodes.Newobj, ctor);

            mock.Inject(emitter.Object, instruction);

            emitter.Verify(e => e.Replace(instruction,
                It.Is<Instruction>(i => i.Operand.ToString() == instruction.Operand.ToString())));
        }

        [Theory, AutoMoqData]
        internal void Inject_Struct_Injected(Mock<IEmitter> emitter)
        {
            var type = typeof(DateTime);
            var typeInfo = new TypeInfo(type, new List<FakeDependency>());
            var mock = new ReplaceTypeRefMock(typeInfo, type);
            var typeRef = typeInfo.Module.ImportReference(type);
            var instruction = Instruction.Create(OpCodes.Initobj, typeRef);

            mock.Inject(emitter.Object, instruction);

            emitter.Verify(e => e.Replace(instruction,
                It.Is<Instruction>(i => i.Operand.ToString() == instruction.Operand.ToString())));
        }

        [Theory, AutoMoqData]
        internal void Initialize_AnyType_Empty(ReplaceTypeRefMock sut)
        {
            Assert.Empty(sut.Initialize(GetType()));
        }

        [Fact]
        public void GetHashCode_Type_TheSameCache()
        {
            var type = typeof(ReplaceTypeRefMockTests);
            var typeInfo = new TypeInfo(type, new List<FakeDependency>());
            var mock = new ReplaceTypeRefMock(typeInfo, type);

            Assert.Equal(type.GetHashCode(), mock.GetHashCode());
        }

        [Fact]
        public void GetHashCode_Equals_Success()
        {
            var type = typeof(ReplaceTypeRefMockTests);
            var typeInfo = new TypeInfo(type, new List<FakeDependency>());
            var mock1 = new ReplaceTypeRefMock(typeInfo, type);
            var mock2 = new ReplaceTypeRefMock(typeInfo, type);
            var mock3 = new ReplaceTypeRefMock(typeInfo, typeof(ReplaceTypeRefMock));

            Assert.NotSame(mock1, mock2);
            Assert.True(mock1.Equals(mock2));
            Assert.False(mock1.Equals(mock3));
            Assert.False(mock1.Equals(null));
        }
    }
}

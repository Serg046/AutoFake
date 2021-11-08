using AutoFake.Expression;
using AutoFixture.Xunit2;
using Mono.Cecil;
using Moq;
using Xunit;

namespace AutoFake.UnitTests
{
    public class ProcessorFactoryTests
    {
        [Theory, AutoMoqData]
        internal void TypeInfo_ValidData_Success(ITypeInfo typeInfo, IAssemblyWriter writer)
        {
            var factory = new ProcessorFactory(typeInfo, writer);

            Assert.Equal(typeInfo, factory.TypeInfo);
        }

        [Theory, AutoMoqData]
        internal void CreatePrePostProcessor_TypeInfo_Injected(
            string name,
            [Frozen]Mock<IAssemblyWriter> writer,
            ProcessorFactory factory)
        {
            var proc = factory.CreatePrePostProcessor();
            proc.GenerateField(name, typeof(InvocationExpression));

            writer.Verify(t => t.AddField(It.Is<FieldDefinition>(f => f.Name == name)));
        }
    }
}

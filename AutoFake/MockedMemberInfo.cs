using System.Linq;
using System.Text;
using AutoFake.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal class MockedMemberInfo
    {
        private readonly byte _order;

        public MockedMemberInfo(IMock mock, string testMethodName, byte order = 0)
        {
            _order = order;
            Mock = mock;
            TestMethodName = testMethodName;
        }

        public IMock Mock { get; }
        public string TestMethodName { get; }
        public FieldDefinition SetupBodyField { get; internal set; }
        public FieldDefinition RetValueField { get; internal set; }
        public VariableDefinition ActualCallsAccumulator { get; internal set; }
        public FieldDefinition ExpectedCallsFuncField { get; internal set; }
        public FieldDefinition CallbackField { get; internal set; }

        public string GenerateFieldName()
        {
            var fieldName = new StringBuilder(Mock.SourceMember.ReturnType.ToString().Replace(".", ""))
                .Append("_").Append(Mock.SourceMember.Name).Append("_")
                .Append(ArgumentsToString(Mock.SourceMember))
                .Append(TestMethodName);
            if (_order > 0) fieldName.Append(_order);
            return fieldName.ToString();
        }

        private string ArgumentsToString(ISourceMember member)
        {
            var parameters = member.GetParameters();
            return parameters.Any()
                ? string.Join(string.Empty, parameters.Select(p => p.ParameterType.ToString().Replace(".", ""))) + "_"
                : string.Empty;
        }
    }
}

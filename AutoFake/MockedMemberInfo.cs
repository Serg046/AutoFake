using System;
using System.Text;
using AutoFake.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal class MockedMemberInfo
    {
        private readonly byte _order;
        private readonly Lazy<string> _uniqueName;

        public MockedMemberInfo(IMock mock, string testMethodName, byte order = 0)
        {
            _order = order;
            Mock = mock;
            TestMethodName = testMethodName;

            _uniqueName = new Lazy<string>(() =>
            {
                var fieldName = new StringBuilder(Mock.UniqueName).Append("_").Append(TestMethodName);
                if (_order > 0) fieldName.Append(_order);
                return fieldName.ToString();
            });
        }

        public IMock Mock { get; }
        public string TestMethodName { get; }
        public string UniqueName => _uniqueName.Value;
        public FieldDefinition SetupBodyField { get; internal set; }
        public FieldDefinition RetValueField { get; internal set; }
        public VariableDefinition ActualCallsAccumulator { get; internal set; }
        public FieldDefinition ExpectedCallsFuncField { get; internal set; }
        public FieldDefinition CallbackField { get; internal set; }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Setup;

namespace AutoFake
{
    internal class FakeGenerator
    {
        private readonly ITypeInfo _typeInfo;
        private readonly MockerFactory _mockerFactory;

        public FakeGenerator(ITypeInfo typeInfo, MockerFactory mockerFactory)
        {
            _typeInfo = typeInfo;
            _mockerFactory = mockerFactory;
        }

        public void Generate(ICollection<IMock> mocks, ICollection<MockedMemberInfo> mockedMembers, MethodBase executeFunc)
        {
            foreach (var mock in mocks)
            {
                var mocker = _mockerFactory.CreateMocker(_typeInfo, new MockedMemberInfo(mock, executeFunc.Name,
                    (byte)mockedMembers.Count(m => m.TestMethodName == executeFunc.Name)));
                mock.PrepareForInjecting(mocker);
                var method = _typeInfo.Methods.Single(m => m.EquivalentTo(executeFunc));
                new FakeMethod(method, mocker).ApplyMock(mock);
                mockedMembers.Add(mocker.MemberInfo);
            }
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Setup;

namespace AutoFake
{
    internal class FakeGenerator
    {
        private readonly ITypeInfo _typeInfo;
        private readonly MockerFactory _mockerFactory;
        private readonly GeneratedObject _generatedObject;

        public FakeGenerator(ITypeInfo typeInfo, MockerFactory mockerFactory, GeneratedObject generatedObject)
        {
            _typeInfo = typeInfo;
            _mockerFactory = mockerFactory;
            _generatedObject = generatedObject;
        }

        public void Save(string fileName)
        {
            using (var fileStream = File.Create(fileName))
            {
                _typeInfo.WriteAssembly(fileStream);
            }
        }

        public void Generate(ICollection<Mock> mocks, MethodBase executeFunc)
        {
            if (_generatedObject.IsBuilt)
                throw new FakeGeneretingException("Fake is already built. Please use another instance.");

            MockSetups(mocks, executeFunc);
        }


        private void MockSetups(ICollection<Mock> mocks, MethodBase executeFunc)
        {
            foreach (var mock in mocks)
            {
                var mocker = _mockerFactory.CreateMocker(_typeInfo,
                    new MockedMemberInfo(mock, executeFunc.Name, GetExecuteFuncSuffixName(executeFunc)));
                mock.PrepareForInjecting(mocker);

                var method = _typeInfo.Methods.Single(m => m.EquivalentTo(executeFunc));
                new FakeMethod(method, mocker).ApplyMock(mock);

                _generatedObject.MockedMembers.Add(mocker.MemberInfo);
            }
        }

        private string GetExecuteFuncSuffixName(MethodBase executeFunc)
        {
            var suffixName = executeFunc.Name;
            var installedCount = _generatedObject.MockedMembers.Count(g => g.TestMethodName == executeFunc.Name);
            if (installedCount > 0)
                suffixName += installedCount;
            return suffixName;
        }
    }
}

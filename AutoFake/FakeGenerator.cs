using System.Collections.Generic;
using Mono.Cecil;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Setup;

namespace AutoFake
{
    internal class FakeGenerator
    {
        private readonly TypeInfo _typeInfo;
        private readonly MockerFactory _mockerFactory;
        private readonly GeneratedObject _generatedObject;

        public FakeGenerator(TypeInfo typeInfo, MockerFactory mockerFactory, GeneratedObject generatedObject)
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
                ReplaceInstructions(method, mock, mocker);

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

        private void ReplaceInstructions(MethodDefinition currentMethod, Mock mock, IMocker mocker)
        {
            MethodDefinition asyncMethod;
            if (mock.IsAsyncMethod(currentMethod, out asyncMethod))
                ReplaceInstructions(asyncMethod, mock, mocker);
            
            foreach (var instruction in currentMethod.Body.Instructions.ToList())
            {
                if (mock.IsInstalledInstruction(_typeInfo, instruction))
                {
                    var proc = currentMethod.Body.GetILProcessor();
                    mock.Inject(mocker, proc, instruction);
                }
                else if (mock.IsMethodInstruction(instruction))
                {
                    var method = (MethodReference)instruction.Operand;
                    
                    if (IsFakeAssemblyMethod(method))
                    {
                        ReplaceInstructions(method.Resolve(), mock, mocker);
                    }
                }
            }
        }

        private bool IsFakeAssemblyMethod(MethodReference methodReference) 
            => methodReference.DeclaringType.Scope is ModuleDefinition module && module == _typeInfo.Module;
    }
}

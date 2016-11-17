using Mono.Cecil;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Setup;
using GuardExtensions;

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

        public GeneratedObject Generate(SetupCollection setups, MethodInfo executeFunc)
        {
            if (setups.Any(s => !s.IsVerification && !s.IsVoid && !s.IsReturnObjectSet))
                throw new SetupException("At least one non-void installed member does not have a return value.");

            MockSetups(setups, executeFunc);

            using (var memoryStream = new MemoryStream())
            {
                _typeInfo.WriteAssembly(memoryStream);
                var assembly = Assembly.Load(memoryStream.ToArray());
                _generatedObject.Type = assembly.GetType(_typeInfo.FullTypeName);
                _generatedObject.Instance = IsStatic(_typeInfo.SourceType)
                    ? null
                    : _typeInfo.CreateInstance(_generatedObject.Type);
                return _generatedObject;
            }
        }

        private bool IsStatic(Type type) => type.IsAbstract && type.IsSealed;

        private void MockSetups(SetupCollection setups, MethodInfo executeFunc)
        {
            foreach (var setup in setups)
            {
                var mocker = _mockerFactory.CreateMocker(_typeInfo,
                    new MockedMemberInfo(setup, executeFunc, GetExecuteFuncSuffixName(executeFunc)));
                mocker.GenerateCallsCounter();

                if (!setup.IsVoid)
                    mocker.GenerateRetValueField();
                if (setup.Callback != null)
                    mocker.GenerateCallbackField();

                var methodInjector = _mockerFactory.CreateMethodInjector(mocker);
                var method = _typeInfo.Methods.Single(m => m.EquivalentTo(executeFunc));
                ReplaceInstructions(method, methodInjector);

                _generatedObject.MockedMembers.Add(mocker.MemberInfo);
            }
        }

        private string GetExecuteFuncSuffixName(MethodInfo executeFunc)
        {
            var suffixName = executeFunc.Name;
            var installedCount = _generatedObject.MockedMembers.Count(g => g.TestMethodInfo.Name == executeFunc.Name);
            if (installedCount > 0)
                suffixName += installedCount;
            return suffixName;
        }

        private void ReplaceInstructions(MethodDefinition currentMethod, IMethodInjector methodInjector)
        {
            MethodDefinition asyncMethod;
            if (methodInjector.IsAsyncMethod(currentMethod, out asyncMethod))
                ReplaceInstructions(asyncMethod, methodInjector);
            
            foreach (var instruction in currentMethod.Body.Instructions.ToList())
            {
                if (methodInjector.IsMethodInstruction(instruction))
                {
                    var method = (MethodReference)instruction.Operand;
                    
                    if (methodInjector.IsInstalledMethod(method))
                    {
                        var proc = currentMethod.Body.GetILProcessor();
                        methodInjector.Process(proc, instruction);
                    }
                    else if (IsClientSourceCode(method))
                    {
                        ReplaceInstructions(method.Resolve(), methodInjector);
                    }
                }
            }
        }

        private bool IsClientSourceCode(MethodReference methodReference) => methodReference.IsDefinition;
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Setup.Mocks;
using Mono.Cecil;

namespace AutoFake
{
    internal class FakeGenerator
    {
        private readonly ITypeInfo _typeInfo;

        public FakeGenerator(ITypeInfo typeInfo)
        {
            _typeInfo = typeInfo;
        }

        public void Generate(IEnumerable<IMock> mocks, MethodBase executeFunc)
        {
            var executeFuncRef = _typeInfo.Module.ImportReference(executeFunc);
            var executeFuncDef = _typeInfo.Methods.Single(m => m.EquivalentTo(executeFuncRef));

            ReplaceTypeRefMock[] replaceTypeRefMocks = null;
            if (executeFunc is MethodInfo methodInfo)
            {
                if (methodInfo.ReturnType.Module == methodInfo.Module)
                {
                    var returnTypeRef = _typeInfo.Module.ImportReference(methodInfo.ReturnType);
                    executeFuncDef.ReturnType = returnTypeRef;
                    replaceTypeRefMocks = new[] { new ReplaceTypeRefMock(_typeInfo, methodInfo.ReturnType) };
                    mocks = mocks.Concat(replaceTypeRefMocks);
                }
            }

            foreach (var mock in mocks) mock.BeforeInjection(executeFuncDef);
            var testMethod = new TestMethod(executeFuncDef, mocks);
            testMethod.Rewrite();
            foreach (var mock in mocks) mock.AfterInjection(executeFuncDef.Body.GetEmitter());

            if (replaceTypeRefMocks != null)
            {
                foreach (var ctor in _typeInfo.Methods.Where(m => m.Name == ".ctor" || m.Name == ".cctor"))
                {
                    new TestMethod(ctor, replaceTypeRefMocks).Rewrite();
                }
            }
        }

        private class TestMethod
        {
            private readonly MethodDefinition _originalMethod;
            private readonly IEnumerable<IMock> _mocks;

            public TestMethod(MethodDefinition originalMethod, IEnumerable<IMock> mocks)
            {
                _originalMethod = originalMethod;
                _mocks = mocks;
            }

            public void Rewrite()
            {
                Rewrite(_originalMethod);
            }

            private void Rewrite(MethodDefinition currentMethod)
            {
                if (currentMethod.IsAsync(out var asyncMethod))
                {
                    Rewrite(asyncMethod);
                }

                foreach (var instruction in currentMethod.Body.Instructions.ToList())
                foreach (var mock in _mocks)
                {
                    if (mock.IsSourceInstruction(_originalMethod, instruction))
                    {
                        mock.Inject(currentMethod.Body.GetEmitter(), instruction);
                    }
                    else if (instruction.Operand is MethodReference method && IsFakeAssemblyMethod(method))
                    {
                        var methodDefinition = method.Resolve();
                        Rewrite(methodDefinition);
                    }
                }
            }

            private bool IsFakeAssemblyMethod(MethodReference methodReference)
                => methodReference.DeclaringType.Scope is ModuleDefinition module && module == _originalMethod.Module;
        }
    }
}

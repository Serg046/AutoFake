using System;
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
        private readonly FakeOptions _options;

        public FakeGenerator(ITypeInfo typeInfo, FakeOptions fakeOptions)
        {
            _typeInfo = typeInfo;
            _options = fakeOptions;
        }

        public void Generate(IEnumerable<IMock> mocks, MethodBase executeFunc)
        {
            var executeFuncRef = _typeInfo.Module.ImportReference(executeFunc);
            var executeFuncDef = _typeInfo.GetMethod(executeFuncRef);
            if (executeFuncDef.Body == null) throw new InvalidOperationException("Methods without body are not supported");

            var replaceTypeRefMocks = ProcessOriginalMethodContract(executeFunc, executeFuncDef);
            mocks = mocks.Concat(replaceTypeRefMocks);
            foreach (var mock in mocks) mock.BeforeInjection(executeFuncDef);
            var testMethod = new TestMethod(this, executeFuncDef, mocks);
            testMethod.Rewrite();
            foreach (var mock in mocks) mock.AfterInjection(executeFuncDef.Body.GetEmitter());

            if (replaceTypeRefMocks.Any())
            {
                foreach (var ctor in _typeInfo.Methods.Where(m => m.Name == ".ctor" || m.Name == ".cctor"))
                {
                    new TestMethod(this, ctor, replaceTypeRefMocks).Rewrite();
                }
            }
        }

        private ICollection<ReplaceTypeRefMock> ProcessOriginalMethodContract(MethodBase executeFunc, MethodDefinition executeFuncDef)
        {
            var replaceTypeRefMocks = new HashSet<ReplaceTypeRefMock>();
            if (executeFunc is MethodInfo methodInfo)
            {
                if (methodInfo.ReturnType.Module == methodInfo.Module)
                {
                    var typeRef = _typeInfo.Module.ImportReference(methodInfo.ReturnType);
                    executeFuncDef.ReturnType = typeRef;
                    replaceTypeRefMocks.Add(new ReplaceTypeRefMock(_typeInfo, methodInfo.ReturnType));
                }

                var parameters = methodInfo.GetParameters();
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType.Module == methodInfo.Module)
                    {
                        var typeRef = _typeInfo.Module.ImportReference(parameters[i].ParameterType);
                        executeFuncDef.Parameters[i].ParameterType = typeRef;
                        replaceTypeRefMocks.Add(new ReplaceTypeRefMock(_typeInfo, parameters[i].ParameterType));
                    }
                }
            }
            return replaceTypeRefMocks;
        }

        private class TestMethod
        {
            private readonly FakeGenerator _gen;
            private readonly MethodDefinition _originalMethod;
            private readonly IEnumerable<IMock> _mocks;
            private readonly HashSet<MethodDefinition> _methods;

            public TestMethod(FakeGenerator gen, MethodDefinition originalMethod, IEnumerable<IMock> mocks)
            {
                _gen = gen;
                _originalMethod = originalMethod;
                _mocks = mocks;
                _methods = new HashSet<MethodDefinition>();
            }

            public void Rewrite()
            {
                Rewrite(_originalMethod);
            }

            private void Rewrite(MethodDefinition currentMethod)
            {
                if (currentMethod?.Body == null || !_methods.Add(currentMethod)) return;

                if (currentMethod.IsVirtual && (_gen._options.IncludeAllVirtualMembers ||
                    _gen._options.VirtualMembers.Contains(currentMethod.Name)))
                {
                    foreach (var virtualMethod in _gen._typeInfo.GetDerivedVirtualMethods(currentMethod))
                    {
                        Rewrite(virtualMethod);
                    }
                }

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
                        Rewrite(method.ToMethodDefinition());
                    }
                }
            }

            private bool IsFakeAssemblyMethod(MethodReference methodReference)
                => methodReference.Module == _originalMethod.Module;
        }
    }
}

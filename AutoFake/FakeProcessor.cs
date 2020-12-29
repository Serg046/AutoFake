﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Setup.Mocks;
using Mono.Cecil;

namespace AutoFake
{
    internal class FakeProcessor
    {
        private readonly ITypeInfo _typeInfo;
        private readonly FakeOptions _options;

        public FakeProcessor(ITypeInfo typeInfo, FakeOptions fakeOptions)
        {
            _typeInfo = typeInfo;
            _options = fakeOptions;
        }

        public void Generate(IEnumerable<IMock> mocks, MethodBase executeFunc)
        {
	        using var emitterPool = new EmitterPool();
            var executeFuncRef = _typeInfo.ImportReference(executeFunc);
            var executeFuncDef = _typeInfo.GetMethod(executeFuncRef);
            if (executeFuncDef?.Body == null) throw new InvalidOperationException("Methods without body are not supported");

            var replaceTypeRefMocks = ProcessOriginalMethodContract(executeFunc, executeFuncDef);
            mocks = mocks.Concat(replaceTypeRefMocks);
            foreach (var mock in mocks) mock.BeforeInjection(executeFuncDef);
            var testMethod = new TestMethod(this, executeFuncDef, mocks, emitterPool);
            testMethod.Rewrite();
            foreach (var mock in mocks) mock.AfterInjection(emitterPool.GetEmitter(executeFuncDef.Body));

            if (replaceTypeRefMocks.Any())
            {
                foreach (var ctor in _typeInfo.GetMethods(m => m.Name == ".ctor" || m.Name == ".cctor"))
                {
                    new TestMethod(this, ctor, replaceTypeRefMocks, emitterPool).Rewrite();
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
                    var typeRef = _typeInfo.ImportReference(methodInfo.ReturnType);
                    executeFuncDef.ReturnType = typeRef;
                    replaceTypeRefMocks.Add(new ReplaceTypeRefMock(_typeInfo, methodInfo.ReturnType));
                }

                var parameters = methodInfo.GetParameters();
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType.Module == methodInfo.Module)
                    {
                        var typeRef = _typeInfo.ImportReference(parameters[i].ParameterType);
                        executeFuncDef.Parameters[i].ParameterType = typeRef;
                        replaceTypeRefMocks.Add(new ReplaceTypeRefMock(_typeInfo, parameters[i].ParameterType));
                    }
                }
            }
            return replaceTypeRefMocks;
        }

        private class TestMethod
        {
            private readonly FakeProcessor _gen;
            private readonly MethodDefinition _originalMethod;
            private readonly IEnumerable<IMock> _mocks;
            private readonly IEmitterPool _emitterPool;
            private readonly HashSet<string> _methods;

            public TestMethod(FakeProcessor gen, MethodDefinition originalMethod, IEnumerable<IMock> mocks, IEmitterPool emitterPool)
            {
                _gen = gen;
                _originalMethod = originalMethod;
                _mocks = mocks;
                _emitterPool = emitterPool;
                _methods = new HashSet<string>();
            }

            public void Rewrite()
            {
                Rewrite(_originalMethod);
            }

            private void Rewrite(MethodDefinition currentMethod)
            {
                if (currentMethod?.Body == null || !_methods.Add(currentMethod.ToString())) return;

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
                        mock.Inject(_emitterPool.GetEmitter(currentMethod.Body), instruction);
                    }
                    else if (instruction.Operand is MethodReference method && ShouldBeAnalyzed(method))
                    {
                        Rewrite(method.ToMethodDefinition());
                    }
                }
            }

            private bool ShouldBeAnalyzed(MethodReference methodReference)
            {
	            switch (_gen._options.AnalysisLevel)
	            {
		            case AnalysisLevels.Type:
		            {
			            if (methodReference.DeclaringType.FullName == _originalMethod.DeclaringType.FullName &&
			                methodReference.Module.Assembly == _originalMethod.Module.Assembly) return true;
			            break;
		            }
		            case AnalysisLevels.Assembly:
		            {
			            if (methodReference.Module.Assembly == _originalMethod.Module.Assembly) return true;
			            break;
		            }
		            case AnalysisLevels.AllAssemblies: return true;
                    default: throw new NotSupportedException($"{_gen._options.AnalysisLevel.ToString()} is not supported");
	            }

	            foreach (var assembly in _gen._options.Assemblies)
	            {
		            if (methodReference.DeclaringType.Module.Assembly.FullName == assembly.FullName)
		            {
			            return true;
		            }
	            }

	            return false;
            }
        }
    }
}

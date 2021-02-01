using System;
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

            var replaceTypeMocks = ProcessOriginalContracts(mocks, executeFunc, executeFuncDef);
            mocks = mocks.Concat(replaceTypeMocks);

            foreach (var mock in mocks) mock.BeforeInjection(executeFuncDef);
            var testMethod = new TestMethod(this, executeFuncDef, mocks, emitterPool);
            testMethod.Rewrite();
            foreach (var mock in mocks) mock.AfterInjection(emitterPool.GetEmitter(executeFuncDef.Body));

            if (replaceTypeMocks.Any())
            {
                foreach (var ctor in _typeInfo.GetMethods(m => m.Name == ".ctor" || m.Name == ".cctor"))
                {
                    new TestMethod(this, ctor, replaceTypeMocks, emitterPool).Rewrite();
                }
            }
        }

        private HashSet<IMock> ProcessOriginalContracts(IEnumerable<IMock> mocks, MethodBase executeFunc, MethodDefinition executeFuncDef)
        {
	        var replaceTypeMocks = new HashSet<IMock>();
	        ProcessOriginalMethodContract(replaceTypeMocks, executeFunc, executeFuncDef);
	        if (executeFunc.ReflectedType != null)
	        {
		        foreach (var ctor in executeFunc.ReflectedType.GetConstructors())
		        {
			        var ctorRef = _typeInfo.ImportReference(ctor);
			        var ctorDef = _typeInfo.GetMethod(ctorRef);
			        ProcessOriginalMethodContract(replaceTypeMocks, ctor, ctorDef);
		        }
	        }

	        foreach (var replaceMock in mocks.OfType<ReplaceMock>().Where(m => m.ReturnType != null))
	        {
		        AddReplaceTypeMocks(replaceTypeMocks, replaceMock.ReturnType);
	        }

	        return replaceTypeMocks;
        }

        private void ProcessOriginalMethodContract(HashSet<IMock> mocks, MethodBase executeFunc, MethodDefinition executeFuncDef)
        {
            if (executeFunc is MethodInfo methodInfo)
            {
                if (methodInfo.ReturnType.Module == methodInfo.Module)
                {
                    var typeRef = _typeInfo.ImportReference(methodInfo.ReturnType);
                    executeFuncDef.ReturnType = typeRef;
                    AddReplaceTypeMocks(mocks, methodInfo.ReturnType);
                }
            }

            var parameters = executeFunc.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType.Module == executeFunc.Module)
                {
                    var typeRef = _typeInfo.ImportReference(parameters[i].ParameterType);
                    executeFuncDef.Parameters[i].ParameterType = typeRef;
                    AddReplaceTypeMocks(mocks, parameters[i].ParameterType);
                }
            }
        }

        private void AddReplaceTypeMocks(HashSet<IMock> mocks, Type type)
        {
	        mocks.Add(new ReplaceTypeCtorMock(_typeInfo, type));
	        foreach (var mock in ReplaceInterfaceCallMock.Create(_typeInfo, type))
	        {
		        mocks.Add(mock);
	        }
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

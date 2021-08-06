using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Setup.Mocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

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

        public void ProcessMethod(MethodBase method)
		{
			var typeDef = _typeInfo.GetTypeDefinition(method.DeclaringType);
			var methodRef = _typeInfo.ImportReference(method);
			var methodDef = _typeInfo.GetMethod(typeDef, methodRef);
			ProcessSourceMethod(new IMock[0], method, methodDef);
        }

        public void ProcessSourceMethod(IEnumerable<IMock> mocks, MethodBase executeFunc)
        {
	        var executeFuncRef = _typeInfo.ImportReference(executeFunc);
	        ProcessSourceMethod(mocks, executeFunc, _typeInfo.GetMethod(executeFuncRef));
        }

        private void ProcessSourceMethod(IEnumerable<IMock> mocks, MethodBase executeFunc, MethodDefinition executeFuncDef)
        {
	        if (executeFuncDef?.Body == null) throw new InvalidOperationException("Methods without body are not supported");

	        var replaceTypeMocks = ProcessOriginalContracts(mocks, executeFunc, executeFuncDef);
	        mocks = mocks.Concat(replaceTypeMocks);

	        using var emitterPool = new EmitterPool();
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
	        ProcessAllOriginalMethodContracts(replaceTypeMocks, executeFuncDef, false);
	        foreach (var mock in mocks.OfType<SourceMemberMock>())
	        {
		        if (mock is ReplaceMock replaceMock && replaceMock.ReturnType?.Module == executeFunc.Module)
		        {
			        AddReplaceInterfaceCallMocks(replaceTypeMocks, _typeInfo.GetTypeDefinition(replaceMock.ReturnType));
		        }

		        if (mock.SourceMember.OriginalMember is MethodBase method && method.Module == executeFunc.Module && method.DeclaringType != null)
		        {
			        var typeDef = _typeInfo.GetTypeDefinition(method.DeclaringType);
					var methodRef = _typeInfo.ImportReference(method);
			        var methodDef = _typeInfo.GetMethod(typeDef, methodRef);
					ProcessAllOriginalMethodContracts(replaceTypeMocks, methodDef, true);
		        }
			}

			foreach (var ctorDef in _typeInfo.GetMethods(m => m.Name == ".ctor"))
			{
				ProcessOriginalMethodContract(replaceTypeMocks, ctorDef, false);
			}

			return replaceTypeMocks;
        }

        private void ProcessAllOriginalMethodContracts(HashSet<IMock> mocks, MethodDefinition methodDef, bool replaceCtors)
        {
	        foreach (var typeDef in _typeInfo.TypeMap.GetAllParentsAndDescendants(methodDef.DeclaringType))
	        {
				var method = _typeInfo.GetMethod(typeDef, methodDef);
				if (method != null) ProcessOriginalMethodContract(mocks, method, replaceCtors);
			}
        }
		
        private void ProcessOriginalMethodContract(HashSet<IMock> mocks, MethodDefinition methodDef, bool replaceCtors)
        {
	        if (methodDef.ReturnType != null && methodDef.ReturnType.FullName != "System.Void" && _typeInfo.IsInFakeModule(methodDef.ReturnType))
	        {
		        AddReplaceTypeMocks(mocks, methodDef.ReturnType.ToTypeDefinition());
		        methodDef.ReturnType = _typeInfo.CreateImportedTypeReference(methodDef.ReturnType);
	        }

	        foreach (var parameterDef in methodDef.Parameters.Where(parameterDef => _typeInfo.IsInFakeModule(parameterDef.ParameterType)))
	        {
		        var typeDefinition = parameterDef.ParameterType.ToTypeDefinition();
				AddReplaceInterfaceCallMocks(mocks, typeDefinition);
		        if (replaceCtors) AddReplaceTypeMocks(mocks, typeDefinition);
				parameterDef.ParameterType = _typeInfo.CreateImportedTypeReference(parameterDef.ParameterType);
	        }
        }

		private void AddReplaceTypeMocks(HashSet<IMock> mocks, TypeDefinition typeDef)
        {
	        foreach (var mockTypeDef in _typeInfo.TypeMap.GetAllParentsAndDescendants(typeDef))
	        {
				var importedTypeRef = _typeInfo.CreateImportedTypeReference(mockTypeDef);
		        if (!mockTypeDef.IsInterface)
		        {
			        mocks.Add(mockTypeDef.IsValueType ? new ReplaceValueTypeCtorMock(importedTypeRef) : new ReplaceReferenceTypeCtorMock(importedTypeRef));
		        }
		        mocks.Add(mockTypeDef.IsValueType ? new ReplaceValueTypeCastMock(importedTypeRef) : new ReplaceReferenceTypeCastMock(importedTypeRef));
			}
		}

		private void AddReplaceInterfaceCallMocks(HashSet<IMock> mocks, TypeDefinition typeDef)
		{
			foreach (var mockTypeDef in _typeInfo.TypeMap.GetAllParentsAndDescendants(typeDef))
			{
				var importedTypeRef = _typeInfo.CreateImportedTypeReference(mockTypeDef);
				if (mockTypeDef.IsInterface) mocks.Add(new ReplaceInterfaceCallMock(importedTypeRef));
				mocks.Add(mockTypeDef.IsValueType ? new ReplaceValueTypeCastMock(importedTypeRef) : new ReplaceReferenceTypeCastMock(importedTypeRef));
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
                Rewrite(_originalMethod, Enumerable.Empty<MethodDefinition>());
            }

            private void Rewrite(MethodDefinition currentMethod, IEnumerable<MethodDefinition> parents)
            {
                if (currentMethod?.Body == null || !_methods.Add(currentMethod.ToString())) return;

                if (currentMethod.IsVirtual && (_gen._options.IncludeAllVirtualMembers ||
                    _gen._options.VirtualMembers.Contains(currentMethod.Name)))
                {
                    foreach (var virtualMethod in _gen._typeInfo.GetDerivedVirtualMethods(currentMethod))
                    {
                        Rewrite(virtualMethod, GetParents());
                    }
                }

                if (currentMethod.IsAsync(out var asyncMethod))
                {
                    Rewrite(asyncMethod, GetParents());
                }

                foreach (var instruction in currentMethod.Body.Instructions.ToList())
                {
	                var originalInstruction = true;
	                var instructionRef = instruction;
					foreach (var mock in _mocks)
	                {
		                if (mock.IsSourceInstruction(_originalMethod, instructionRef))
		                {
			                var emitter = _emitterPool.GetEmitter(currentMethod.Body);
			                if (originalInstruction)
			                {
				                instructionRef = emitter.ShiftDown(instructionRef);
								originalInstruction = false;
			                }

			                mock.Inject(emitter, instructionRef);
			                TryAddAffectedAssembly(currentMethod);
			                foreach (var parent in parents)
			                {
				                TryAddAffectedAssembly(parent);
			                }
		                }
		                else if (instructionRef.Operand is MethodReference method && ShouldBeAnalyzed(method))
		                {
			                Rewrite(method.ToMethodDefinition(), GetParents());
		                }
	                }
                }

                IEnumerable<MethodDefinition> GetParents() => parents.Concat(new[] { currentMethod });
            }

            private void TryAddAffectedAssembly(MethodDefinition currentMethod)
            {
	            if (currentMethod.Module.Assembly != _originalMethod.Module.Assembly)
	            {
		            _gen._typeInfo.TryAddAffectedAssembly(currentMethod.Module.Assembly);
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

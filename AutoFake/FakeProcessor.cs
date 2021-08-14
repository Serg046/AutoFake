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
        private readonly HashSet<IMock> _replaceContractMocks;
        private readonly Dictionary<string, TypeReference> _importedTypes;

        public FakeProcessor(ITypeInfo typeInfo, FakeOptions fakeOptions)
        {
            _typeInfo = typeInfo;
            _options = fakeOptions;
            _replaceContractMocks = new HashSet<IMock>();
            _importedTypes = new Dictionary<string, TypeReference>();
        }

        public void ProcessMethod(IEnumerable<IMock> mocks, MethodBase executeFunc)
        {
	        var executeFuncRef = _typeInfo.ImportReference(executeFunc);
	        var executeFuncDef = _typeInfo.GetMethod(executeFuncRef);
			if (executeFuncDef?.Body == null) throw new InvalidOperationException("Methods without body are not supported");

			var testMethods = new List<TestMethod>();
	        using var emitterPool = new EmitterPool();
	        foreach (var mock in mocks) mock.BeforeInjection(executeFuncDef);
	        var testMethod = new TestMethod(this, executeFuncDef, emitterPool);
			testMethod.ProcessCommonOriginalContracts(mocks.OfType<SourceMemberMock>());
	        testMethod.RewriteAndProcessContracts(mocks);
	        foreach (var mock in mocks) mock.AfterInjection(emitterPool.GetEmitter(executeFuncDef.Body));
	        testMethods.Add(testMethod);

			foreach (var ctor in _typeInfo.GetMethods(m => m.Name is ".ctor" or ".cctor"))
			{
				var testCtor = new TestMethod(this, ctor, emitterPool);
				testCtor.RewriteAndProcessContracts(Enumerable.Empty<IMock>());
				testMethods.Add(testCtor);
			}

			foreach (var method in testMethods)
			{
				method.Rewrite(_replaceContractMocks);
			}
		}
		
		private class TestMethod
        {
            private readonly FakeProcessor _gen;
            private readonly MethodDefinition _originalMethod;
            private readonly IEmitterPool _emitterPool;
            private readonly HashSet<string> _methodContracts;
            private readonly List<MethodDefinition> _methods;

            public TestMethod(FakeProcessor gen, MethodDefinition originalMethod, IEmitterPool emitterPool)
            {
                _gen = gen;
                _originalMethod = originalMethod;
                _emitterPool = emitterPool;
                _methodContracts = new HashSet<string>();
                _methods = new List<MethodDefinition>();
            }

            public void RewriteAndProcessContracts(IEnumerable<IMock> mocks)
            {
                Rewrite(mocks);
				ProcessAllOriginalMethodContractsWithMocks(_originalMethod, replaceCtors: false);
				foreach (var methodDef in _methods.Where(m => m != _originalMethod))
				{
					ProcessAllOriginalMethodContracts(methodDef);
				}
            }

			public void Rewrite(IEnumerable<IMock> mocks)
            {
	            Rewrite(mocks, _originalMethod);
            }

			private void Rewrite(IEnumerable<IMock> mocks, MethodDefinition methodDef)
            {
	            _methodContracts.Clear();
				Rewrite(mocks, methodDef, Enumerable.Empty<MethodDefinition>());
			}

            private void Rewrite(IEnumerable<IMock> mocks, MethodDefinition? currentMethod, IEnumerable<MethodDefinition> parents)
            {
				if (currentMethod == null || !_methodContracts.Add(currentMethod.ToString())) return;
				_methods.Add(currentMethod);

				if (currentMethod.DeclaringType.IsInterface)
				{
					foreach (var typeDef in _gen._typeInfo.TypeMap.GetAllParentsAndDescendants(currentMethod.DeclaringType))
					{
						var method = _gen._typeInfo.GetMethod(typeDef, currentMethod);
						if (method != null) Rewrite(mocks, method, GetParents(parents, currentMethod));
					}
				}

                if (currentMethod.IsVirtual && (_gen._options.IncludeAllVirtualMembers ||
                    _gen._options.VirtualMembers.Contains(currentMethod.Name)))
                {
                    foreach (var virtualMethod in _gen._typeInfo.GetDerivedVirtualMethods(currentMethod))
                    {
                        Rewrite(mocks, virtualMethod, GetParents(parents, currentMethod));
                    }
                }

                if (currentMethod.IsAsync(out var asyncMethod))
                {
                    Rewrite(mocks, asyncMethod, GetParents(parents, currentMethod));
                }

                if (currentMethod.Body != null)
                {
	                ProcessInstructions(mocks, currentMethod, parents);
                }
            }

            private void ProcessInstructions(IEnumerable<IMock> mocks, MethodDefinition currentMethod, IEnumerable<MethodDefinition> parents)
            {
	            foreach (var instruction in currentMethod.Body.Instructions.ToList())
	            {
		            var originalInstruction = true;
		            var instructionRef = instruction;

		            if (instructionRef.Operand is MethodReference method && ShouldBeAnalyzed(method))
		            {
			            Rewrite(mocks, method.ToMethodDefinition(), GetParents(parents, currentMethod));
		            }

		            foreach (var mock in mocks)
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
		            }
	            }
            }

            private IEnumerable<MethodDefinition> GetParents(IEnumerable<MethodDefinition> parents, MethodDefinition currentMethod) => parents.Concat(new[] {currentMethod});

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
                    default: throw new NotSupportedException($"{_gen._options.AnalysisLevel} is not supported");
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

			public void ProcessCommonOriginalContracts(IEnumerable<SourceMemberMock> sourceMemberMocks)
			{
				foreach (var mock in sourceMemberMocks)
				{
					if (mock is ReplaceMock replaceMock && replaceMock.ReturnType?.Module == _gen._typeInfo.SourceType.Module)
					{
						AddReplaceInterfaceCallMocks(_gen._typeInfo.GetTypeDefinition(replaceMock.ReturnType));
					}

					if (mock.SourceMember.OriginalMember is MethodBase method &&
					    method.Module == _gen._typeInfo.SourceType.Module && method.DeclaringType != null)
					{
						var typeDef = _gen._typeInfo.GetTypeDefinition(method.DeclaringType);
						var methodRef = _gen._typeInfo.ImportReference(method);
						ProcessAllOriginalMethodContractsWithMocks(_gen._typeInfo.GetMethod(typeDef, methodRef), replaceCtors: true);
					}
				}
			}

			private void ProcessAllOriginalMethodContractsWithMocks(MethodDefinition methodDef, bool replaceCtors)
			{
				foreach (var typeDef in _gen._typeInfo.TypeMap.GetAllParentsAndDescendants(methodDef.DeclaringType))
				{
					var method = _gen._typeInfo.GetMethod(typeDef, methodDef);
					if (method != null) ProcessOriginalMethodContractWithMocks(method, replaceCtors);
				}
			}

			private void ProcessOriginalMethodContractWithMocks(MethodDefinition methodDef, bool replaceCtors)
			{
				if (methodDef.ReturnType != null && methodDef.ReturnType.FullName != "System.Void" && _gen._typeInfo.IsInFakeModule(methodDef.ReturnType))
				{
					AddReplaceTypeMocks(methodDef.ReturnType.ToTypeDefinition());
					methodDef.ReturnType = _gen._typeInfo.CreateImportedTypeReference(methodDef.ReturnType);
				}

				foreach (var parameterDef in methodDef.Parameters.Where(parameterDef => _gen._typeInfo.IsInFakeModule(parameterDef.ParameterType)))
				{
					var typeDefinition = parameterDef.ParameterType.ToTypeDefinition();
					AddReplaceInterfaceCallMocks(typeDefinition);
					if (replaceCtors) AddReplaceTypeMocks(typeDefinition);
					parameterDef.ParameterType = _gen._typeInfo.CreateImportedTypeReference(parameterDef.ParameterType);
				}
			}

			private void AddReplaceTypeMocks(TypeDefinition typeDef)
			{
				foreach (var mockTypeDef in _gen._typeInfo.TypeMap.GetAllParentsAndDescendants(typeDef))
				{
					var importedTypeRef = _gen._typeInfo.CreateImportedTypeReference(mockTypeDef);
					if (!mockTypeDef.IsInterface)
					{
						_gen._replaceContractMocks.Add(mockTypeDef.IsValueType ? new ReplaceValueTypeCtorMock(importedTypeRef) : new ReplaceReferenceTypeCtorMock(importedTypeRef));
					}
					_gen._replaceContractMocks.Add(mockTypeDef.IsValueType ? new ReplaceValueTypeCastMock(importedTypeRef) : new ReplaceReferenceTypeCastMock(importedTypeRef));
					TryAddImportedType(mockTypeDef, importedTypeRef);
				}
			}

			private void AddReplaceInterfaceCallMocks(TypeDefinition typeDef)
			{
				foreach (var mockTypeDef in _gen._typeInfo.TypeMap.GetAllParentsAndDescendants(typeDef))
				{
					var importedTypeRef = _gen._typeInfo.CreateImportedTypeReference(mockTypeDef);
					if (mockTypeDef.IsInterface) _gen._replaceContractMocks.Add(new ReplaceInterfaceCallMock(importedTypeRef));
					_gen._replaceContractMocks.Add(mockTypeDef.IsValueType ? new ReplaceValueTypeCastMock(importedTypeRef) : new ReplaceReferenceTypeCastMock(importedTypeRef));
					TryAddImportedType(mockTypeDef, importedTypeRef);
				}
			}

			private void TryAddImportedType(TypeDefinition typeDef, TypeReference typeRef)
			{
				var contract = typeDef.ToString();
				if (!_gen._importedTypes.ContainsKey(contract)) _gen._importedTypes.Add(contract, typeRef);
			}

			private void ProcessAllOriginalMethodContracts(MethodDefinition methodDef)
			{
				foreach (var typeDef in _gen._typeInfo.TypeMap.GetAllParentsAndDescendants(methodDef.DeclaringType))
				{
					var method = _gen._typeInfo.GetMethod(typeDef, methodDef);
					if (method != null)
					{
						ProcessOriginalMethodContract(method);
					}
				}
			}

			private void ProcessOriginalMethodContract(MethodDefinition methodDef)
			{
				if (_gen._importedTypes.TryGetValue(methodDef.ReturnType.ToString(), out var importedType))
				{
					methodDef.ReturnType = importedType;
				}

				foreach (var parameterDef in methodDef.Parameters)
				{
					if (_gen._importedTypes.TryGetValue(parameterDef.ParameterType.ToString(), out importedType))
					{
						parameterDef.ParameterType = importedType;
					}
				}
			}
		}
    }
}

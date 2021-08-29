﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Expression;
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

        public void ProcessMethod(IEnumerable<IMock> mocks, IInvocationExpression invocationExpression)
        {
	        var visitor = new GetTestMethodVisitor();
	        invocationExpression.AcceptMemberVisitor(visitor);
			var executeFuncRef = _typeInfo.ImportReference(visitor.Method);
	        var executeFuncDef = _typeInfo.GetMethod(executeFuncRef, searchInBaseType: true);
	        if (executeFuncDef?.Body == null) throw new InvalidOperationException("Methods without body are not supported");

			var testMethods = new List<TestMethod>();
	        using var emitterPool = new EmitterPool();
	        foreach (var mock in mocks) mock.BeforeInjection(executeFuncDef);
	        var testMethod = new TestMethod(this, executeFuncDef, emitterPool);
			testMethod.ProcessCommonOriginalContracts(mocks.OfType<SourceMemberMock>());
	        testMethod.RewriteAndProcessContracts(mocks, 
		        invocationExpression.GetSourceMember().GetGenericArguments(_typeInfo));
	        foreach (var mock in mocks) mock.AfterInjection(emitterPool.GetEmitter(executeFuncDef.Body));
	        testMethods.Add(testMethod);

			foreach (var ctor in _typeInfo.GetMethods(m => m.Name is ".ctor" or ".cctor"))
			{
				var testCtor = new TestMethod(this, ctor, emitterPool);
				testCtor.RewriteAndProcessContracts(Enumerable.Empty<IMock>(), Enumerable.Empty<GenericArgument>());
				testMethods.Add(testCtor);
			}

			foreach (var method in testMethods)
			{
				method.Rewrite(_replaceContractMocks, Enumerable.Empty<GenericArgument>());
			}
		}

        private IEnumerable<GenericArgument> GetGenericArguments(MethodReference methodRef, MethodDefinition methodDef)
        {
			if (methodRef is GenericInstanceMethod genericInstanceMethod)
			{
		        for (var i = 0; i < genericInstanceMethod.GenericArguments.Count; i++)
		        {
			        var genericArgument = genericInstanceMethod.GenericArguments[i];
			        var declaringType = methodDef.DeclaringType.ToString();
			        yield return new GenericArgument(
				        methodDef.GenericParameters[i].Name,
				        genericArgument.ToString(),
				        declaringType,
				        GetGenericDeclaringType(genericArgument as GenericParameter));
		        }
	        }

			if (methodRef.DeclaringType is GenericInstanceType genericInstanceType)
			{
				for (var i = 0; i < genericInstanceType.GenericArguments.Count; i++)
				{
					var genericArgument = genericInstanceType.GenericArguments[i];
					var declaringType = methodDef.DeclaringType.ToString();
					yield return new GenericArgument(
						methodDef.DeclaringType.GenericParameters[i].Name,
						genericArgument.ToString(),
						declaringType,
						GetGenericDeclaringType(genericArgument as GenericParameter));
				}
			}
        }

        private string? GetGenericDeclaringType(GenericParameter? genericArgument)
        {
	        return genericArgument != null
		        ? genericArgument.DeclaringType?.ToString() ?? genericArgument.DeclaringMethod.DeclaringType.ToString()
		        : null;
        }

        private IEnumerable<GenericArgument> GetGenericArguments(FieldReference fieldRef)
        {
	        var fieldDef = fieldRef.ToFieldDefinition();
	        if (fieldRef.DeclaringType is GenericInstanceType genericInstanceType)
	        {
		        for (var i = 0; i < genericInstanceType.GenericArguments.Count; i++)
		        {
			        var genericArgument = genericInstanceType.GenericArguments[i];
			        var declaringType = fieldDef.DeclaringType.ToString();
			        yield return new GenericArgument(
				        fieldDef.DeclaringType.GenericParameters[i].Name,
				        genericArgument.ToString(),
				        declaringType,
				        GetGenericDeclaringType(genericArgument as GenericParameter));
		        }
	        }
        }

		private class TestMethod
        {
            private readonly FakeProcessor _gen;
            private readonly MethodDefinition _originalMethod;
            private readonly IEmitterPool _emitterPool;
            private readonly HashSet<string> _methodContracts;
            private readonly List<MethodDefinition> _methods;
            private readonly HashSet<MethodDefinition> _implementations;

            public TestMethod(FakeProcessor gen, MethodDefinition originalMethod, IEmitterPool emitterPool)
            {
                _gen = gen;
                _originalMethod = originalMethod;
                _emitterPool = emitterPool;
                _methodContracts = new HashSet<string>();
                _methods = new List<MethodDefinition>();
                _implementations = new HashSet<MethodDefinition>();
            }

            public void RewriteAndProcessContracts(IEnumerable<IMock> mocks, IEnumerable<GenericArgument> genericArgs)
            {
                Rewrite(mocks, genericArgs);
				ProcessAllOriginalMethodContractsWithMocks(_originalMethod);
				foreach (var methodDef in _methods.Where(m => m != _originalMethod))
				{
					ProcessOriginalMethodContract(methodDef);
				}
            }

			public void Rewrite(IEnumerable<IMock> mocks, IEnumerable<GenericArgument> genericArgs)
            {
	            Rewrite(mocks, _originalMethod, genericArgs);
            }

			private void Rewrite(IEnumerable<IMock> mocks, MethodDefinition methodDef, IEnumerable<GenericArgument> genericArgs)
            {
				_methods.Clear();
	            _methodContracts.Clear();
				_implementations.Clear();
				Rewrite(mocks, methodDef, Enumerable.Empty<MethodDefinition>(), genericArgs);
			}

            private void Rewrite(IEnumerable<IMock> mocks, MethodDefinition? currentMethod, IEnumerable<MethodDefinition> parents, IEnumerable<GenericArgument> genericArgs)
            {
				if (currentMethod == null || !CheckAnalysisLevel(currentMethod) || !CheckVirtualMember(currentMethod) || !_methodContracts.Add(currentMethod.ToString())) return;
				_methods.Add(currentMethod);

				if ((currentMethod.DeclaringType.IsInterface || currentMethod.IsVirtual) && !_implementations.Contains(currentMethod))
				{
					var implementations = _gen._typeInfo.GetAllImplementations(currentMethod, includeAffectedAssemblies: true);
					foreach (var implementation in implementations)
					{
						_implementations.Add(implementation);
					}

					foreach (var methodDef in implementations)
					{
						Rewrite(mocks, methodDef, GetParents(parents, currentMethod), genericArgs);
					}
				}

                if (currentMethod.IsAsync(out var asyncMethod))
                {
                    Rewrite(mocks, asyncMethod, GetParents(parents, currentMethod), genericArgs);
                }

                if (currentMethod.Body != null)
                {
	                ProcessInstructions(mocks, currentMethod, parents, genericArgs);
                }
            }

            private void ProcessInstructions(IEnumerable<IMock> mocks, MethodDefinition currentMethod, IEnumerable<MethodDefinition> parents, IEnumerable<GenericArgument> genericArgs)
            {
	            foreach (var instruction in currentMethod.Body.Instructions.ToList())
	            {
		            var originalInstruction = true;
		            var instructionRef = instruction;

		            if (instructionRef.Operand is MethodReference method)
		            {
			            var methodDefinition = method.ToMethodDefinition();
			            genericArgs = _gen.GetGenericArguments(method, methodDefinition).Concat(genericArgs);
			            Rewrite(mocks, methodDefinition, GetParents(parents, currentMethod), genericArgs);
		            }
		            else if (instructionRef.Operand is FieldReference field)
		            {
			            genericArgs = _gen.GetGenericArguments(field).Concat(genericArgs);
					}

					foreach (var mock in mocks)
		            {
			            if (mock.IsSourceInstruction(_originalMethod, instructionRef, genericArgs))
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

            private bool CheckAnalysisLevel(MethodReference methodRef)
            {
	            switch (_gen._options.AnalysisLevel)
	            {
		            case AnalysisLevels.Type:
		            {
			            if (methodRef.DeclaringType.FullName == _originalMethod.DeclaringType.FullName &&
			                methodRef.Module.Assembly == _originalMethod.Module.Assembly) return true;
			            break;
		            }
		            case AnalysisLevels.Assembly:
		            {
			            if (methodRef.Module.Assembly == _originalMethod.Module.Assembly) return true;
			            break;
		            }
		            case AnalysisLevels.AllExceptSystemAndMicrosoft:
		            {
			            return !methodRef.DeclaringType.Namespace.StartsWith(nameof(System)) &&
			                   !methodRef.DeclaringType.Namespace.StartsWith(nameof(Microsoft));
		            }
		            default: throw new NotSupportedException($"{_gen._options.AnalysisLevel} is not supported");
	            }

				return _gen._typeInfo.IsInReferencedAssembly(methodRef.DeclaringType.Module.Assembly);
            }

            private bool CheckVirtualMember(MethodDefinition method)
            {
	            if (!method.IsVirtual) return true;
	            if (_gen._options.DisableVirtualMembers) return false;
	            var contract = method.ToMethodContract();
	            return _gen._options.AllowedVirtualMembers.Count == 0 || _gen._options.AllowedVirtualMembers.Any(m => m(contract));
			}

			public void ProcessCommonOriginalContracts(IEnumerable<SourceMemberMock> sourceMemberMocks)
			{
				foreach (var mock in sourceMemberMocks)
				{
					if (mock is ReplaceMock replaceMock && replaceMock.ReturnType?.Module == _gen._typeInfo.SourceType.Module)
					{
						AddReplaceContractMocks(_gen._typeInfo.GetTypeDefinition(replaceMock.ReturnType));
					}

					if (mock.SourceMember.OriginalMember is MethodBase method &&
					    method.Module == _gen._typeInfo.SourceType.Module && method.DeclaringType != null)
					{
						var typeDef = _gen._typeInfo.GetTypeDefinition(method.DeclaringType);
						var methodRef = _gen._typeInfo.ImportReference(method);
						var methodDef = _gen._typeInfo.GetMethod(typeDef, methodRef);
						if (methodDef != null)
						{
							ProcessAllOriginalMethodContractsWithMocks(methodDef);
						}
					}
				}
			}

			private void ProcessAllOriginalMethodContractsWithMocks(MethodDefinition methodDef)
			{
				foreach (var method in _gen._typeInfo.GetAllImplementations(methodDef))
				{
					ProcessOriginalMethodContractWithMocks(method);
				}
			}

			private void ProcessOriginalMethodContractWithMocks(MethodDefinition methodDef)
			{
				if (methodDef.ReturnType != null && methodDef.ReturnType.FullName != "System.Void" && _gen._typeInfo.IsInFakeModule(methodDef.ReturnType))
				{
					AddReplaceContractMocks(methodDef.ReturnType.ToTypeDefinition());
					methodDef.ReturnType = _gen._typeInfo.CreateImportedTypeReference(methodDef.ReturnType);
				}

				foreach (var parameterDef in methodDef.Parameters.Where(parameterDef => _gen._typeInfo.IsInFakeModule(parameterDef.ParameterType)))
				{
					var typeDefinition = parameterDef.ParameterType.ToTypeDefinition();
					AddReplaceContractMocks(typeDefinition);
					parameterDef.ParameterType = _gen._typeInfo.CreateImportedTypeReference(parameterDef.ParameterType);
				}
			}

			private void AddReplaceContractMocks(TypeDefinition typeDef)
			{
				foreach (var mockTypeDef in _gen._typeInfo.TypeMap.GetAllParentsAndDescendants(typeDef))
				{
					var importedTypeRef = _gen._typeInfo.CreateImportedTypeReference(mockTypeDef);
					if (mockTypeDef.IsInterface)
					{
						_gen._replaceContractMocks.Add(new ReplaceInterfaceCallMock(importedTypeRef));
					}
					else
					{
						_gen._replaceContractMocks.Add(mockTypeDef.IsValueType ? new ReplaceValueTypeCtorMock(importedTypeRef) : new ReplaceReferenceTypeCtorMock(importedTypeRef));
					}
					_gen._replaceContractMocks.Add(mockTypeDef.IsValueType ? new ReplaceValueTypeCastMock(importedTypeRef) : new ReplaceReferenceTypeCastMock(importedTypeRef));
					TryAddImportedType(mockTypeDef, importedTypeRef);
				}
			}

			private void TryAddImportedType(TypeDefinition typeDef, TypeReference typeRef)
			{
				var contract = typeDef.ToString();
				if (!_gen._importedTypes.ContainsKey(contract)) _gen._importedTypes.Add(contract, typeRef);
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

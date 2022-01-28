using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutoFake.Expression;
using AutoFake.Setup.Mocks;
using Mono.Cecil;

namespace AutoFake
{
    internal class FakeProcessor : IFakeProcessor
    {
        private readonly ITypeInfo _typeInfo;
        private readonly IAssemblyWriter _assemblyWriter;
        private readonly IMemberVisitorFactory _memberVisitorFactory;
        private readonly IContractProcessor _contractProcessor;
        private readonly Func<IEmitterPool> _createEmitterPool;
        private readonly Func<MethodDefinition, IEmitterPool, TestMethod> _createTestMethod;

        public FakeProcessor(ITypeInfo typeInfo, IAssemblyWriter assemblyWriter, IMemberVisitorFactory memberVisitorFactory, IContractProcessor contractProcessor,
	        Func<IEmitterPool> createEmitterPool, Func<MethodDefinition, IEmitterPool, TestMethod> createTestMethod)
        {
            _typeInfo = typeInfo;
            _assemblyWriter = assemblyWriter;
            _memberVisitorFactory = memberVisitorFactory;
            _contractProcessor = contractProcessor;
            _createEmitterPool = createEmitterPool;
            _createTestMethod = createTestMethod;
        }

		[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
		public void ProcessMethod(IEnumerable<IMock> mocks, IInvocationExpression invocationExpression)
        {
	        var executeFuncDef = GetMethodDefinition(invocationExpression);
	        var testMethods = new List<TestMethod>();
	        using var emitterPool = _createEmitterPool();
	        foreach (var mock in mocks) mock.BeforeInjection(executeFuncDef);
            var replaceContractMocks = new HashSet<IMock>();
            _contractProcessor.ProcessCommonOriginalContracts(mocks.OfType<ISourceMemberMock>(), replaceContractMocks);
	        var testMethod = _createTestMethod(executeFuncDef, emitterPool);
	        Rewrite(testMethod, executeFuncDef, mocks, invocationExpression.GetSourceMember().GetGenericArguments(), replaceContractMocks);
	        foreach (var mock in mocks) mock.AfterInjection(emitterPool.GetEmitter(executeFuncDef.Body));
	        testMethods.Add(testMethod);
			ProcessConstructors(emitterPool, replaceContractMocks, testMethods);

			foreach (var method in testMethods)
			{
				method.Rewrite(replaceContractMocks, Enumerable.Empty<GenericArgument>());
			}
		}

		private void ProcessConstructors(IEmitterPool emitterPool, HashSet<IMock> replaceContractMocks, ICollection<TestMethod> testMethods)
		{
			foreach (var ctor in _typeInfo.GetMethods(m => m.Name is ".ctor" or ".cctor"))
			{
				var testCtor = _createTestMethod(ctor, emitterPool);
				Rewrite(testCtor, ctor, Enumerable.Empty<IMock>(), Enumerable.Empty<GenericArgument>(), replaceContractMocks);
				testMethods.Add(testCtor);
			}
		}

		private void Rewrite(TestMethod testMethod, MethodDefinition originalMethod, IEnumerable<IMock> mocks, IEnumerable<GenericArgument> genericArgs, HashSet<IMock> replaceContractMocks)
		{
			var methods = testMethod.Rewrite(mocks, genericArgs);
			_contractProcessor.ProcessAllOriginalMethodContractsWithMocks(originalMethod, replaceContractMocks);
			foreach (var methodDef in methods.Where(m => m != originalMethod))
			{
				_contractProcessor.ProcessOriginalMethodContract(methodDef);
			}
		}

		private MethodDefinition GetMethodDefinition(IInvocationExpression invocationExpression)
        {
	        var visitor = _memberVisitorFactory.GetMemberVisitor<GetTestMethodVisitor>();
	        invocationExpression.AcceptMemberVisitor(visitor);
	        var executeFuncRef = _assemblyWriter.ImportToSourceAsm(visitor.Method);
	        var executeFuncDef = _typeInfo.GetMethod(executeFuncRef, searchInBaseType: true);
	        if (executeFuncDef?.Body == null) throw new InvalidOperationException("Methods without body are not supported");
	        
	        return executeFuncDef;
        }
    }
}

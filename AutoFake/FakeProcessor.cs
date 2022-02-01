using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Expression;
using AutoFake.Abstractions.Setup.Mocks;
using AutoFake.Expression;
using Mono.Cecil;

namespace AutoFake
{
    internal class FakeProcessor : IFakeProcessor
    {
        private readonly ITypeInfo _typeInfo;
        private readonly IMemberVisitorFactory _memberVisitorFactory;
        private readonly IContractProcessor _contractProcessor;
        private readonly Func<IEmitterPool> _createEmitterPool;
        private readonly Func<IEmitterPool, TestMethod> _createTestMethod;

        public FakeProcessor(ITypeInfo typeInfo, IMemberVisitorFactory memberVisitorFactory, IContractProcessor contractProcessor,
	        Func<IEmitterPool> createEmitterPool, Func<IEmitterPool, TestMethod> createTestMethod)
        {
            _typeInfo = typeInfo;
            _memberVisitorFactory = memberVisitorFactory;
            _contractProcessor = contractProcessor;
            _createEmitterPool = createEmitterPool;
            _createTestMethod = createTestMethod;
        }

		[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
		public void ProcessMethod(IEnumerable<IMock> mocks, IInvocationExpression invocationExpression, IFakeOptions options)
        {
	        var executeFuncDef = GetMethodDefinition(invocationExpression);
	        var testMethods = new List<Tuple<TestMethod, MethodDefinition>>();
	        using var emitterPool = _createEmitterPool();
	        foreach (var mock in mocks) mock.BeforeInjection(executeFuncDef);
            var replaceContractMocks = new HashSet<IMock>();
            _contractProcessor.ProcessCommonOriginalContracts(mocks.OfType<ISourceMemberMock>(), replaceContractMocks);
	        var testMethod = _createTestMethod(emitterPool);
			var methods = testMethod.Rewrite(executeFuncDef, options, mocks, invocationExpression.GetSourceMember().GetGenericArguments());
	        Rewrite(methods, executeFuncDef, replaceContractMocks);
	        foreach (var mock in mocks) mock.AfterInjection(emitterPool.GetEmitter(executeFuncDef.Body));
	        testMethods.Add(new (testMethod, executeFuncDef));
			ProcessConstructors(emitterPool, replaceContractMocks, options, testMethods);

			foreach (var method in testMethods)
			{
				method.Item1.Rewrite(method.Item2, options, replaceContractMocks, Enumerable.Empty<GenericArgument>());
			}
		}

		private void ProcessConstructors(IEmitterPool emitterPool, HashSet<IMock> replaceContractMocks,
			IFakeOptions options, ICollection<Tuple<TestMethod, MethodDefinition>> testMethods)
		{
			foreach (var ctor in _typeInfo.GetMethods(m => m.Name is ".ctor" or ".cctor"))
			{
				var testCtor = _createTestMethod(emitterPool);
				var methods = testCtor.Rewrite(ctor, options, Enumerable.Empty<IMock>(), Enumerable.Empty<GenericArgument>());
				Rewrite(methods, ctor, replaceContractMocks);
				testMethods.Add(new (testCtor, ctor));
			}
		}

		private void Rewrite(IEnumerable<MethodDefinition> methods, MethodDefinition originalMethod, HashSet<IMock> replaceContractMocks)
		{
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
	        var executeFuncRef = _typeInfo.ImportToSourceAsm(visitor.Method);
	        var executeFuncDef = _typeInfo.GetMethod(executeFuncRef, searchInBaseType: true);
	        if (executeFuncDef?.Body == null) throw new InvalidOperationException("Methods without body are not supported");
	        
	        return executeFuncDef;
        }
    }
}

using System;
using System.Collections.Generic;
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
        private readonly FakeOptions _options;
        private readonly ContractProcessor _contractProcessor;

		public FakeProcessor(ITypeInfo typeInfo, IAssemblyWriter assemblyWriter, FakeOptions fakeOptions)
        {
            _typeInfo = typeInfo;
            _assemblyWriter = assemblyWriter;
            _options = fakeOptions;
			_contractProcessor = new ContractProcessor(_typeInfo, _assemblyWriter);
        }

		public void ProcessMethod(IEnumerable<IMock> mocks, IInvocationExpression invocationExpression)
        {
	        var executeFuncDef = GetMethodDefinition(invocationExpression);
	        var testMethods = new List<TestMethod>();
	        using var emitterPool = new EmitterPool();
	        foreach (var mock in mocks) mock.BeforeInjection(executeFuncDef);
            var replaceContractMocks = new HashSet<IMock>();
            _contractProcessor.ProcessCommonOriginalContracts(mocks.OfType<SourceMemberMock>(), replaceContractMocks);
	        var testMethod = new TestMethod(executeFuncDef, emitterPool, _typeInfo, _options, _contractProcessor, _assemblyWriter);
	        testMethod.RewriteAndProcessContracts(
		        mocks, 
		        invocationExpression.GetSourceMember().GetGenericArguments(_assemblyWriter),
		        replaceContractMocks);
	        foreach (var mock in mocks) mock.AfterInjection(emitterPool.GetEmitter(executeFuncDef.Body));
	        testMethods.Add(testMethod);
			ProcessConstructors(emitterPool, replaceContractMocks, testMethods);

			foreach (var method in testMethods)
			{
				method.Rewrite(replaceContractMocks, Enumerable.Empty<GenericArgument>());
			}
		}

		private void ProcessConstructors(EmitterPool emitterPool, HashSet<IMock> replaceContractMocks, ICollection<TestMethod> testMethods)
		{
			foreach (var ctor in _typeInfo.GetMethods(m => m.Name is ".ctor" or ".cctor"))
			{
				var testCtor = new TestMethod(ctor, emitterPool, _typeInfo, _options, _contractProcessor, _assemblyWriter);
				testCtor.RewriteAndProcessContracts(
					Enumerable.Empty<IMock>(),
					Enumerable.Empty<GenericArgument>(),
					replaceContractMocks);
				testMethods.Add(testCtor);
			}
		}

		private MethodDefinition GetMethodDefinition(IInvocationExpression invocationExpression)
        {
	        var visitor = new GetTestMethodVisitor();
	        invocationExpression.AcceptMemberVisitor(visitor);
	        var executeFuncRef = _assemblyWriter.ImportToSourceAsm(visitor.Method);
	        var executeFuncDef = _typeInfo.GetMethod(executeFuncRef, searchInBaseType: true);
	        if (executeFuncDef?.Body == null) throw new InvalidOperationException("Methods without body are not supported");
	        
	        return executeFuncDef;
        }
    }
}

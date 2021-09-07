using System;
using System.Collections.Generic;
using System.Linq;
using AutoFake.Expression;
using AutoFake.Setup.Mocks;

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

        public void ProcessMethod(IEnumerable<IMock> mocks, IInvocationExpression invocationExpression)
        {
	        var visitor = new GetTestMethodVisitor();
	        invocationExpression.AcceptMemberVisitor(visitor);
			var executeFuncRef = _typeInfo.ImportReference(visitor.Method);
	        var executeFuncDef = _typeInfo.GetMethod(executeFuncRef, searchInBaseType: true);
	        if (executeFuncDef?.Body == null) throw new InvalidOperationException("Methods without body are not supported");

			var testMethods = new List<TestMethod>();
			var contractProcessor = new ContractProcessor(_typeInfo);
	        using var emitterPool = new EmitterPool();
	        foreach (var mock in mocks) mock.BeforeInjection(executeFuncDef);
	        var testMethod = new TestMethod(executeFuncDef, emitterPool, _typeInfo, _options, contractProcessor);
            var replaceContractMocks = new HashSet<IMock>();
            contractProcessor.ProcessCommonOriginalContracts(mocks.OfType<SourceMemberMock>(), replaceContractMocks);
	        testMethod.RewriteAndProcessContracts(
		        mocks, 
		        invocationExpression.GetSourceMember().GetGenericArguments(_typeInfo),
		        replaceContractMocks);
	        foreach (var mock in mocks) mock.AfterInjection(emitterPool.GetEmitter(executeFuncDef.Body));
	        testMethods.Add(testMethod);

			foreach (var ctor in _typeInfo.GetMethods(m => m.Name is ".ctor" or ".cctor"))
			{
				var testCtor = new TestMethod(ctor, emitterPool, _typeInfo, _options, contractProcessor);
				testCtor.RewriteAndProcessContracts(
					Enumerable.Empty<IMock>(),
					Enumerable.Empty<GenericArgument>(),
					replaceContractMocks);
				testMethods.Add(testCtor);
			}

			foreach (var method in testMethods)
			{
				method.Rewrite(replaceContractMocks, Enumerable.Empty<GenericArgument>());
			}
		}
    }
}

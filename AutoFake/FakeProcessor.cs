using System;
using System.Collections.Generic;
using System.Linq;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Expression;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Mocks;
using Mono.Cecil;

namespace AutoFake;

internal class FakeProcessor : IFakeProcessor
{
	private readonly ITypeInfo _typeInfo;
	private readonly IMemberVisitorFactory _memberVisitorFactory;
	private readonly Func<IMockCollection, IContractProcessor> _createContractProcessor;
	private readonly Func<IEmitterPool> _createEmitterPool;
	private readonly Func<IEmitterPool, ITestMethod> _createTestMethod;

	public FakeProcessor(ITypeInfo typeInfo, IMemberVisitorFactory memberVisitorFactory,
		Func<IMockCollection, IContractProcessor> createContractProcessor,
		Func<IEmitterPool> createEmitterPool, Func<IEmitterPool, ITestMethod> createTestMethod)
	{
		_typeInfo = typeInfo;
		_memberVisitorFactory = memberVisitorFactory;
		_createContractProcessor = createContractProcessor;
		_createEmitterPool = createEmitterPool;
		_createTestMethod = createTestMethod;
	}

	public void ProcessMethod(IMockCollection mockCollection, IInvocationExpression invocationExpression, IFakeOptions options)
	{
		var contractProcessor = _createContractProcessor(mockCollection);
		var executeFuncDef = GetMethodDefinition(invocationExpression);
		var testMethods = new List<Tuple<ITestMethod, MethodDefinition>>();
		using var emitterPool = _createEmitterPool();
		foreach (var mock in mockCollection.Mocks) mock.BeforeInjection(executeFuncDef);
		contractProcessor.ProcessCommonOriginalContracts(mockCollection.Mocks.OfType<ISourceMemberMock>());
		var testMethod = _createTestMethod(emitterPool);
		var methods = testMethod.Rewrite(executeFuncDef, options, mockCollection.Mocks, invocationExpression.GetSourceMember().GetGenericArguments());
		Rewrite(methods, executeFuncDef, contractProcessor);
		foreach (var mock in mockCollection.Mocks) mock.AfterInjection(emitterPool.GetEmitter(executeFuncDef.Body));
		testMethods.Add(new(testMethod, executeFuncDef));
		ProcessConstructors(emitterPool, options, contractProcessor, testMethods);

		foreach (var method in testMethods)
		{
			method.Item1.Rewrite(method.Item2, options, mockCollection.ContractMocks, Enumerable.Empty<GenericArgument>());
		}
	}

	private void ProcessConstructors(IEmitterPool emitterPool, IFakeOptions options,
		IContractProcessor contractProcessor, ICollection<Tuple<ITestMethod, MethodDefinition>> testMethods)
	{
		foreach (var ctor in _typeInfo.GetConstructors())
		{
			var testCtor = _createTestMethod(emitterPool);
			var methods = testCtor.Rewrite(ctor, options, Enumerable.Empty<IMock>(), Enumerable.Empty<GenericArgument>());
			Rewrite(methods, ctor, contractProcessor);
			testMethods.Add(new(testCtor, ctor));
		}
	}

	private void Rewrite(IEnumerable<MethodDefinition> methods, MethodDefinition originalMethod, IContractProcessor contractProcessor)
	{
		contractProcessor.ProcessAllOriginalMethodContractsWithMocks(originalMethod);
		foreach (var methodDef in methods.Where(m => m != originalMethod))
		{
			contractProcessor.ProcessOriginalMethodContract(methodDef);
		}
	}

	private MethodDefinition GetMethodDefinition(IInvocationExpression invocationExpression)
	{
		var visitor = _memberVisitorFactory.GetMemberVisitor<IGetTestMethodVisitor>();
		var method = invocationExpression.AcceptMemberVisitor(visitor);
		var executeFuncDef = _typeInfo.GetMethod(method, searchInBaseType: true);
		if (executeFuncDef == null) throw new MissingMethodException(method.DeclaringType?.FullName, method.Name);
		if (executeFuncDef.Body == null) throw new NotSupportedException("Methods without body are not supported");

		return executeFuncDef;
	}
}

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
	private readonly Func<IEmitterPool> _createEmitterPool;
	private readonly Func<IEmitterPool, ITestMethod> _createTestMethod;

	public FakeProcessor(ITypeInfo typeInfo, IMemberVisitorFactory memberVisitorFactory,
		Func<IEmitterPool> createEmitterPool, Func<IEmitterPool, ITestMethod> createTestMethod)
	{
		_typeInfo = typeInfo;
		_memberVisitorFactory = memberVisitorFactory;
		_createEmitterPool = createEmitterPool;
		_createTestMethod = createTestMethod;
	}

	public void ProcessMethod(IMockCollection mockCollection, IInvocationExpression invocationExpression, IFakeOptions options)
	{
		var executeFuncDef = GetMethodDefinition(invocationExpression);
		var testMethods = new List<Tuple<ITestMethod, MethodDefinition>>();
		using var emitterPool = _createEmitterPool();
		foreach (var mock in mockCollection.Mocks) mock.BeforeInjection(executeFuncDef);
		var testMethod = _createTestMethod(emitterPool);
		var methods = testMethod.Rewrite(executeFuncDef, options, mockCollection.Mocks, invocationExpression.GetSourceMember().GetGenericArguments());
		foreach (var mock in mockCollection.Mocks) mock.AfterInjection(emitterPool.GetEmitter(executeFuncDef.Body));
		testMethods.Add(new(testMethod, executeFuncDef));
		ProcessConstructors(emitterPool, options, testMethods);
	}

	private void ProcessConstructors(IEmitterPool emitterPool, IFakeOptions options, ICollection<Tuple<ITestMethod, MethodDefinition>> testMethods)
	{
		foreach (var ctor in _typeInfo.GetConstructors())
		{
			var testCtor = _createTestMethod(emitterPool);
			testMethods.Add(new(testCtor, ctor));
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

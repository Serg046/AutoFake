using System;

namespace AutoFake.Expression
{
	internal interface IMemberVisitorFactory
	{
		T GetMemberVisitor<T>() where T : IMemberVisitor;
		GetValueMemberVisitor GetValueMemberVisitor(object? instance);
		TargetMemberVisitor GetTargetMemberVisitor(IMemberVisitor requestedVisitor, Type targetType);
	}
}
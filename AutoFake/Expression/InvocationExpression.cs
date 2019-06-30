using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Setup;
using Microsoft.CSharp.RuntimeBinder;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake.Expression
{
    internal class InvocationExpression : IInvocationExpression
    {
        private readonly LinqExpression _expression;

        public InvocationExpression(LinqExpression expression)
        {
            _expression = expression;
        }

        public void AcceptMemberVisitor(IMemberVisitor visitor) => AnalyzeExpression(_expression, visitor);

        private void AnalyzeExpression(LinqExpression expression, IMemberVisitor visitor)
        {
            try
            {
                Analyze((dynamic)expression, visitor);
            }
            catch (RuntimeBinderException)
            {
                throw new NotSupportedExpressionException(
                    $"Ivalid expression format. Type '{_expression.GetType().FullName}'. Source: {_expression}.");
            }
        }

        private void Analyze(LambdaExpression expression, IMemberVisitor visitor) => AnalyzeExpression(expression.Body, visitor);

        private void Analyze(UnaryExpression expression, IMemberVisitor visitor) => AnalyzeExpression(expression.Operand, visitor);

        private void Analyze(MethodCallExpression expression, IMemberVisitor visitor) => visitor.Visit(expression, expression.Method);

        private void Analyze(MemberExpression expression, IMemberVisitor visitor)
        {
            try
            {
                Analyze((dynamic)expression.Member, visitor);
            }
            catch (RuntimeBinderException)
            {
                throw new NotSupportedExpressionException(
                    $"Ivalid MemberExpression format. Type '{expression.Member.GetType().FullName}'. Source: {expression}.");
            }
        }

        private void Analyze(NewExpression expression, IMemberVisitor visitor) => visitor.Visit(expression, expression.Constructor);

        private void Analyze(PropertyInfo propertyInfo, IMemberVisitor visitor) => visitor.Visit(propertyInfo);

        private void Analyze(FieldInfo fieldInfo, IMemberVisitor visitor) => visitor.Visit(fieldInfo);

        //-----------------------------------------------------------------------------------------------------------

        public ISourceMember GetSourceMember()
        {
            var memberVisitor = new GetSourceMemberVisitor();
            AcceptMemberVisitor(memberVisitor);
            return memberVisitor.SourceMember;
        }

        private IList<FakeArgument> GetArguments()
        {
            var visitor = new GetArgumentsMemberVisitor();
            AcceptMemberVisitor(visitor);
            return visitor.Arguments;
        }

        public void MatchArguments(ICollection<object[]> arguments, bool checkArguments, Func<byte, bool> expectedCalls)
        {
            if (arguments.Count > byte.MaxValue) throw new InvalidOperationException($"Too many calls occurred - {arguments.Count}");
            if (expectedCalls != null && !expectedCalls((byte)arguments.Count))
            {
                throw new ExpectedCallsException($"Setup and actual calls are not matched. Actual value - {arguments.Count}.");
            }
            var fakeArguments = GetArguments();
            if (checkArguments)
            {
                foreach (var args in arguments)
                    for (var i = 0; i < args.Length; i++)
                    {
                        var fakeArgument = fakeArguments[i];
                        if (!fakeArgument.Check(args[i]))
                        {
                            throw new VerifiableException(
                                $"Setup and actual arguments are not matched. Expected - {fakeArgument}, actual - {args[i]}.");
                        }
                    }
            }
        }
    }
}

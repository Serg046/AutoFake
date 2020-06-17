using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using AutoFake.Exceptions;
using AutoFake.Setup;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake.Expression
{
    public class InvocationExpression : IInvocationExpression
    {
        private readonly LinqExpression _expression;
        private IList<IFakeArgument> _arguments;

        internal InvocationExpression(LinqExpression expression)
        {
            _expression = expression;
        }

        void IInvocationExpression.AcceptMemberVisitor(IMemberVisitor visitor) => AcceptMemberVisitor(visitor);
        internal void AcceptMemberVisitor(IMemberVisitor visitor) => AnalyzeExpression(_expression, visitor);

        private void AnalyzeExpression(LinqExpression expression, IMemberVisitor visitor)
        {
            switch (expression)
            {
                case LambdaExpression le: Analyze(le, visitor); break;
                case UnaryExpression ue: Analyze(ue, visitor); break;
                case MethodCallExpression mce: Analyze(mce, visitor); break;
                case NewExpression ne: Analyze(ne, visitor); break;
                case MemberExpression me: Analyze(me, visitor); break;
                default: throw new NotSupportedExpressionException($"Invalid expression format. Type '{_expression.GetType().FullName}'. Source: {_expression}.");
            }
        }

        private void Analyze(LambdaExpression expression, IMemberVisitor visitor) => AnalyzeExpression(expression.Body, visitor);

        private void Analyze(UnaryExpression expression, IMemberVisitor visitor) => AnalyzeExpression(expression.Operand, visitor);

        private void Analyze(MethodCallExpression expression, IMemberVisitor visitor) => visitor.Visit(expression, expression.Method);

        private void Analyze(NewExpression expression, IMemberVisitor visitor) => visitor.Visit(expression, expression.Constructor);
        
        [ExcludeFromCodeCoverage]
        private void Analyze(MemberExpression expression, IMemberVisitor visitor)
        {
            switch (expression.Member)
            {
                case FieldInfo fi: Analyze(fi, visitor); break;
                case PropertyInfo pi: Analyze(pi, visitor); break;
                default: throw new NotSupportedException($"'{expression.Member.GetType().FullName}' is not supported.");
            }
        }

        private void Analyze(PropertyInfo propertyInfo, IMemberVisitor visitor) => visitor.Visit(propertyInfo);

        private void Analyze(FieldInfo fieldInfo, IMemberVisitor visitor) => visitor.Visit(fieldInfo);

        //-----------------------------------------------------------------------------------------------------------

        ISourceMember IInvocationExpression.GetSourceMember() => GetSourceMember();
        internal ISourceMember GetSourceMember()
        {
            var memberVisitor = new GetSourceMemberVisitor();
            ((IInvocationExpression)this).AcceptMemberVisitor(memberVisitor);
            return memberVisitor.SourceMember;
        }

        IList<IFakeArgument> IInvocationExpression.GetArguments() => GetArguments();
        internal IList<IFakeArgument> GetArguments()
        {
            if (_arguments == null)
            {
                var visitor = new GetArgumentsMemberVisitor();
                ((IInvocationExpression) this).AcceptMemberVisitor(visitor);
                _arguments = visitor.Arguments;
            }

            return _arguments;
        }

        public Task MatchArgumentsAsync(Task task, ICollection<object[]> arguments, bool checkArguments, Func<byte, bool> expectedCalls)
        {
            return task.ContinueWith(t => MatchArguments(arguments, checkArguments, expectedCalls));
        }

        public Task<T> MatchArgumentsGenericAsync<T>(Task<T> task, ICollection<object[]> arguments, bool checkArguments, Func<byte, bool> expectedCalls)
        {
            return task.ContinueWith(t =>
            {
                MatchArguments(arguments, checkArguments, expectedCalls);
                return t.Result;
            });
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
                            throw new VerifyException(
                                $"Setup and actual arguments are not matched. Expected - {fakeArgument}, actual - {EqualityArgumentChecker.ToString(args[i])}.");
                        }
                    }
            }
        }
    }
}

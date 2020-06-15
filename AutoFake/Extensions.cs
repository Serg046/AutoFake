using System;
using System.Collections.Generic;
using Mono.Cecil;
using System.Linq;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal static class Extensions
    {
        public static bool EquivalentTo(this MethodReference methodReference, MethodReference method)
            => methodReference.Name == method.Name &&
               methodReference.Parameters.Select(p => p.ParameterType.FullName)
         .SequenceEqual(method.Parameters.Select(p => p.ParameterType.FullName)) &&
               methodReference.ReturnType.FullName == method.ReturnType.FullName;

        public static MethodDescriptor ToMethodDescriptor(this Delegate action)
        {
            return new MethodDescriptor(action.Method.DeclaringType?.FullName, action.Method.Name);
        }

        public static ClosureDescriptor ToClosureDescriptor(this Delegate action, IProcessorFactory processorFactory)
        {
            var members = GetCapturedMembers(action, processorFactory);
            return new ClosureDescriptor(action.Method.DeclaringType.FullName, action.Method.Name, members);
        }

        public static IEmitter GetEmitter(this MethodBody method) => new Emitter(method);

        public static bool IsAsync(this MethodDefinition method, out MethodDefinition asyncMethod)
        {
            var asyncAttribute = method.CustomAttributes
                .SingleOrDefault(a => a.AttributeType.Name == "AsyncStateMachineAttribute");
            if (asyncAttribute != null)
            {
                var generatedAsyncType = (TypeReference)asyncAttribute.ConstructorArguments[0].Value;
                asyncMethod = generatedAsyncType.Resolve().Methods.Single(m => m.Name == "MoveNext");
                return true;
            }
            asyncMethod = null;
            return false;
        }

        public static void ReplaceType(this Instruction instruction, TypeReference typeRef)
        {
            if (instruction.Operand is FieldReference field && field.FieldType.FullName == typeRef.FullName)
            {
                field.FieldType = typeRef;
            }
            else if (instruction.Operand is MethodReference method)
            {
                if (method.ReturnType.FullName == typeRef.FullName)
                {
                    method.ReturnType = typeRef;
                }
                for (var i = 0; i < method.Parameters.Count; i++)
                {
                    if (method.Parameters[i].ParameterType.FullName == typeRef.FullName)
                    {
                        method.Parameters[i].ParameterType = typeRef;
                    }
                }
            }
        }

        public static ICollection<CapturedMember> GetCapturedMembers(this Delegate action, IProcessorFactory processorFactory)
        {
            var proc = processorFactory.CreatePrePostProcessor();
            var delegateType = processorFactory.TypeInfo.Module.GetType(action.Method.DeclaringType.FullName, true) as TypeDefinition;
            var delegateRef = delegateType.Methods.Single(m => m.Name == action.Method.Name);
            if (delegateRef.IsAsync(out var asyncMethod)) delegateRef = asyncMethod;
            return delegateRef.Body.Instructions
                .Where(c => c.OpCode == OpCodes.Ldfld || c.OpCode == OpCodes.Ldflda)
                .Select(c => (FieldDefinition)c.Operand)
                .Distinct()
                .Where(field => field.DeclaringType == delegateType &&
                                !delegateRef.Body.Instructions.Any(c => c.OpCode == OpCodes.Stfld && c.Operand == field))
                .Select(field =>
                {
                    var capturedField = action.Target.GetType().GetField(field.Name);
                    var capturedValue = capturedField.GetValue(action.Target);
                    var generatedField = proc.GenerateField(
                        $"Captured_{field.Name}_{Guid.NewGuid()}", capturedValue.GetType());
                    return new CapturedMember(field, generatedField, capturedValue);
                }).ToList();
        }
    }
}

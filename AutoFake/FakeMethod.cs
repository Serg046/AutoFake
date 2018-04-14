using System.Linq;
using AutoFake.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal class FakeMethod
    {
        private readonly MethodDefinition _method;
        private readonly IMethodMocker _mocker;

        public FakeMethod(MethodDefinition method, IMethodMocker mocker)
        {
            _method = method;
            _mocker = mocker;
        }

        public void ApplyMock(IMock mock) => ApplyMock(_method, mock);

        private void ApplyMock(MethodDefinition currentMethod, IMock mock)
        {
            if (mock.IsAsyncMethod(currentMethod, out var asyncMethod))
            {
                ApplyMock(asyncMethod, mock);
            }

            foreach (var instruction in currentMethod.Body.Instructions.ToList())
            {
                if (mock.IsInstalledInstruction(_mocker.TypeInfo, instruction))
                {
                    var proc = currentMethod.Body.GetILProcessor();
                    mock.Inject(_mocker, proc, instruction);
                }
                else if (instruction.Operand is MethodReference method && IsFakeAssemblyMethod(method))
                {
                    var methodDefinition = method.Resolve();
                    if (methodDefinition.IsConstructor)
                    {
                        if (methodDefinition.DeclaringType.IsNestedPrivate)
                        {
                            var typeName = methodDefinition.DeclaringType.FullName.Replace("/", "+");
                            var type = _mocker.TypeInfo.SourceType.Assembly.GetType(typeName);
                            var importedType = _mocker.TypeInfo.Module.Import(type);
                            var proc = currentMethod.Body.GetILProcessor();
                            proc.InsertBefore(instruction, Instruction.Create(OpCodes.Ldsfld, _mocker.TypeInfo.CreateInstanceByReflectionFunc));
                            proc.InsertBefore(instruction, Instruction.Create(OpCodes.Ldtoken, importedType));
                            var getType = _mocker.TypeInfo.Module.Import(typeof(System.Type).GetMethod(nameof(System.Type.GetTypeFromHandle)));
                            proc.InsertBefore(instruction, Instruction.Create(OpCodes.Call, getType));

                            var listType = typeof(System.Collections.Generic.List<string>);
                            var ctor = listType.GetConstructor(new System.Type[0]);
                            var addToList = _mocker.TypeInfo.Module.Import(listType.GetMethod("Add"));
                            proc.InsertBefore(instruction, Instruction.Create(OpCodes.Newobj, _mocker.TypeInfo.Module.Import(ctor)));
                            foreach (var _ in methodDefinition.Parameters)
                            {
                                proc.InsertBefore(instruction, Instruction.Create(OpCodes.Call, addToList));
                            }

                            var invoke = _mocker.TypeInfo.Module.Import(typeof(System.Func<System.Type,
                                System.Collections.Generic.IEnumerable<string>, object>).GetMethod("Invoke"));
                            proc.Replace(instruction, Instruction.Create(OpCodes.Call, invoke));
                        }
                        else
                        {
                            instruction.Operand = _mocker.TypeInfo.ConvertToSourceAssembly(methodDefinition);
                        }
                    }
                    else
                    {
                        ApplyMock(methodDefinition, mock);
                    }
                }
            }
        }

        private bool IsFakeAssemblyMethod(MethodReference methodReference)
            => methodReference.DeclaringType.Scope is ModuleDefinition module && module == _mocker.TypeInfo.Module;
    }
}

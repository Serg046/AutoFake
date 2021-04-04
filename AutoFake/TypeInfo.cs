using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Setup;
using Mono.Cecil;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace AutoFake
{
    internal class TypeInfo : ITypeInfo
    {
        private const BindingFlags CONSTRUCTOR_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly AssemblyDefinition _assemblyDef;
        private readonly TypeDefinition _sourceTypeDef;
        private readonly Lazy<TypeDefinition> _fieldsTypeDef;
        private readonly Lazy<AssemblyDefinition> _fieldsAssemblyDef;
        private readonly IList<FakeDependency> _dependencies;
        private readonly FakeOptions _fakeOptions;
        private readonly Dictionary<string, ushort> _addedFields;
        private readonly Dictionary<string, IList<MethodDefinition>> _virtualMethods;
        private readonly AssemblyHost _assemblyHost;
        private readonly HashSet<AssemblyDefinition> _affectedAssemblies;

        public TypeInfo(Type sourceType, IList<FakeDependency> dependencies, FakeOptions fakeOptions)
        {
            SourceType = sourceType;
            _dependencies = dependencies;
            _fakeOptions = fakeOptions;
            _addedFields = new Dictionary<string, ushort>();
            _virtualMethods = new Dictionary<string, IList<MethodDefinition>>();
            _assemblyHost = new AssemblyHost();
            _affectedAssemblies = new HashSet<AssemblyDefinition>(new AssemblyDefinitionComparer());

            _assemblyDef = AssemblyDefinition.ReadAssembly(SourceType.Module.FullyQualifiedName,
	            new ReaderParameters {ReadSymbols = fakeOptions.Debug});
            _assemblyDef.Name.Name += "Fake";
            _sourceTypeDef = GetTypeDefinition(SourceType);

            _fieldsAssemblyDef = new Lazy<AssemblyDefinition>(() =>
            {
	            if (IsMultipleAssembliesMode)
	            {
		            var name = $"AutoFakeFields{Guid.NewGuid()}";
                    return AssemblyDefinition.CreateAssembly(
	                    new AssemblyNameDefinition(name, new Version(1, 0)), name, ModuleKind.Dll);
                }
                return _assemblyDef;
            });
            _fieldsTypeDef = new Lazy<TypeDefinition>(() =>
            {
	            var module = _fieldsAssemblyDef.Value.MainModule;
	            var typeDef = new TypeDefinition("AutoFake", "Fields", 
		            TypeAttributes.Class | TypeAttributes.Public, module.TypeSystem.Object);
	            module.Types.Add(typeDef);
	            return typeDef;
            });

        }

        public bool IsMultipleAssembliesMode
	        => _fakeOptions.AnalysisLevel == AnalysisLevels.AllAssemblies || _fakeOptions.Assemblies.Count > 0;

        public Type SourceType { get; }

        public TypeDefinition GetTypeDefinition(Type type) =>
	        _assemblyDef.MainModule.GetType(type.FullName, runtimeName: true).ToTypeDefinition();

        public TypeReference ImportReference(Type type)
	        => _assemblyDef.MainModule.ImportReference(type);

        public FieldReference ImportReference(FieldInfo field)
	        => _assemblyDef.MainModule.ImportReference(field);

        public MethodReference ImportReference(MethodBase method) 
	        => _assemblyDef.MainModule.ImportReference(method);

        public TypeReference ImportToFieldsAsm(Type type)
	        => _fieldsAssemblyDef.Value.MainModule.ImportReference(type);

        public FieldReference ImportToFieldsAsm(FieldInfo field)
	        => _fieldsAssemblyDef.Value.MainModule.ImportReference(field);

        public MethodReference ImportToFieldsAsm(MethodBase method)
	        => _fieldsAssemblyDef.Value.MainModule.ImportReference(method);

        public FieldDefinition GetField(Predicate<FieldDefinition> fieldPredicate)
	        => _sourceTypeDef.Fields.SingleOrDefault(f => fieldPredicate(f));

        public IEnumerable<MethodDefinition> GetMethods(Predicate<MethodDefinition> methodPredicate) 
	        => _sourceTypeDef.Methods.Where(m => methodPredicate(m));

        public MethodDefinition GetMethod(MethodReference methodReference) =>
            GetMethod(_sourceTypeDef, methodReference);

        public MethodDefinition GetMethod(TypeDefinition type, MethodReference methodReference)
        {
	        var method = type.Methods.SingleOrDefault(m => m.EquivalentTo(methodReference));
	        if (method == null && type.BaseType != null)
	        {
		        return GetMethod(type.BaseType.ToTypeDefinition(), methodReference);
	        }

	        return method;
        }

        public void AddField(FieldDefinition field)
        {
            if (!_addedFields.ContainsKey(field.Name))
            {
                _fieldsTypeDef.Value.Fields.Add(field);
                _addedFields.Add(field.Name, 0);
            }
            else
            {
                _addedFields[field.Name]++;
                field.Name += _addedFields[field.Name];
                _fieldsTypeDef.Value.Fields.Add(field);
            }
        }

        private object CreateInstance(Type type)
        {
            if (_dependencies.Any(d => d.Type == null))
            {
                try
                {
                    var args = _dependencies.Select(d => d.Instance).ToArray();
                    return Activator.CreateInstance(type, CONSTRUCTOR_FLAGS, null, args, null);
                }
                catch (AmbiguousMatchException)
                {
                    throw new InitializationException(
                        $"Ambiguous null-invocation. Please use {nameof(Arg)}.{nameof(Arg.IsNull)}<T>() instead of null.");
                }
            }

            var constructor = type.GetConstructor(CONSTRUCTOR_FLAGS,
                null, _dependencies.Select(d => d.Type).ToArray(), null);

            if (constructor == null)
                throw new InitializationException("Constructor is not found");

            return constructor.Invoke(_dependencies.Select(d => d.Instance).ToArray());
        }

        internal static string GetClrName(string monoCecilTypeName) => monoCecilTypeName.Replace('/', '+');

        public FakeObjectInfo CreateFakeObject(MockCollection mocks)
        {
            //TODO: Apply the fix of https://github.com/jbevain/cecil/issues/710 when released
            using var stream = _fakeOptions.Debug 
		        ? File.Create(Path.GetFullPath($"{_assemblyDef.Name.Name}-{Guid.NewGuid()}.dll")) 
		        : (Stream)new MemoryStream();
	        var fakeProcessor = new FakeProcessor(this, _fakeOptions);
	        foreach (var mock in mocks)
	        {
		        fakeProcessor.ProcessSourceMethod(mock.Mocks, mock.Method);
	        }

	        LoadAffectedAssemblies();
            _assemblyDef.Write(stream, new WriterParameters { WriteSymbols = _fakeOptions.Debug });
            var assembly = _assemblyHost.Load(stream);
            var fieldsType = _fieldsTypeDef.IsValueCreated
	            ? GetFieldsAssembly(assembly).GetType(_fieldsTypeDef.Value.FullName, true)
	            : null;

	        var sourceType = assembly.GetType(GetClrName(_sourceTypeDef.FullName), true);
	        if (SourceType.IsGenericType)
	        {
		        sourceType = sourceType.MakeGenericType(SourceType.GetGenericArguments());
	        }

	        var instance = !IsStatic(SourceType) ? CreateInstance(sourceType) : null;
	        var parameters = mocks
		        .SelectMany(m => m.Mocks)
		        .SelectMany(m => m.Initialize(fieldsType))
		        .ToList();
	        return new FakeObjectInfo(parameters, sourceType, fieldsType, instance);
        }

        private Assembly GetFieldsAssembly(Assembly executingAsm)
        {
	        if (_fieldsAssemblyDef.Value != _assemblyDef)
	        {
                using var stream = new MemoryStream();
                _fieldsAssemblyDef.Value.Write(stream);
                return LoadRenamedAssembly(stream, _fieldsAssemblyDef.Value);
	        }

	        return executingAsm;
        }

        private void LoadAffectedAssemblies()
		{
			foreach (var affectedAssembly in _affectedAssemblies)
			{
				var asmRef = _assemblyDef.MainModule.AssemblyReferences.Single(a => a.FullName == affectedAssembly.FullName);
				asmRef.Name = affectedAssembly.Name.Name = asmRef.Name + Guid.NewGuid();

				//TODO: support debug symbols
				using var stream = new MemoryStream();
				affectedAssembly.Write(stream);
				LoadRenamedAssembly(stream, affectedAssembly);
			}
        }

        private Assembly LoadRenamedAssembly(MemoryStream stream, AssemblyDefinition affectedAssembly)
        {
#if NETCOREAPP3_0
			return _assemblyHost.Load(stream);
#else
	        var assembly = _assemblyHost.Load(stream);
	        AppDomain.CurrentDomain.AssemblyResolve += (sender, args)
		        => args.Name == affectedAssembly.FullName ? assembly : null;
	        return assembly;
#endif
        }

        private bool IsStatic(Type type) => type.IsAbstract && type.IsSealed;

        public IList<MethodDefinition> GetDerivedVirtualMethods(MethodDefinition method)
        {
	        var methodContract = method.ToString();
            if (!_virtualMethods.ContainsKey(methodContract))
            {
                var list = new List<MethodDefinition>();
                foreach (var type in GetDerivedTypes(method.DeclaringType))
                {
                    var derivedMethod = type.Methods.SingleOrDefault(m => m.EquivalentTo(method));
                    if (derivedMethod != null)
                    {
                        list.Add(derivedMethod);
                    }
                }
                _virtualMethods.Add(methodContract, list);
            }

            return _virtualMethods[methodContract];
        }

        public bool TryAddAffectedAssembly(AssemblyDefinition assembly)
        {
	        return _affectedAssemblies.Add(assembly);
        }

		private IEnumerable<TypeDefinition> GetDerivedTypes(TypeDefinition parentType)
		{
            return _fakeOptions.AnalysisLevel == AnalysisLevels.Type 
	            ? Enumerable.Empty<TypeDefinition>()
	            : GetReferencedAssemblies().SelectMany(a => a.Modules).SelectMany(m => m.GetTypes()
		            .Where(t => IsDerived(t, parentType)));
		}
        
        private ICollection<AssemblyDefinition> GetReferencedAssemblies()
        {
	        var referencedAssemblies = new Dictionary<AssemblyName, AssemblyDefinition>
	        {
				{SourceType.Assembly.GetName(), _assemblyDef}
			};

	        if (_fakeOptions.AnalysisLevel == AnalysisLevels.AllAssemblies)
	        {
		        foreach (var assemblyName in SourceType.Assembly.GetReferencedAssemblies())
		        {
			        if (!referencedAssemblies.ContainsKey(assemblyName))
			        {
				        var referencedAssembly = Assembly.Load(assemblyName);
				        foreach (var module in referencedAssembly.GetModules())
				        {
							referencedAssemblies.Add(assemblyName,
								AssemblyDefinition.ReadAssembly(module.FullyQualifiedName));
						}
			        }
		        }
	        }

	        foreach (var assembly in _fakeOptions.Assemblies)
	        {
		        var assemblyName = assembly.GetName();
                if (!referencedAssemblies.ContainsKey(assemblyName))
		        {
			        foreach (var module in assembly.GetModules())
			        {
				        referencedAssemblies.Add(assemblyName,
					        AssemblyDefinition.ReadAssembly(module.FullyQualifiedName));
			        }
		        }
	        }

            return referencedAssemblies.Values;
        }

        private bool IsDerived(TypeDefinition derived, TypeDefinition parent)
		{
			if (derived.BaseType != null)
			{
				var baseType = derived.BaseType.ToTypeDefinition();
				return baseType.FullName == parent.FullName || IsDerived(baseType, parent);
			}

			return false;
		}
	}
}

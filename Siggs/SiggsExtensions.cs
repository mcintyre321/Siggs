using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Siggs
{
    public static class SiggsExtensions
    {
        private static AssemblyBuilder asmBuilder;
        private static ModuleBuilder modBuilder;
        private static ConcurrentDictionary<string, Type> typeCache = new ConcurrentDictionary<string, Type>();

        static SiggsExtensions()
        {
            asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("MethodSignatures"), AssemblyBuilderAccess.RunAndSave);

            modBuilder = asmBuilder.DefineDynamicModule("MethodSignatures", "MethodSignatures.dll");


        }

        public static Type GetTypeForMethodInfo(this MethodInfo mi)
        {
            var methodName = mi.Name;
            var classFullName = mi.DeclaringType.FullName;
            var parameterInfos =
                mi.GetParameters().Select(pi => new GenericParameterInfo { Name = pi.Name, ParameterType = pi.ParameterType, GetCustomAttributes = () => pi.GetCustomAttributesData().ToArray() });
            var key = classFullName + "." + methodName;
            return GetTypeForGenericMethodInfo(key, methodName, classFullName, parameterInfos);
        }
 

        public static Type GetTypeForGenericMethodInfo(string fullnameAndMethodName, string methodName, string classFullName, IEnumerable<GenericParameterInfo> parameterInfos)
        {
            return typeCache.GetOrAdd(fullnameAndMethodName, (k) => BuildType(methodName, classFullName, parameterInfos));
        }

        private static Type BuildType(string methodName, string classFullName, IEnumerable<GenericParameterInfo> parameterInfos)
        {
            var fullnameAndMethodName = classFullName + "." + methodName;

            // Our intermediate language generator
            ILGenerator ilgen;

            TypeBuilder typeBuilder = modBuilder.DefineType(fullnameAndMethodName, TypeAttributes.Class | TypeAttributes.Public);

            // The default constructor
            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);


            foreach (var parameterInfo in parameterInfos)
            {
                var propertyName = parameterInfo.Name;
                if (methodName.StartsWith("set_")) propertyName = methodName.Substring(4);
                FieldBuilder backingField = typeBuilder.DefineField("_" + propertyName, parameterInfo.ParameterType, FieldAttributes.Private);
                PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propertyName, System.Reflection.PropertyAttributes.HasDefault, parameterInfo.ParameterType, null);

                foreach (var customAttributeData in parameterInfo.GetCustomAttributes())
                {
                    var ctorArgs = customAttributeData.ConstructorArguments.Select(c => c.Value);
                    var namedArguments = customAttributeData.NamedArguments;
                    CustomAttributeData data = customAttributeData;
                    var namedProperties = namedArguments.Where(a => !a.IsField).Select(a => new {Property = data.AttributeType.GetProperty(a.MemberName), Value = a.TypedValue.Value});
                    var namedFields = namedArguments.Where(a => a.IsField).Select(a => new {Field = data.AttributeType.GetField(a.MemberName), Value = a.TypedValue.Value});

                    var cab = new CustomAttributeBuilder(customAttributeData.Constructor, ctorArgs.ToArray(), namedProperties.Select(p => p.Property).ToArray(),
                        namedProperties.Select(p => p.Value).ToArray(), namedFields.Select(p => p.Field).ToArray(), namedFields.Select(p => p.Value).ToArray());
                    propertyBuilder.SetCustomAttribute(cab);
                }


                // Custom attributes for get, set accessors
                MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;

                MethodBuilder getter = typeBuilder.DefineMethod("get_" + propertyName, getSetAttr, parameterInfo.ParameterType, Type.EmptyTypes);

                // Code generation
                ilgen = getter.GetILGenerator();
                ilgen.Emit(OpCodes.Ldarg_0);
                ilgen.Emit(OpCodes.Ldfld, backingField);
                ilgen.Emit(OpCodes.Ret);

                MethodBuilder setter = typeBuilder.DefineMethod("set_" + propertyName, getSetAttr, null, new Type[] {parameterInfo.ParameterType});

                // Code generation
                ilgen = setter.GetILGenerator();
                ilgen.Emit(OpCodes.Ldarg_0);
                ilgen.Emit(OpCodes.Ldarg_1);
                ilgen.Emit(OpCodes.Stfld, backingField); // setting the firstname field from the first argument (1)
                ilgen.Emit(OpCodes.Ret);


                propertyBuilder.SetGetMethod(getter);
                propertyBuilder.SetSetMethod(setter);



            }
            typeBuilder.CreateType();
            var type = asmBuilder.GetType(fullnameAndMethodName);
            return type;
        }

    }

 
    public class GenericParameterInfo
    {
        public string Name { get; set; }
        public Type ParameterType { get; set; }
        public Func<CustomAttributeData[]> GetCustomAttributes { get; set; }

    }

    internal class TypeDescriptorOverridingProvider : TypeDescriptionProvider
    {
        private readonly ICustomTypeDescriptor ctd;

        public TypeDescriptorOverridingProvider(ICustomTypeDescriptor ctd)
        {
            this.ctd = ctd;
        }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            return ctd;
        }
    }

    internal class PropertyOverridingTypeDescriptor : CustomTypeDescriptor
    {
        private readonly Dictionary<string, PropertyDescriptor> overridePds = new Dictionary<string, PropertyDescriptor>();

        public PropertyOverridingTypeDescriptor(ICustomTypeDescriptor parent) : base(parent)
        {
        }

        public void OverrideProperty(PropertyDescriptor pd)
        {
            overridePds[pd.Name] = pd;
        }

        public override object GetPropertyOwner(PropertyDescriptor pd)
        {
            object o = base.GetPropertyOwner(pd);

            if (o == null)
            {
                return this;
            }

            return o;
        }

        public PropertyDescriptorCollection GetPropertiesImpl(PropertyDescriptorCollection pdc)
        {
            List<PropertyDescriptor> pdl = new List<PropertyDescriptor>(pdc.Count + 1);

            foreach (PropertyDescriptor pd in pdc)
            {
                if (overridePds.ContainsKey(pd.Name))
                {
                    pdl.Add(overridePds[pd.Name]);
                }
                else
                {
                    pdl.Add(pd);
                }
            }

            PropertyDescriptorCollection ret = new PropertyDescriptorCollection(pdl.ToArray());

            return ret;
        }

        public override PropertyDescriptorCollection GetProperties()
        {
            return GetPropertiesImpl(base.GetProperties());
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return GetPropertiesImpl(base.GetProperties(attributes));
        }
    }
}
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
        static ConcurrentDictionary<string, Type> typeCache = new ConcurrentDictionary<string, Type>();

        static SiggsExtensions()
        {
            asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("MethodSignatures"), AssemblyBuilderAccess.RunAndSave);

            modBuilder = asmBuilder.DefineDynamicModule("MethodSignatures", "MethodSignatures.dll");
          

        }
        public static Type GetTypeForMethodInfo(this MethodInfo mi)
        {
            var name = mi.DeclaringType.FullName + "." + mi.Name;
            return typeCache.GetOrAdd(name, (s) =>
            {
                // Our intermediate language generator
                ILGenerator ilgen;

                TypeBuilder typeBuilder = modBuilder.DefineType(name, TypeAttributes.Class | TypeAttributes.Public);

                // The default constructor
                typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
                foreach (var parameterInfo in mi.GetParameters())
                {
                    var propertyName = parameterInfo.Name;
                    FieldBuilder backingField = typeBuilder.DefineField("_" + propertyName, typeof (string),
                                                                        FieldAttributes.Private);
                    PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propertyName,
                                                                          System.Reflection.PropertyAttributes.
                                                                              HasDefault, typeof (string), null);

                    // Custom attributes for get, set accessors
                    MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.HideBySig |
                                                  MethodAttributes.SpecialName;

                    // get,set accessors for FirstName
                    MethodBuilder getter = typeBuilder.DefineMethod("get_" + propertyName, getSetAttr, typeof (string),
                                                                    Type.EmptyTypes);

                    // Code generation
                    ilgen = getter.GetILGenerator();
                    ilgen.Emit(OpCodes.Ldarg_0);
                    ilgen.Emit(OpCodes.Ldfld, backingField); // returning the firstname field
                    ilgen.Emit(OpCodes.Ret);

                    MethodBuilder setter = typeBuilder.DefineMethod("set_" + propertyName, getSetAttr, null,
                                                                    new Type[] {typeof (string)});

                    // Code generation
                    ilgen = setter.GetILGenerator();
                    ilgen.Emit(OpCodes.Ldarg_0);
                    ilgen.Emit(OpCodes.Ldarg_1);
                    ilgen.Emit(OpCodes.Stfld, backingField); // setting the firstname field from the first argument (1)
                    ilgen.Emit(OpCodes.Ret);


                    propertyBuilder.SetGetMethod(getter);
                    propertyBuilder.SetSetMethod(setter);


                    //foreach (var attribute in parameterInfo.GetCustomAttributes(false))
                    //{
                    //    propertyBuilder.SetCustomAttribute(char, );
                    //}


                }
                typeBuilder.CreateType();
                var type = asmBuilder.GetType(name);
                return type;
            });
        }

        // Define other methods and classes here


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

        public PropertyOverridingTypeDescriptor(ICustomTypeDescriptor parent)
            : base(parent)
        { }

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

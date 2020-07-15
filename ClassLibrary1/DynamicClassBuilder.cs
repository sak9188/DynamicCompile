using HK.DynamicCompile.Exception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace HK.DynamicCompile
{
    public static partial class DynamicClassBuilder
    {
        public sealed class DynamicClassBuilderDeserializationBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                Type dyType = null;
                if (!DyTypeDict.TryGetValue(typeName, out dyType))
                {
                    String exeAssembly = Assembly.GetExecutingAssembly().FullName;
                    dyType = Type.GetType(String.Format("{0}, {1}", typeName, exeAssembly));
                }

                return dyType;
            }
        }

        private static IDictionary<string, Type> DyTypeDict = new Dictionary<string, Type>();

        public static void CompileDynamicTypes(IList<TableConfig> list)
        {
            foreach (TableConfig item in list)
            {
                CompileDynamicType(item);
            }
        }

        public static Type CompileDynamicType(TableConfig config)
        {
            Type dyType = null;
            if (!DyTypeDict.TryGetValue(config.name, out dyType))
            {
                dyType = CompileResultType(config);
                DyTypeDict.Add(config.name, dyType);
            }

            return dyType;
        }

        public static dynamic NewEmptyDynamic(TableConfig config)
        {
            Type dyType = CompileDynamicType(config);
            dynamic obj = Activator.CreateInstance(dyType);
            return obj;
        }

        public static List<dynamic> NewDynamics(TableConfig config, IDictionary<string, List<string>> valSet)
        {
            List<dynamic> dynamicList = new List<dynamic>();
            Type dyType = CompileDynamicType(config);

            int length = valSet.Values.First().Count;
            for (int ptr = 0; ptr < length; ptr++)
            {
                dynamic obj = Activator.CreateInstance(dyType);
                foreach (var field in dyType.GetFields())
                {
                    List<string> valList;
                    if (valSet.TryGetValue(field.Name, out valList))
                    {
                        dynamic fieldValue = config.GetFieldValue(field.Name, valList[ptr]);
                        obj.GetType().InvokeMember(field.Name, BindingFlags.SetField, null, obj, new object[] { fieldValue });
                    }
                    else
                    {
                        string log = String.Format("there is no field {0} in dynamic class of {1}", field.Name, config.name);
                        throw new NoFieldInDynamicClassException(log);
                    }
                }
                dynamicList.Add(obj);
            }
            return dynamicList;
        }

        private static Type CompileResultType(TableConfig config)
        {
            TypeBuilder tb = GetTypeBuilder(config.name);
            ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public);
            // ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            foreach (dynamic field in config)
                tb.DefineField(field.Field, field.FieldType, FieldAttributes.Public);
            // TODO 这里要对unique进行处理
            //CreateProperty(tb, field.Field, field.FieldType);

            Type objectType = tb.CreateType();
            return objectType;
        }

        private static string DyAssemblyName = "DynamicBuilder";
        private static string DyModuleName = "ConfigEntity";
        private static AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(DyAssemblyName), AssemblyBuilderAccess.Run);
        private static ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(DyModuleName);
        private static TypeBuilder GetTypeBuilder(string typeName)
        {
            TypeBuilder tb = moduleBuilder.DefineType(typeName,
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.Serializable |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout,
                    null);
            return tb;
        }
        public static void SaveDynamicEntityAssembly()
        {
            Type attributeType = typeof(AssemblyFileVersionAttribute);
            Type[] ctorParameters = { typeof(string) };
            ConstructorInfo ctor = attributeType.GetConstructor(ctorParameters);
            object[] ctorArgs = { "1.0.0" };
            CustomAttributeBuilder attribute =
               new CustomAttributeBuilder(ctor, ctorArgs);
            assemblyBuilder.SetCustomAttribute(attribute);

            attributeType = typeof(AssemblyTitleAttribute);
            ctor = attributeType.GetConstructor(ctorParameters);
            ctorArgs = new object[] { "The HK Dynamic Entity" };
            attribute = new CustomAttributeBuilder(ctor, ctorArgs);
            assemblyBuilder.SetCustomAttribute(attribute);


            attributeType = typeof(AssemblyCopyrightAttribute);
            ctor = attributeType.GetConstructor(ctorParameters);
            ctorArgs = new object[] { "© HK Personal CopyRight 1997-2020" };
            attribute = new CustomAttributeBuilder(ctor, ctorArgs);
            assemblyBuilder.SetCustomAttribute(attribute);


            attributeType = typeof(AssemblyCompanyAttribute);
            ctor = attributeType.GetConstructor(ctorParameters);
            attribute = new CustomAttributeBuilder(ctor,
               new object[] { "HK Personal" });

            attributeType = typeof(AssemblyProductAttribute);
            ctor = attributeType.GetConstructor(ctorParameters);
            attribute = new CustomAttributeBuilder(ctor,
               new object[] { "The HK Dynamic" });
            assemblyBuilder.SetCustomAttribute(attribute);

            assemblyBuilder.Save(DyModuleName + ".dll");
        }

        /// <summary>
        /// 这是一个自动生成属性的方法，但是我并不想用
        /// 仅做参考
        /// </summary>
        /// <param name="tb"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyType"></param>
        private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType)
        {

            FieldBuilder fieldBuilder = tb.DefineField(propertyName, propertyType, FieldAttributes.Public);

            PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            MethodBuilder getPropMthdBldr =
                tb.DefineMethod("get_" + propertyName,
                MethodAttributes.Public |
                MethodAttributes.SpecialName |
                MethodAttributes.HideBySig,
                propertyType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr =
                tb.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            Label modifyProperty = setIl.DefineLabel();
            Label exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);

            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }
    }
}
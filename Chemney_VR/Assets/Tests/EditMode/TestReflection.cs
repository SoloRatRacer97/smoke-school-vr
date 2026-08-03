using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace SmokeSchool.Tests
{
    internal static class TestReflection
    {
        private const BindingFlags AllMembers = BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static;

        public static Type RequireType(string fullName)
        {
            Assembly applicationAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "Assembly-CSharp");
            Type type = applicationAssembly?.GetType(fullName, false) ?? AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, false))
                .FirstOrDefault(candidate => candidate != null);
            Assert.That(type, Is.Not.Null, $"Required application type was not loaded: {fullName}");
            return type;
        }

        public static object InvokeStatic(string typeName, string methodName, params object[] arguments)
        {
            Type type = RequireType(typeName);
            MethodInfo method = type.GetMethod(methodName, AllMembers);
            Assert.That(method, Is.Not.Null, $"Required method was not found: {typeName}.{methodName}");
            return method.Invoke(null, arguments);
        }

        public static object GetStaticProperty(string typeName, string propertyName)
        {
            Type type = RequireType(typeName);
            PropertyInfo property = type.GetProperty(propertyName, AllMembers);
            Assert.That(property, Is.Not.Null, $"Required property was not found: {typeName}.{propertyName}");
            return property.GetValue(null);
        }

        public static object GetStaticField(string typeName, string fieldName)
        {
            FieldInfo field = RequireType(typeName).GetField(fieldName, AllMembers);
            Assert.That(field, Is.Not.Null, $"Required field was not found: {typeName}.{fieldName}");
            return field.GetValue(null);
        }

        public static void SetStaticField(string typeName, string fieldName, object value)
        {
            FieldInfo field = RequireType(typeName).GetField(fieldName, AllMembers);
            Assert.That(field, Is.Not.Null, $"Required field was not found: {typeName}.{fieldName}");
            field.SetValue(null, value);
        }

        public static object EnumValue(string enumTypeName, string value)
        {
            return Enum.Parse(RequireType(enumTypeName), value);
        }

        public static object GetField(object target, string fieldName)
        {
            Assert.That(target, Is.Not.Null);
            FieldInfo field = target.GetType().GetField(fieldName, AllMembers);
            Assert.That(field, Is.Not.Null, $"Required field was not found: {target.GetType().FullName}.{fieldName}");
            return field.GetValue(target);
        }

        public static object Invoke(object target, string methodName, params object[] arguments)
        {
            Assert.That(target, Is.Not.Null);
            MethodInfo method = target.GetType().GetMethod(methodName, AllMembers);
            Assert.That(method, Is.Not.Null, $"Required method was not found: {target.GetType().FullName}.{methodName}");
            return method.Invoke(target, arguments);
        }

        public static void SetField(object target, string fieldName, object value)
        {
            Assert.That(target, Is.Not.Null);
            FieldInfo field = target.GetType().GetField(fieldName, AllMembers);
            Assert.That(field, Is.Not.Null, $"Required field was not found: {target.GetType().FullName}.{fieldName}");
            field.SetValue(target, value);
        }
    }
}

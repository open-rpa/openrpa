using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.ExpressionEditor
{
    public class EditorUtil
    {
        public static ExpressionNode autoCompletionTree;
        public static void Init()
        {
            autoCompletionTree = CreateDefaultAutoCompletionTree();
        }
        /// <summary>Builds a default auto-completion navigation tree based on the currently loaded 
        /// assemblies.</summary>
        public static ExpressionNode CreateDefaultAutoCompletionTree()
        {
            ExpressionNode rootNode = new ExpressionNode();
            Assembly target = Assembly.GetExecutingAssembly();
            List<Assembly> references = (from assemblyName in target.GetReferencedAssemblies()
                                         select Assembly.Load(assemblyName)).ToList();

            List<Type> types = new List<Type>(references.SelectMany(
                (assembly) => (from childType in assembly.GetTypes()
                               where (childType.IsPublic && childType.IsVisible && childType.Namespace != null)
                               select childType).ToList()));
            foreach (Type child in types)
            {
                AddTypeToExpressionTree(rootNode, child);
            }
            rootNode.Sort();
            return rootNode;
        }
        /// <summary>Adds details about a type to the supplied expression tree.</summary>
        /// <param name="target">The root node of the expression tree.</param>
        /// <param name="child">The type to add.</param>
        public static void AddTypeToExpressionTree(ExpressionNode target, Type child)
        {
            ExpressionNode rootNode;

            if (child.IsGenericType)
            {
                rootNode = ExpressionNode.SearchForNode(target, child.Namespace, true, true);
                AddGenericTypeDetails(rootNode, child);
            }
            else if (child.IsClass)
            {
                rootNode = ExpressionNode.SearchForNode(target, child.Namespace, true, true);
                AddClassDetails(rootNode, child);
            }
            else if (child.IsEnum)
            {
                rootNode = ExpressionNode.SearchForNode(target, child.Namespace, true, true);
                AddEnumeratedTypeDetails(rootNode, child);
            }
            else if (child.IsValueType)
            {
                rootNode = ExpressionNode.SearchForNode(target, child.Namespace, true, true);
                AddValueTypeDetails(rootNode, child);
            }
        }
        public static void AddGenericTypeDetails(ExpressionNode rootNode, Type child)
        {
            ExpressionNode entityNode = new ExpressionNode
            {
                Description = "Type: " + child.Name,
                Name = child.Name,
                ItemType = "class",
                Parent = rootNode
            };

            rootNode.Add(entityNode);

            AddFieldNodes(entityNode, child);
            AddPropertyNodes(entityNode, child);
            AddMethodNodes(entityNode, child);
        }
        public static void AddClassDetails(ExpressionNode rootNode, Type child)
        {
            ExpressionNode entityNode = new ExpressionNode
            {
                Description = "Class: " + child.Name,
                Name = child.Name,
                ItemType = "class",
                Parent = rootNode
            };

            rootNode.Add(entityNode);

            AddFieldNodes(entityNode, child);
            AddPropertyNodes(entityNode, child);
            AddMethodNodes(entityNode, child);
        }
        public static void AddEnumeratedTypeDetails(ExpressionNode rootNode, Type child)
        {
            ExpressionNode enumNode = new ExpressionNode
            {
                Description = "Enum: " + child.Name,
                Name = child.Name,
                ItemType = "enum",
                Parent = rootNode
            };

            rootNode.Add(enumNode);

            string[] names = Enum.GetNames(child);
            Array values = Enum.GetValues(child);

            for (int i = 0; i < names.Length; i++)
            {
                enumNode.Add(new ExpressionNode
                {
                    Description = string.Format("Enum Value: {0} = {1} ", names[i], values.GetValue(i)),
                    Name = names[i],
                    ItemType = "enum",
                    Parent = enumNode
                });
            }
        }
        public static void AddValueTypeDetails(ExpressionNode rootNode, Type child)
        {
            // TODO: Need more validation
            try
            {
                ExpressionNode entityNode = new ExpressionNode
                {
                    Description = "Class: " + child.Name,
                    Name = child.Name,
                    ItemType = "class",
                    Parent = rootNode
                };

                rootNode.Add(entityNode);

                AddFieldNodes(entityNode, child);
                AddPropertyNodes(entityNode, child);
                AddMethodNodes(entityNode, child);

            }
            catch (Exception)
            {

                throw;
            }
        }
        public static void AddFieldNodes(ExpressionNode target, Type child)
        {
            foreach (FieldInfo field in child.GetFields(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
            {
                ExpressionNode fieldNode = new ExpressionNode
                {
                    Name = field.Name,
                    ItemType = "field",
                    Parent = target,
                    Description = GetFieldDescription(field)
                };

                target.Add(fieldNode);
            }
        }
        public static string GetFieldDescription(FieldInfo target)
        {
            StringBuilder description = new StringBuilder(128);

            if (target.IsPublic) description.Append("Public ");
            if (target.IsPrivate) description.Append("Private ");
            if (target.IsStatic) description.Append("Shared ");

            description.Append(target.Name);
            description.Append("() ");
            description.Append("As " + target.FieldType.Name);
            description.Append(GetParameters(target.FieldType));

            return description.ToString();
        }
        public static string GetParameters(Type target)
        {
            StringBuilder parameter = new StringBuilder(128);

            if (target.IsGenericType)
            {
                parameter.Append("(Of ");

                foreach (Type argument in target.GetGenericArguments())
                {
                    parameter.Append(argument.Name);
                    parameter.Append(", ");
                }

                if (parameter.Length > 4)
                {
                    parameter.Remove(parameter.Length - 2, 2);
                }

                parameter.Append(")");
            }

            return parameter.ToString();
        }
        public static string GetParameters(MethodInfo target)
        {
            StringBuilder parameter = new StringBuilder(128);

            if (target.IsGenericMethod)
            {
                parameter.Append("(Of ");

                foreach (Type argument in target.GetGenericArguments())
                {
                    parameter.Append(argument.Name);
                    parameter.Append(", ");
                }

                if (parameter.Length > 4)
                {
                    parameter.Remove(parameter.Length - 2, 2);
                }

                parameter.Append(")");
            }

            return parameter.ToString();
        }
        public static void AddMethodNodes(ExpressionNode target, Type child)
        {
            // Protect against the properties being identified as methods with a 'get_' or 'set_' 
            // prefix on their name...
            List<string> properties = new List<string>();

            foreach (PropertyInfo property in child.GetProperties(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
            {
                if (property.CanRead) properties.Add("get_" + property.Name);
                if (property.CanWrite) properties.Add("set_" + property.Name);
            }

            foreach (MethodInfo method in child.GetMethods(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
            {
                if (properties.Contains(method.Name)) continue;

                ExpressionNode methodNode = new ExpressionNode
                {
                    Name = method.Name,
                    ItemType = "method",
                    Parent = target,
                    Description = GetMethodDescription(method)
                };

                target.Add(methodNode);
            }
        }
        public static string GetMethodDescription(MethodInfo target)
        {
            StringBuilder description = new StringBuilder(128);

            if (target.IsPublic) description.Append("Public ");
            if (target.IsFamily) description.Append("Protected ");
            if (target.IsAssembly) description.Append("Friend ");
            if (target.IsPrivate) description.Append("Private ");
            if (target.IsAbstract) description.Append("MustOverride ");
            if (target.IsVirtual && !target.IsFinal) description.Append("Overridable ");
            if (target.IsStatic) description.Append("Shared ");

            if (target.ReturnType != typeof(void)) description.Append("Function ");
            else description.Append("Sub ");

            description.Append(target.Name);
            description.Append(GetParameters(target));

            description.Append("(");

            ParameterInfo[] parameters = target.GetParameters();

            foreach (ParameterInfo param in parameters)
            {
                if (param.IsOptional) description.Append("Optional ");

                if (param.IsOut) description.Append("ByRef ");
                else description.Append("ByVal ");

                description.Append(param.Name + " As " + param.ParameterType.Name);
                description.Append(GetParameters(param.ParameterType));

                if (param.DefaultValue == null) description.Append(" = Nothing");
                else description.Append(" = " + param.DefaultValue);

                description.Append(", ");
            }

            //remove trailing comma, if present.
            if (parameters.Length > 0) description.Remove(description.Length - 2, 2);

            description.Append(") ");

            if (target.ReturnType != typeof(void))
            {
                description.Append("As " + target.ReturnType.Name);
                description.Append(GetParameters(target.ReturnType));
            }

            return description.ToString();
        }
        public static void AddPropertyNodes(ExpressionNode target, Type child)
        {
            foreach (PropertyInfo property in child.GetProperties(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
            {
                ExpressionNode propertyNode = new ExpressionNode
                {
                    Name = property.Name,
                    ItemType = "property",
                    Parent = target,
                    Description = GetPropertyDescription(property)
                };

                target.Add(propertyNode);
            }
        }
        public static string GetPropertyDescription(PropertyInfo target)
        {
            StringBuilder description = new StringBuilder(128);

            if (!target.CanWrite && target.CanRead) description.Append("ReadOnly ");
            else if (target.CanWrite && !target.CanRead) description.Append("WriteOnly ");

            description.Append("Property " + target.Name + " As " + target.PropertyType.Name);
            description.Append(GetParameters(target.PropertyType));

            return description.ToString();
        }
    }
}

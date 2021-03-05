using Mono.Cecil;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

static internal class TypeDefinitionExtensions
{
   /// <summary>
   /// Is childTypeDef a subclass of parentTypeDef. Does not test interface inheritance
   /// </summary>
   /// <param name="childTypeDef"></param>
   /// <param name="parentTypeDef"></param>
   /// <returns></returns>
   public static bool IsSubclassOf(this TypeDefinition childTypeDef, TypeDefinition parentTypeDef) => 
      childTypeDef.MetadataToken 
          != parentTypeDef.MetadataToken 
          && childTypeDef
         .EnumerateBaseClasses()
         .Any(b => b.MetadataToken == parentTypeDef.MetadataToken);

   /// <summary>
   /// Does childType inherit from parentInterface
   /// </summary>
   /// <param name="childType"></param>
   /// <param name="parentInterfaceDef"></param>
   /// <returns></returns>
   public static bool DoesAnySubTypeImplementInterface(this TypeDefinition childType, TypeDefinition parentInterfaceDef)
   {
      Debug.Assert(parentInterfaceDef.IsInterface);
      return childType
     .EnumerateBaseClasses()
     .Any(typeDefinition => typeDefinition.DoesSpecificTypeImplementInterface(parentInterfaceDef));
   }

   /// <summary>
   /// Does the childType directly inherit from parentInterface. Base
   /// classes of childType are not tested
   /// </summary>
   /// <param name="childTypeDef"></param>
   /// <param name="parentInterfaceDef"></param>
   /// <returns></returns>
   public static bool DoesSpecificTypeImplementInterface(this TypeDefinition childTypeDef, TypeDefinition parentInterfaceDef)
   {
      Debug.Assert(parentInterfaceDef.IsInterface);
      return childTypeDef
     .Interfaces
     .Any(ifaceDef => DoesSpecificInterfaceImplementInterface(ifaceDef.InterfaceType.Resolve(), parentInterfaceDef));
   }

   /// <summary>
   /// Does interface iface0 equal or implement interface iface1
   /// </summary>
   /// <param name="iface0"></param>
   /// <param name="iface1"></param>
   /// <returns></returns>
   public static bool DoesSpecificInterfaceImplementInterface(TypeDefinition iface0, TypeDefinition iface1)
   {
     Debug.Assert(iface1.IsInterface);
     Debug.Assert(iface0.IsInterface);
     return iface0.MetadataToken == iface1.MetadataToken || iface0.DoesAnySubTypeImplementInterface(iface1);
   }

   /// <summary>
   /// Is source type assignable to target type
   /// </summary>
   /// <param name="target"></param>
   /// <param name="source"></param>
   /// <returns></returns>
   public static bool IsAssignableFrom(this TypeDefinition target, TypeDefinition source) 
  => target == source 
     || target.MetadataToken == source.MetadataToken 
     || source.IsSubclassOf(target)
     || target.IsInterface && source.DoesAnySubTypeImplementInterface(target);

   /// <summary>
   /// Enumerate the current type, it's parent and all the way to the top type
   /// </summary>
   /// <param name="klassType"></param>
   /// <returns></returns>
   public static IEnumerable<TypeDefinition> EnumerateBaseClasses(this TypeDefinition klassType)
   {
      for (var typeDefinition = klassType; typeDefinition != null; typeDefinition = typeDefinition.BaseType?.Resolve())
      {
         yield return typeDefinition;
      }
   }


    /// <summary>
    /// �ж�һ�������Ƿ���delegate
    /// </summary>
    /// <param name="typeDefinition">Ҫ�жϵ�����</param>
    /// <returns></returns>
    public static bool IsDelegate(this TypeDefinition typeDefinition)
    {
        if (typeDefinition.BaseType == null)
        {
            return false;
        }
        return typeDefinition.BaseType.FullName == "System.MulticastDelegate";
    }

    /// <summary>
    /// �ж�һ�������ǲ��Ƿ���
    /// </summary>
    /// <param name="type">Ҫ�жϵ�����</param>
    /// <returns></returns>
    public static bool IsGeneric(this TypeReference type)
    {
        if (type.HasGenericParameters || type.IsGenericParameter)
        {
            return true;
        }
        if (type.IsByReference)
        {
            return ((ByReferenceType)type).ElementType.IsGeneric();
        }
        if (type.IsArray)
        {
            return ((ArrayType)type).ElementType.IsGeneric();
        }
        if (type.IsGenericInstance)
        {
            foreach (var typeArg in ((GenericInstanceType)type).GenericArguments)
            {
                if (typeArg.IsGeneric())
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// �ж�һ�����͵ķ���ʵ���Ƿ������Ժ����ķ���ʵ��
    /// </summary>
    /// <param name="type">Ҫ�жϵ�����</param>
    /// <returns></returns>
    public static bool HasGenericArgumentFromMethod(this TypeReference type)
    {
        if (type.IsGenericParameter)
        {
            return (type as GenericParameter).Type == GenericParameterType.Method;
        }

        if (type.IsByReference)
        {
            return ((ByReferenceType)type).ElementType.HasGenericArgumentFromMethod();
        }
        if (type.IsArray)
        {
            return ((ArrayType)type).ElementType.HasGenericArgumentFromMethod();
        }
        if (type.IsGenericInstance)
        {
            foreach (var typeArg in ((GenericInstanceType)type).GenericArguments)
            {
                if (typeArg.HasGenericArgumentFromMethod())
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// �ж�һ�������ǲ��Ƿ���
    /// </summary>
    /// <param name="method">Ҫ�жϵķ���</param>
    /// <returns></returns>
    public static bool IsGeneric(this MethodReference method)
    {
        if (method.HasGenericParameters) return true;
        //if (method.ReturnType.IsGeneric()) return true;
        //foreach (var paramInfo in method.Parameters)
        //{
        //    if (paramInfo.ParameterType.IsGeneric()) return true;
        //}
        if (method.IsGenericInstance)
        {
            foreach (var typeArg in ((GenericInstanceMethod)method).GenericArguments)
            {
                if (typeArg.IsGeneric())
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// �ж�һ���ֶε������ǲ��Ƿ���
    /// </summary>
    /// <param name="field">Ҫ�ж��ֶ�</param>
    /// <returns></returns>
    public static bool IsGeneric(this FieldReference field)
    {
        return field.FieldType.IsGeneric();
    }

    /// <summary>
    /// �ж����������ǲ���ͬһ��
    /// </summary>
    /// <param name="left">����1</param>
    /// <param name="right">����2</param>
    /// <returns></returns>
    public static bool IsSameType(this TypeReference left, TypeReference right)
    {
        return left.FullName == right.FullName
            && left.Module.Assembly.FullName == right.Module.Assembly.FullName
            && left.Module.FullyQualifiedName == right.Module.FullyQualifiedName;
    }

    /// <summary>
    /// �ж��������͵������Ƿ���ͬ
    /// </summary>
    /// <param name="left">����1</param>
    /// <param name="right">����2</param>
    /// <returns></returns>
    public static bool IsSameName(this TypeReference left, TypeReference right)
    {
        return left.FullName == right.FullName;
    }

    /// <summary>
    /// �ж�����������������ж���������ͼ�����ֵ���͵����֣��Ƿ����
    /// </summary>
    /// <param name="left">����1</param>
    /// <param name="right">����2</param>
    /// <returns></returns>
    public static bool IsTheSame(this MethodReference left, MethodReference right)
    {
        if (left.Parameters.Count != right.Parameters.Count
                    || left.Name != right.Name
                    || !left.ReturnType.IsSameName(right.ReturnType)
                    || !left.DeclaringType.IsSameName(right.DeclaringType)
                    || left.HasThis != left.HasThis
                    || left.GenericParameters.Count != right.GenericParameters.Count)
        {
            return false;
        }

        for (int i = 0; i < left.Parameters.Count; i++)
        {
            if (left.Parameters[i].Attributes != right.Parameters[i].Attributes
                || !left.Parameters[i].ParameterType.IsSameName(right.Parameters[i].ParameterType))
            {
                return false;
            }
        }

        for (int i = 0; i < left.GenericParameters.Count; i++)
        {
            if (left.GenericParameters[i].IsSameName(right.GenericParameters[i]))
            {
                return false;
            }
        }

        return true;
    }

}
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using TLua.Analysis;

/// <summary>
/// 用于产生TLua编辑器使用的符号描述。代码是拷贝自SLua的LuaCodeGen然后修改的（都是私有方法，只能拷贝了）
/// 导出算法：
/// 1、枚举类型：枚举
/// 
/// 
/// </summary>
public class DeclarationGen
{
    static List<string> memberFilter = new List<string>
    {
        "AnimationClip.averageDuration",
        "AnimationClip.averageAngularSpeed",
        "AnimationClip.averageSpeed",
        "AnimationClip.apparentSpeed",
        "AnimationClip.isLooping",
        "AnimationClip.isAnimatorMotion",
        "AnimationClip.isHumanMotion",
        "AnimatorOverrideController.PerformOverrideClipListCleanup",
        "Caching.SetNoBackupFlag",
        "Caching.ResetNoBackupFlag",
        "Light.areaSize",
        "Security.GetChainOfTrustValue",
        "Texture2D.alphaIsTransparency",
        "WWW.movie",
        "WebCamTexture.MarkNonReadable",
        "WebCamTexture.isReadable",
        // i don't why below 2 functions missed in iOS platform
        "Graphic.OnRebuildRequested",
        "Text.OnRebuildRequested",
        // il2cpp not exixts
        "Application.ExternalEval",
        "GameObject.networkView",
        "Component.networkView",
        // unity5
        "AnimatorControllerParameter.name",
        "Input.IsJoystickPreconfigured",
        "Resources.LoadAssetAtPath",
    };

    public ChunkDeclaration RootChunk { get; set; }

    ChunkDeclaration FindOrCreateNameSpaceChunk(string namespaceName)
    {
        if (string.IsNullOrEmpty(namespaceName))
        {
            return this.RootChunk;
        }

        string[] subt = namespaceName.Split(new Char[] { '.' });

        ChunkDeclaration parent = RootChunk;

        for (int i = 0; i < subt.Length; ++i)
        {
            string subNamespaceName = subt[i];

            ChunkDeclaration findDecl = parent.GetGlobal(subNamespaceName) as ChunkDeclaration;

            if (findDecl == null)
            {
                findDecl = new ChunkDeclaration();
                findDecl.Name = subNamespaceName;
                findDecl.DisplayText = subNamespaceName;
                findDecl.Description = "namespace " + subNamespaceName;
                findDecl.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.NameSpace);

                parent.AddGlobals(findDecl);
            }
            parent = findDecl;
        }

        return parent;
    }

    LuaClassDeclaration CreateClassDeclaraion(string name, string fullName)
    {
        LuaClassDeclaration classDecl = new LuaClassDeclaration();
        classDecl.Name = name;
        classDecl.DisplayText = classDecl.Name;
        classDecl.Description = "class " + fullName;
        classDecl.FullName = fullName;
        classDecl.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.Class_Type);
        return classDecl;
    }

    LuaClassDeclaration GetorCreateClassDeclaraion(Type t, string givenNameSpace = null)
    {
        if (t == null)
            return null;

        if (!string.IsNullOrEmpty(givenNameSpace))
        {
            ChunkDeclaration declParent = null;
            string className = null;
            string[] subt = givenNameSpace.Split(new Char[] { '.' });
            if (subt.Length == 1)
            {
                className = givenNameSpace;
                declParent = this.RootChunk;
            }
            else
            {
                string realNameSpace = givenNameSpace.Substring(givenNameSpace.LastIndexOf('.'));
                declParent = FindOrCreateNameSpaceChunk(realNameSpace);
                className = subt[subt.Length - 1];
            }

            if (declParent == null || string.IsNullOrEmpty(className))
                return null;

            LuaClassDeclaration declClass = declParent.GetGlobal(className) as LuaClassDeclaration;
            if (declClass == null)
            {
                declClass = CreateClassDeclaraion(className, givenNameSpace);
                declParent.AddGlobals(declClass);
            }
            return declClass;
           
        }

        if (t.IsNested)
        {
            LuaClassDeclaration declParent = GetorCreateClassDeclaraion(t.ReflectedType);
            if (declParent == null)
            {
                return null;
            }

            LuaClassDeclaration declClass = declParent.GetMember(t.Name) as LuaClassDeclaration;
            if (declClass == null)
            {
                declClass = CreateClassDeclaraion(t.Name,t.FullName);
                declParent.AddMember(declClass);
            }

            return declClass;
        }
        else
        {
            ChunkDeclaration declParent = FindOrCreateNameSpaceChunk(t.Namespace);
            if (declParent == null)
            {
                return null;
            }

            LuaClassDeclaration declClass = declParent.GetGlobal(t.Name) as LuaClassDeclaration;
            if (declClass == null)
            {
                declClass = CreateClassDeclaraion(t.Name,t.FullName);
                declParent.AddGlobals(declClass);
            }
            return declClass;
        }
    }

    VariableDeclaration CreateVaribaleDeclation(string name, string typeFullName, bool readOnly, bool isStatic, AnalysisType at, string optDesc = null)
    {
        VariableDeclaration varDecl = new VariableDeclaration();
        varDecl.Name = name;
        varDecl.DisplayText = varDecl.Name;
        varDecl.Description = string.IsNullOrEmpty(optDesc) ? (typeFullName + " " + name) : optDesc;
        varDecl.ReadOnly = readOnly;
        varDecl.IsStatic = isStatic;
        varDecl.Type = typeFullName;
        varDecl.TypeImageIndex = AnalysisConfig.TypeImageIndex(at);
        return varDecl;
    }

    FunctionDeclaration CreateFunctionDeclation(string funcName, string parentFullName, Type retType, ParameterInfo[] pars, bool isStatic)
    {
        string returnTypeName = AnalysisConfig.T_void;
        if (retType != null && retType != typeof(void))
        {
            returnTypeName = SimpleType(retType);
        }

        string paramMsg = ParameterDecl(pars);

        FunctionDeclaration func = new FunctionDeclaration();
        func.Name = funcName;
        func.DisplayText = func.Name;
        func.Description = returnTypeName + " " + parentFullName + "." + funcName + "(" + paramMsg + ")";
        func.IsStatic = isStatic;
        func.IsVararg = false;
        if (func.IsStatic)
        {
            func.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.Class_Static_Function);
        }
        else
        {
            func.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.Class_Instance_Function);
        }

        func.AddReturnType(returnTypeName);

        func.ParentClassFullName = parentFullName;

        for (int k = 0; k < pars.Length; k++)
        {
            ParameterInfo param = pars[k];
            string paramName = param.Name;
            string paramType = SimpleType(param.ParameterType);
            string paramDesc = AnalysisConfig.Label_Parameter + paramType + " " + paramName;
            VariableDeclaration paramDecl = CreateVaribaleDeclation(paramName, paramType, false, false, AnalysisType.Function_Param, paramDesc);
            func.AddParam(paramDecl);
        }

        return func;
    }

    public bool Generate(Type t, string givenNamespace = null, bool exportDelegate = false)
    {

        if (!t.IsGenericTypeDefinition && (!IsObsolete(t) && t != typeof(YieldInstruction) && t != typeof(Coroutine))
            || (t.BaseType != null && t.BaseType == typeof(System.MulticastDelegate)))
        {
            if (t.IsEnum)
            {
                /// 枚举类型生成一个类。每个枚举值是该类的一个变量
                /// 
                LuaClassDeclaration enumDecl = GetorCreateClassDeclaraion(t, givenNamespace);
                if (enumDecl != null)
                {
                    RegEnumFunction(t, enumDecl);
                    enumDecl.Description = "enum " + enumDecl.FullName;
                }
                else
                {
                    Debug.LogError("Can Not Create Enum Declaration : " + t.FullName);
                }

            }
            else if (t.BaseType == typeof(System.MulticastDelegate))
            {
                /// Delegate 应该不需要写出。SLua并不能在Lua层构建一个Delegate对象。只需要在变量类型上说明下这个delegate的描述就可以。

                if (exportDelegate)
                {
                    string fn;
                    if (t.IsGenericType)
                    {
                        if (t.ContainsGenericParameters)
                            return false;

                        fn = string.Format("Lua{0}_{1}", _Name(GenericBaseName(t)), _Name(GenericName(t)));
                    }
                    else
                    {
                        fn = "LuaDelegate_" + _Name(t.FullName);
                    }

                    LuaClassDeclaration delegateDecl = GetorCreateClassDeclaraion(t, fn);
                    if (delegateDecl != null)
                    {
                        WriteDelegate(t, delegateDecl);
                    }
                    else
                    {
                        Debug.LogError("Can Not Create Delegate Declaration : " + t.FullName);
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {

                LuaClassDeclaration classDecl = GetorCreateClassDeclaraion(t, givenNamespace);

                if (classDecl == null)
                {
                    Debug.LogError("Can Not Create Class Declaration : " + t.FullName);
                    return false;
                }

                WriteConstructor(t, classDecl);
                WriteFunction(t, classDecl);
                WriteField(t, classDecl);

                if (t.BaseType != null && !CutBase(t.BaseType))
                {
                    if (t.BaseType.Name.Contains("UnityEvent`1"))
                    {
                        classDecl.BaseType = _Name(GenericName(t.BaseType));
                    }
                    else
                    {
                        classDecl.BaseType = FullName(t.BaseType);
                    }
                }
            }

            return true;
        }
        return false;
    }

    void WriteDelegate(Type t, LuaClassDeclaration decl)
    {
        MethodInfo mi = t.GetMethod("Invoke");

        string strDescription = "delegate ";

        if (mi.ReturnType != typeof(void))
        {
            strDescription += SimpleType(mi.ReturnType);
        }
        else
        {
            strDescription += AnalysisConfig.T_void;
        }

        strDescription += " ";

        strDescription += SimpleType(t);

        strDescription += "(";

        List<int> outindex = new List<int>();
        List<int> refindex = new List<int>();
        strDescription += ArgsList(mi, ref outindex, ref refindex);
            
        strDescription += ")";


      
    }

    string ArgsList(MethodInfo m, ref List<int> outindex, ref List<int> refindex)
    {
        string str = "";
        ParameterInfo[] pars = m.GetParameters();
        for (int n = 0; n < pars.Length; n++)
        {
            string t = SimpleType(pars[n].ParameterType);


            ParameterInfo p = pars[n];
            if (p.ParameterType.IsByRef && p.IsOut)
            {
                str += string.Format("out {0} a{1}", t, n + 1);
                outindex.Add(n);
            }
            else if (p.ParameterType.IsByRef)
            {
                str += string.Format("ref {0} a{1}", t, n + 1);
                refindex.Add(n);
            }
            else
                str += string.Format("{0} a{1}", t, n + 1);
            if (n < pars.Length - 1)
                str += ",";


        }
        return str;
    }

    void tryMake(Type t)
    {

        if (t.BaseType == typeof(System.MulticastDelegate))
        {
            DeclarationGen cg = new DeclarationGen();
            cg.Generate(t);
        }
    }

    void RegEnumFunction(Type t, LuaClassDeclaration decl)
    {
        // Write export function
        FieldInfo[] fields = t.GetFields();
        foreach (FieldInfo f in fields)
        {
            if (f.Name == "value__") 
                continue;

            /// like C#
            string desc = decl.Name + " " + decl.Name + "." + f.Name;

            VariableDeclaration varDecl = CreateVaribaleDeclation(f.Name, AnalysisConfig.T_number, true, true,AnalysisType.EnumMember, desc);
                
            decl.AddProperty(varDecl, varDecl.IsStatic);            
        }

    }

    void WriteFunction(Type t, LuaClassDeclaration decl)
    {
         BindingFlags bf = BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Instance;

        MethodInfo[] members = t.GetMethods(bf);
        foreach (MethodInfo mi in members)
        {
            if (mi.MemberType == MemberTypes.Method
                && !IsObsolete(mi)
                && !DontExport(mi)
                && isUsefullMethod(mi)
                && !MemberInFilter(t, mi)
                && !mi.ReturnType.ContainsGenericParameters
                && !mi.ContainsGenericParameters
                )
            {

                FunctionDeclaration func = CreateFunctionDeclation(mi.Name, decl.FullName, mi.ReturnType, mi.GetParameters(), mi.IsStatic);
                decl.AddFunction(func, func.IsStatic, true);
            }
            else
            {
                continue;
            }
        }
    }

    bool MemberInFilter(Type t, MemberInfo mi)
    {
        return memberFilter.Contains(t.Name + "." + mi.Name);
    }

    bool IsObsolete(MemberInfo mi)
    {
        return LuaCodeGen.IsObsolete(mi);
    }

    bool CutBase(Type t)
    {
        if (t.FullName.StartsWith("System.Object"))
            return true;
        return false;
    }

    private void WriteField(Type t, LuaClassDeclaration declClass)
    {
        // public Filed
        FieldInfo[] fields = t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        foreach (FieldInfo fi in fields)
        {
            if (DontExport(fi) || IsObsolete(fi))
                continue;

            string typeFullName = FullName(fi.FieldType);
            bool readOnly = fi.IsLiteral || fi.IsInitOnly;

            AnalysisType at = fi.IsStatic ? AnalysisType.Class_Static_Variable : AnalysisType.Class_Instace_Variable;
            
            StringBuilder desc = new StringBuilder();
            if(readOnly)
            {
                desc.Append(AnalysisConfig.Label_ReadOnly);
            }
            if(fi.IsStatic)
            {
                desc.Append(AnalysisConfig.Label_ClassStatic);
            }

            desc.Append(SimpleType(fi.FieldType));
            desc.Append( " " );
            desc.Append( declClass.FullName );
            desc.Append(".");
            desc.Append( fi.Name );

            VariableDeclaration varDecl = CreateVaribaleDeclation(fi.Name, typeFullName, readOnly, fi.IsStatic, at, desc.ToString());

            declClass.AddProperty(varDecl, varDecl.IsStatic);

            /// 试着把FIled对应的类型也导出去
            tryMake(fi.FieldType);
        }

        // public Property
        List<PropertyInfo> getter = new List<PropertyInfo>();
        List<PropertyInfo> setter = new List<PropertyInfo>();
        // Write property set/get
        PropertyInfo[] props = t.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        foreach (PropertyInfo fi in props)
        {
            if (IsObsolete(fi) || MemberInFilter(t, fi) || DontExport(fi))
                continue;

            if (fi.Name == "Item")
            {
                //for this[],导出成setItem和getItem函数
                if (!fi.GetGetMethod().IsStatic && fi.GetIndexParameters().Length == 1)
                {
                    if (fi.CanRead && !IsNotSupport(fi.PropertyType))
                        getter.Add(fi);
                    if (fi.CanWrite && fi.GetSetMethod() != null)
                        setter.Add(fi);
                }
                continue;
            }

            /// 这里和SLua有点不同。SLua在get的时候，判断类型是不可以获取的，就不会设置get，但依然可能设置set。
            /// 我们导出符号表的时候，只要不能get，就默认没有这个属性

            if (IsNotSupport(fi.PropertyType))
                continue;


            string typeFullName = FullName(fi.PropertyType);
            bool readOnly = !(fi.CanWrite && fi.GetSetMethod() != null);
            bool isStatic = false;
            if(fi.GetGetMethod()!=null)
            {
                isStatic = fi.GetGetMethod().IsStatic;
            }
            else if(fi.GetSetMethod()!=null)
            {
                isStatic = fi.GetSetMethod().IsStatic;
            }

            AnalysisType at = isStatic ? AnalysisType.Class_Static_Variable : AnalysisType.Class_Instace_Variable;

            StringBuilder desc = new StringBuilder();
            if (readOnly)
            {
                desc.Append(AnalysisConfig.Label_ReadOnly);
            }
            if (isStatic)
            {
                desc.Append(AnalysisConfig.Label_ClassStatic);
            }

            desc.Append(SimpleType(fi.PropertyType));
            desc.Append(" ");
            desc.Append(declClass.FullName);
            desc.Append(".");
            desc.Append(fi.Name);

            VariableDeclaration varDecl = CreateVaribaleDeclation(fi.Name, typeFullName, readOnly, isStatic, at, desc.ToString());

            declClass.AddProperty(varDecl, varDecl.IsStatic);

            /// 试着把Property对应的类型也导出去
            tryMake(fi.PropertyType);
        }

        //for this[]
        WriteThisFunc(t, declClass, getter, setter);
    }
    void WriteThisFunc(Type t, LuaClassDeclaration decl, List<PropertyInfo> getter, List<PropertyInfo> setter)
    {

        //Write property this[] set/get
        if (getter.Count > 0)
        {
            string funcName = "getItem";
            // 函数名是getItem
            for (int i = 0; i < getter.Count; i++)
            {
                PropertyInfo fii = getter[i];
                ParameterInfo[] pars = fii.GetIndexParameters();
                FunctionDeclaration func = CreateFunctionDeclation(funcName,decl.FullName,fii.PropertyType,pars,false);
                    
                decl.AddFunction(func, func.IsStatic, true);
            }
        }
        if (setter.Count > 0)
        {
            string funcName = "setItem";

            for (int i = 0; i < setter.Count; i++)
            {
                PropertyInfo fii = setter[i];
                if (t.BaseType != typeof(MulticastDelegate))
                {
                    ParameterInfo[] pars = fii.GetIndexParameters();
                    FunctionDeclaration func = CreateFunctionDeclation(funcName, decl.FullName, fii.PropertyType, pars, false);

                    decl.AddFunction(func, func.IsStatic, true);
                }
            }
        }
    }

    ConstructorInfo[] GetValidConstructor(Type t)
    {
        List<ConstructorInfo> ret = new List<ConstructorInfo>();
        if (t.GetConstructor(Type.EmptyTypes) == null && t.IsAbstract && t.IsSealed)
            return ret.ToArray();
        if (t.BaseType != null && t.BaseType.Name == "MonoBehaviour")
            return ret.ToArray();

        ConstructorInfo[] cons = t.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
        foreach (ConstructorInfo ci in cons)
        {
            if (!IsObsolete(ci) && !DontExport(ci))
                ret.Add(ci);
        }
        return ret.ToArray();
    }

    bool DontExport(MemberInfo mi)
    {
        return mi.GetCustomAttributes(typeof(DoNotToLuaAttribute), false).Length > 0;
    }

    private void WriteConstructor(Type t, LuaClassDeclaration decl)
    {
        ConstructorInfo[] cons = GetValidConstructor(t);
        if (cons.Length > 0)
        {
            if (cons.Length > 0)
            {
                for (int n = 0; n < cons.Length; n++)
                {
                    ConstructorInfo ci = cons[n];
                    ParameterInfo[] pars = ci.GetParameters();
                    string paramMsg = ParameterDecl(pars);

                    FunctionDeclaration func = new FunctionDeclaration();
                    func.Name = "New";
                    func.DisplayText = func.Name;
                    func.Description = decl.FullName + " " + decl.FullName + "." + "New" + "(" + paramMsg + ")";
                    func.IsStatic = true;
                    func.IsVararg = false;
                    func.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.Class_Static_Function);
                    func.AddReturnType(decl.FullName);
                    func.ParentClassFullName = decl.FullName;
                   
                    for (int k = 0; k < pars.Length; k++)
                    {
                        ParameterInfo param = pars[k];
                        string paramName = param.Name;
                        string paramType = SimpleType(param.ParameterType);
                        string paramDesc = AnalysisConfig.Label_Parameter + paramType + " " + paramName;
                        VariableDeclaration paramDecl = CreateVaribaleDeclation(paramName, paramType, false, false, AnalysisType.Function_Param, paramDesc);
                        func.AddParam(paramDecl);
                    }

                    decl.AddFunction(func, func.IsStatic, true);
                }

            }

        }
        else if (t.IsValueType) // default constructor
        {
            FunctionDeclaration func = new FunctionDeclaration();
            func.Name = "New";
            func.DisplayText = func.Name;
            func.Description = decl.FullName + "." + decl.Name + "()";
            func.IsStatic = true;
            func.IsVararg = false;
            func.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.Class_Static_Function);
            func.AddReturnType(decl.FullName);
            func.ParentClassFullName = decl.FullName;

            decl.AddFunction(func, func.IsStatic, true);
        }
    }

    bool IsNotSupport(Type t)
    {
        if (t.IsSubclassOf(typeof(Delegate)))
            return true;
        return false;
    }

    string[] prefix = new string[] { "System.Collections.Generic" };
    string RemoveRef(string s, bool removearray = true)
    {
        if (s.EndsWith("&")) s = s.Substring(0, s.Length - 1);
        if (s.EndsWith("[]") && removearray) s = s.Substring(0, s.Length - 2);
        if (s.StartsWith(prefix[0])) s = s.Substring(prefix[0].Length + 1, s.Length - prefix[0].Length - 1);

        s = s.Replace("+", ".");
        if (s.Contains("`"))
        {
            string regstr = @"`\d";
            Regex r = new Regex(regstr, RegexOptions.None);
            s = r.Replace(s, "");
            s = s.Replace("[", "<");
            s = s.Replace("]", ">");
        }
        return s;
    }

    string GenericBaseName(Type t)
    {
        string n = t.FullName;
        if (n.IndexOf('[') > 0)
        {
            n = n.Substring(0, n.IndexOf('['));
        }
        return n.Replace("+", ".");
    }
    string GenericName(Type t)
    {
        try
        {
            Type[] tt = t.GetGenericArguments();
            string ret = "";
            for (int n = 0; n < tt.Length; n++)
            {
                string dt = SimpleType(tt[n]);
                ret += dt;
                if (n < tt.Length - 1)
                    ret += "_";
            }
            return ret;
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            return "";
        }
    }

    string _Name(string n)
    {
        string ret = "";
        for (int i = 0; i < n.Length; i++)
        {
            if (char.IsLetterOrDigit(n[i]))
                ret += n[i];
            else
                ret += "_";
        }
        return ret;
    }

    string ParameterDecl(ParameterInfo[] pars)
    {
        StringBuilder ret = new StringBuilder();
        for (int n = 0; n < pars.Length; n++)
        {
            ret.Append(SimpleType(pars[n].ParameterType));
            ret.Append(" ");
            ret.Append(pars[n].Name);
            ret.Append(",");
        }

        if (pars.Length > 0)
        {
            ret.Remove(ret.Length - 1, 1);
        }
        return ret.ToString();
    }

    bool isUsefullMethod(MethodInfo method)
    {
        if (method.Name != "GetType" && method.Name != "GetHashCode" && method.Name != "Equals" &&
            method.Name != "ToString" && method.Name != "Clone" &&
            method.Name != "GetEnumerator" && method.Name != "CopyTo" &&
            method.Name != "op_Implicit" &&
            !method.Name.StartsWith("get_", StringComparison.Ordinal) &&
            !method.Name.StartsWith("set_", StringComparison.Ordinal) &&
            !method.Name.StartsWith("add_", StringComparison.Ordinal) &&
            !IsObsolete(method) && !method.IsGenericMethod &&
            !method.Name.StartsWith("op_", StringComparison.Ordinal) &&
            !method.Name.StartsWith("remove_", StringComparison.Ordinal))
        {
            return true;
        }
        return false;
    }

    string SimpleType_(Type t)
    {

        string tn = t.Name;

        switch (tn)
        {
            case "Single":
                return AnalysisConfig.T_number;
            case "String":
                return AnalysisConfig.T_string;
            case "Double":
                return AnalysisConfig.T_number;
            case "Boolean":
                return AnalysisConfig.T_bool;
            case "Int32":
                return AnalysisConfig.T_number;
            case "Object":
                return FullName(t);
            default:
                tn = TypeDecl(t);
                tn = tn.Replace("System.Collections.Generic.", "");
                tn = tn.Replace("System.Object", "object");
                return tn;
        }
    }

    string SimpleType(Type t)
    {
        string ret = SimpleType_(t);
        return ret;
    }

    string FullName(string str)
    {
        if (str == null)
        {
            throw new NullReferenceException();
        }
        return RemoveRef(str.Replace("+", "."));
    }

    string TypeDecl(Type t)
    {
        if (t.IsGenericType)
        {
            string ret = GenericBaseName(t);

            string gs = "";
            gs += "<";
            Type[] types = t.GetGenericArguments();
            for (int n = 0; n < types.Length; n++)
            {
                gs += TypeDecl(types[n]);
                if (n < types.Length - 1)
                    gs += ",";
            }
            gs += ">";

            ret = Regex.Replace(ret, @"`\d", gs);

            return ret;
        }
        if (t.IsArray)
        {
            return TypeDecl(t.GetElementType()) + "[]";
        }
        else
            return RemoveRef(t.ToString(), false);
    }

    string ExportName(Type t)
    {
        if (t.IsGenericType)
        {
            return string.Format("Lua_{0}_{1}", _Name(GenericBaseName(t)), _Name(GenericName(t)));
        }
        else
        {
            string name = RemoveRef(t.FullName, true);
            return name;
        }
    }

    string FullName(Type t)
    {
        if (t.FullName == null)
        {
            Debug.Log(t.Name);
            return t.Name;
        }
        return FullName(t.FullName);
    }
}
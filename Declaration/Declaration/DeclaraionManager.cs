using System;
using System.Collections.Generic;
using System.Text;

namespace TLua.Analysis
{
    /// 全局环境，代表的_G
    public class DeclaraionManager
    {
        public static DeclaraionManager Ins { get; set;}

        public virtual Declaration GetGlobal(string name)
        {
            return null;
        }

        /// 支持 GGG.KKKK 这种形式
        public virtual Declaration FindDeclrationByFullName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                return null;
            }

            if (fullName.StartsWith("@"))
            {
                return FindTemplateDeclration(fullName);
            }


            string[] split = fullName.Split('.');

            return FindDeclrationByFullName(split);

        }

        public virtual Declaration FindTemplateDeclration(string tmplfullName){
            return null;
        }

        public virtual Declaration FindDeclrationByFullName(string[] split)
        {

            if (split == null || split.Length == 0)
                return null;

            Declaration cursor = GetGlobal(split[0]);

            if (cursor == null)
                return null;

            for (int i = 1; i < split.Length; ++i)
            {
                cursor = GetGlobalFromParent(cursor, split[i]);
                if (cursor == null)
                {
                    break;
                }
            }

            return cursor;
        }

        public virtual Declaration GetGlobalFromParent(Declaration parent, string name)
        {
            Declaration find = null;

            ChunkDeclaration chunkDecl = parent as ChunkDeclaration;
            if(chunkDecl!=null)
            {
                find = chunkDecl.GetGlobal(name);
            }

            if (find != null)
                return find;

            LuaClassDeclaration classDecl = parent as LuaClassDeclaration;
            if(classDecl != null)
            {
                find = classDecl.GetMember(name);
            }

            return find;
        }

        public virtual Declaration FindMemberInClassAndBase(LuaClassDeclaration declClass, string name)
        {
            if (declClass == null || string.IsNullOrEmpty(name))
                return null;

            Declaration decl = declClass.GetMember(name);
            if(decl!=null)
            {
                return decl;
            }
            else
            {
                LuaClassDeclaration declBaseClass = FindDeclrationByFullName(declClass.BaseType) as LuaClassDeclaration;
                return FindMemberInClassAndBase(declBaseClass, name);
            }
        }

        public virtual ChunkDeclaration GetChunk(string chunkName)
        {
            return null;
        }

        // 判断class是否loop了
        public virtual bool IsClassLoop(LuaClassDeclaration decl, string baseName, HashSet<string> checks)
        {
            if (decl == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(baseName))
            {
                return false;
            }
            if (decl.FullName == baseName)
            {
                return true;
            }

            if (checks == null)
            {
                checks = new HashSet<string>();
                checks.Add(decl.FullName);
            }

            LuaClassDeclaration parentDecl = this.GetGlobal(baseName) as LuaClassDeclaration;

            if (parentDecl == null)
            {
                return false;
            }
            if (checks.Contains(parentDecl.FullName))
            {      
                return true;
            }

            return IsClassLoop(parentDecl, parentDecl.BaseType, checks);
        }

    }
}

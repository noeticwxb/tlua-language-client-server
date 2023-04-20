using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace TLua.Analysis
{
    public class DeclarationSerialize
    {
        /// 读取的控制标记，主要用于检查
        enum ReadFlag
        {
            InDeclarationNode,
            InBaseContent,
            InChunkContent,
            InFunctionContent,
            InVariableContent,
            InKeywordContent,
            InTypeContent,
            InLuaClassContent,
        }

        Stack<ReadFlag> m_ReadStack = new Stack<ReadFlag>();

        void PushReadFlag(ReadFlag flag)
        {
            m_ReadStack.Push(flag);
        }

        void PopReadFlag(ReadFlag flag)
        {
            if(m_ReadStack.Peek() == flag)
            {
                m_ReadStack.Pop();               
            }
            else
            {
                throw new System.IO.InvalidDataException(flag.ToString());
            }
        }

        bool CheckReadFlag(ReadFlag flag)
        {
            if (m_ReadStack.Peek() == flag)
            {
                return true;
            }
            else
            {
                throw new System.IO.InvalidDataException(flag.ToString());
            }
        }

        /// 外部负责生成和关闭 XmlReader，设置属性等
        public Declaration Read(XmlReader reader)
        {
            m_ReadStack.Clear();
            return ReadDeclarationNode(reader);
        }
        /// 外部负责生成和关闭XmlWriter，设置属性等
        public void Write(XmlWriter writer, Declaration decl, bool isStartDocument = true)
        {
            if (isStartDocument)
            {
                writer.WriteStartDocument(false);
            }

            WriteDeclarationNode(writer, decl);

            if (isStartDocument)
            {
                writer.WriteEndDocument();
            }

            writer.Flush();
        }

        string ConvertListToString(List<string> listString)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var s in listString)
            {
                builder.Append(s);
                builder.Append(".");
            }
            builder.Remove(builder.Length - 1, 1);  //  remove last "."
            return builder.ToString();
        }

        List<string> ConvertStringToList(string str)
        {
            string[] sArray = str.Split('.');
            List<string> sList = new List<string>();
            sList.AddRange(sArray);
            return sList;
        }
        
        protected void WriteDeclarationNode(XmlWriter writer, Declaration decl)
        {
            writer.WriteStartElement("Declaration");

            if (decl is ChunkDeclaration)
            {
                writer.WriteAttributeString("Type", "ChunkDeclaration");
                WriteBaseDeclarionContent(writer, decl);
                WriteChunkDeclarationContent(writer, decl as ChunkDeclaration);
            }
            else if (decl is FunctionDeclaration)
            {
                writer.WriteAttributeString("Type", "FunctionDeclaration");
                WriteBaseDeclarionContent(writer, decl);
                WriteFunctionDeclarationContent(writer, decl as FunctionDeclaration);
            }
            else if (decl is VariableDeclaration)
            {
                writer.WriteAttributeString("Type", "VariableDeclaration");
                WriteBaseDeclarionContent(writer, decl);
                WriteVariableDeclarationContent(writer, decl as VariableDeclaration);

            }
            else if (decl is KeywordDeclaration)
            {
                writer.WriteAttributeString("Type", "KeywordDeclaration");
                WriteBaseDeclarionContent(writer, decl);
                WriteKeywordDeclarationContent(writer, decl as KeywordDeclaration);
            }
            else if (decl is LuaClassDeclaration)
            {
                writer.WriteAttributeString("Type", "LuaClassDeclaration");
                WriteBaseDeclarionContent(writer, decl);
                WriteTypeDeclarationContent(writer, decl as TypeDeclaration);
                WriteLuaClassDeclarationContent(writer, decl as LuaClassDeclaration);
            }
            else if (decl is TypeDeclaration)
            {
                writer.WriteAttributeString("Type", "TypeDeclaration");
                WriteBaseDeclarionContent(writer, decl);
                WriteTypeDeclarationContent(writer, decl as TypeDeclaration);
            }
            else
            {
                throw new System.NotImplementedException(decl.GetType().ToString());
            }

            writer.WriteEndElement();
        }

        protected Declaration ReadDeclarationNode(XmlReader reader)
        {
            Declaration decl = null;
            do
            {
                
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Declaration")
                {
                    PopReadFlag(ReadFlag.InDeclarationNode);
                    return decl;
                }
                
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Declaration")
                {
                    PushReadFlag(ReadFlag.InDeclarationNode);

                    string typeName = reader.GetAttribute("Type");
                    if (typeName == "ChunkDeclaration")
                    {
                        ChunkDeclaration chunkDecl = new ChunkDeclaration();
                        ReadBaseDeclarationContent(reader, chunkDecl);
                        ReadChunkDeclarationContent(reader, chunkDecl);
                        decl = chunkDecl;
                    }
                    else if (typeName == "FunctionDeclaration")
                    {
                        FunctionDeclaration funcDecl = new FunctionDeclaration();
                        ReadBaseDeclarationContent(reader, funcDecl);
                        ReadFunctionDeclarationContent(reader, funcDecl);
                        decl = funcDecl;
                    }
                    else if (typeName == "VariableDeclaration")
                    {
                        VariableDeclaration varDecl = new VariableDeclaration();
                        ReadBaseDeclarationContent(reader, varDecl);
                        ReadVariableDeclarationContent(reader, varDecl);
                        decl = varDecl;
                    }
                    else if (typeName == "KeywordDeclaration")
                    {
                        KeywordDeclaration keywordDecl = new KeywordDeclaration();
                        ReadBaseDeclarationContent(reader, keywordDecl);
                        ReadKeywordDeclarationContent(reader, keywordDecl);
                        decl = keywordDecl;
                    }
                    else if (typeName == "LuaClassDeclaration")
                    {
                        LuaClassDeclaration classDecl = new LuaClassDeclaration();
                        ReadBaseDeclarationContent(reader, classDecl);
                        ReadTypeDeclarationContent(reader, classDecl);
                        ReadLuaClassDeclarationContent(reader, classDecl);
                        decl = classDecl;
                    }
                    else if (typeName == "TypeDeclaration")
                    {
                        TypeDeclaration typeDecl = new TypeDeclaration();
                        ReadBaseDeclarationContent(reader, typeDecl);
                        ReadTypeDeclarationContent(reader, typeDecl);
                        decl = typeDecl;
                    }
                    else
                    {
                        throw new System.NotImplementedException("Unknown Type: " + typeName);
                    }
                }
            }
            while (reader.Read());

            return null;
        }

        protected void WriteBaseDeclarionContent(XmlWriter writer, Declaration decl)
        {
            writer.WriteStartElement("BaseContent");

            {
                writer.WriteElementString("Name", decl.Name);
                writer.WriteElementString("TypeImageIndex", XmlConvert.ToString(decl.TypeImageIndex));

                if (!string.IsNullOrEmpty(decl.FileName))
                {
                    writer.WriteElementString("FileName", decl.FileName);
                    writer.WriteElementString("Line", XmlConvert.ToString(decl.Line));
                    writer.WriteElementString("Col", XmlConvert.ToString(decl.Col));
                }

                if (!string.IsNullOrEmpty(decl.Description))
                {
                    writer.WriteElementString("Description", decl.Description);
                }

                if (!string.IsNullOrEmpty(decl.DisplayText))
                {
                    writer.WriteElementString("DisplayText", decl.DisplayText);
                }

                if (!string.IsNullOrEmpty(decl.CommentText))
                {
                    writer.WriteElementString("CommentText", decl.CommentText);
                }
            }

            writer.WriteEndElement();
        }

        protected void ReadBaseDeclarationContent(XmlReader reader, Declaration decl)
        {
            do
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "BaseContent")
                {
                    PopReadFlag(ReadFlag.InBaseContent);
                    return;
                }

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "BaseContent")
                    {
                        if (reader.IsEmptyElement)
                        {
                            return;
                        }
                        else
                        {
                            PushReadFlag(ReadFlag.InBaseContent);
                        }                        
                    }

                    if (reader.Name == "Name" && CheckReadFlag(ReadFlag.InBaseContent) )
                    {
                        decl.Name = reader.ReadString();
                    }

                    if (reader.Name == "TypeImageIndex" && CheckReadFlag(ReadFlag.InBaseContent) )
                    {
                        decl.TypeImageIndex = XmlConvert.ToInt32( reader.ReadString() );
                    }

                    if (reader.Name == "FileName" && CheckReadFlag(ReadFlag.InBaseContent))
                    {
                        decl.FileName = reader.ReadString();
                    }

                    if (reader.Name == "Line" && CheckReadFlag(ReadFlag.InBaseContent) )
                    {
                        decl.Line = XmlConvert.ToInt32(reader.ReadString());
                    }

                    if (reader.Name == "Col" && CheckReadFlag(ReadFlag.InBaseContent) )
                    {
                        decl.Col = XmlConvert.ToInt32(reader.ReadString());
                    }

                    if (reader.Name == "Description" && CheckReadFlag(ReadFlag.InBaseContent) )
                    {
                        decl.Description = reader.ReadString();
                    }

                    if (reader.Name == "DisplayText" && CheckReadFlag(ReadFlag.InBaseContent) )
                    {
                        decl.DisplayText = reader.ReadString();
                    }

                    if (reader.Name == "CommentText" && CheckReadFlag(ReadFlag.InBaseContent))
                    {
                        decl.CommentText = reader.ReadString();
                    }
                }
            }
            while (reader.Read()) ;
        }

        protected void WriteChunkDeclarationContent(XmlWriter writer, ChunkDeclaration decl)
        {
            writer.WriteStartElement("ChunkContent");

            /// UsingNameSpace
            foreach (var item in decl.UsingNameSpace)
            {
                writer.WriteElementString("Using", ConvertListToString(item));
            }

            foreach (var item in decl.Alias)
            {
                writer.WriteStartElement("Alias");
                writer.WriteAttributeString("Name", item.Key);
                writer.WriteAttributeString("Type", ConvertListToString(item.Value));
                writer.WriteEndElement();
            }

            /// Globals: 不考虑using写入global的情况。 只有全局chunk才会进行这个处理。全局chunk是不存储的
            if (decl.Globals != null && decl.Globals.Count != 0)
            {
                writer.WriteStartElement("Globals");
                foreach (var item in decl.Globals)
                {
                    WriteDeclarationNode(writer, item);
                }
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        protected void ReadChunkDeclarationContent(XmlReader reader, ChunkDeclaration decl)
        {
            do
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "ChunkContent")
                {
                    PopReadFlag(ReadFlag.InChunkContent);
                    return;
                }

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "ChunkContent")
                    {
                        if (reader.IsEmptyElement)
                        {
                            return;
                        }
                        else
                        {
                            PushReadFlag(ReadFlag.InChunkContent);
                        }

                    }

                    if (reader.Name == "Using" && CheckReadFlag(ReadFlag.InChunkContent) )
                    {
                        var l = ConvertStringToList(reader.ReadString());
                        decl.AddUsingNameSpace(l);
                    }

                    if (reader.Name == "Alias" && CheckReadFlag(ReadFlag.InChunkContent))
                    {
                        string aliasName = reader.GetAttribute("Name");
                        List<string> aliasType = ConvertStringToList(reader.GetAttribute("Type"));
                        decl.AddAlias(aliasName, aliasType);
                    }

                    if (reader.Name == "Globals" && CheckReadFlag(ReadFlag.InChunkContent))
                    {
                        do
                        {
                            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Globals")
                            {
                                break;
                            }

                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "Declaration")
                            {
                                Declaration childDecl = ReadDeclarationNode(reader);
                                if (childDecl != null)
                                {
                                    decl.AddGlobals(childDecl);
                                }
                            }

                        }
                        while (reader.Read()) ;
                    }
                }
            }
            while (reader.Read()) ;
        }

        protected void WriteFunctionDeclarationContent(XmlWriter writer, FunctionDeclaration decl)
        {
            writer.WriteStartElement("FunctionContent");

            /// isStatic
            writer.WriteElementString("IsStatic", XmlConvert.ToString(decl.IsStatic));

            /// ParentClassFullName not need. 
            if (!string.IsNullOrEmpty(decl.ParentClassFullName))
            {
                writer.WriteElementString("ParentClassFullName", decl.ParentClassFullName);
            }

            /// IsVararg
            writer.WriteElementString("IsVararg", XmlConvert.ToString(decl.IsVararg));

            /// return type
            foreach (var item in decl.ReturnTypeList)
            {
                writer.WriteElementString("ReturnType", item);
            }

            /// param
            if (decl.ParamList != null && decl.ParamList.Count != 0)
            {
                writer.WriteStartElement("ParamList");

                foreach (var item in decl.ParamList)
                {
                    WriteDeclarationNode(writer, item);
                }

                writer.WriteEndElement();
            }

            if (decl.OverloadList != null && decl.OverloadList.Count != 0)
            {
                writer.WriteStartElement("OverloadList");

                foreach (var item in decl.OverloadList)
                {
                    WriteDeclarationNode(writer, item);
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        protected void ReadFunctionDeclarationContent(XmlReader reader, FunctionDeclaration decl)
        {
            do
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "FunctionContent")
                {
                    PopReadFlag(ReadFlag.InFunctionContent);
                    return;
                }

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "FunctionContent")
                    {
                        if (reader.IsEmptyElement)
                        {
                            return;
                        }
                        else
                        {
                            PushReadFlag(ReadFlag.InFunctionContent);
                        }
                    }

                    if (reader.Name == "IsStatic" && CheckReadFlag(ReadFlag.InFunctionContent) )
                    {
                        decl.IsStatic = XmlConvert.ToBoolean(reader.ReadString());
                    }

                    if (reader.Name == "ParentClassFullName")
                    {
                        decl.ParentClassFullName = reader.ReadString();
                    }

                    if (reader.Name == "IsVararg" && CheckReadFlag(ReadFlag.InFunctionContent))
                    {
                        decl.IsVararg = XmlConvert.ToBoolean(reader.ReadString());
                    }

                    if (reader.Name == "ReturnType" && CheckReadFlag(ReadFlag.InFunctionContent))
                    {
                        string returnType = reader.ReadString();
                        decl.AddReturnType(returnType);
                    }

                    if (reader.Name == "ParamList" && CheckReadFlag(ReadFlag.InFunctionContent))
                    {
                        do
                        {
                            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "ParamList" && CheckReadFlag(ReadFlag.InFunctionContent))
                            {
                                break;
                            }

                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "Declaration")
                            {
                                Declaration paramDecl = ReadDeclarationNode(reader);
                                decl.AddParam(paramDecl as VariableDeclaration);
                            }
                        }
                        while (reader.Read());
                    }

                    if (reader.Name == "OverloadList" && CheckReadFlag(ReadFlag.InFunctionContent))
                    {
                        do
                        {
                            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "OverloadList" && CheckReadFlag(ReadFlag.InFunctionContent))
                            {
                                break;
                            }

                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "Declaration")
                            {
                                Declaration funcDecl = ReadDeclarationNode(reader);
                                decl.AddOverloadFunction(funcDecl as FunctionDeclaration);
                            }

                        }
                        while (reader.Read());
                    }
                   
                }
            }
            while (reader.Read());
        }


        protected void WriteVariableDeclarationContent(XmlWriter writer, VariableDeclaration decl)
        {
            writer.WriteStartElement("VariableContent");

            writer.WriteElementString("Type", decl.Type);
            if (!string.IsNullOrEmpty(decl.ParentClassFullName))
            {
                writer.WriteElementString("ParentClassFullName", decl.ParentClassFullName);
            }
            writer.WriteElementString("ReadOnly", XmlConvert.ToString(decl.ReadOnly));
            writer.WriteElementString("IsStatic", XmlConvert.ToString(decl.IsStatic));

            writer.WriteEndElement();
        }

        protected void ReadVariableDeclarationContent(XmlReader reader, VariableDeclaration decl)
        {
            do
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "VariableContent")
                {
                    PopReadFlag(ReadFlag.InVariableContent);
                    return;
                }

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "VariableContent")
                    {
                        if (reader.IsEmptyElement)
                        {
                            return;
                        }
                        else
                        {
                            PushReadFlag(ReadFlag.InVariableContent);
                        }
                    }

                    if (reader.Name == "ParentClassFullName" && CheckReadFlag(ReadFlag.InVariableContent))
                    {
                        decl.ParentClassFullName = reader.ReadString();
                    }

                    if (reader.Name == "IsStatic" && CheckReadFlag(ReadFlag.InVariableContent) )
                    {
                        decl.IsStatic = XmlConvert.ToBoolean(reader.ReadString());
                    }

                    if (reader.Name == "Type" && CheckReadFlag(ReadFlag.InVariableContent))
                    {
                        decl.Type = reader.ReadString();
                    }

                    if (reader.Name == "ReadOnly" && CheckReadFlag(ReadFlag.InVariableContent))
                    {
                        decl.ReadOnly = XmlConvert.ToBoolean(reader.ReadString());
                    }
                }
            }
            while (reader.Read());
        }

        protected void WriteKeywordDeclarationContent(XmlWriter writer, KeywordDeclaration decl)
        {
            writer.WriteStartElement("KeywordContent");

            writer.WriteEndElement();
        }


        protected void ReadKeywordDeclarationContent(XmlReader reader, KeywordDeclaration decl)
        {
            do
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "KeywordContent")
                {
                    PopReadFlag(ReadFlag.InKeywordContent);
                    return;
                }

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "KeywordContent")
                    {
                        if (reader.IsEmptyElement)
                        {
                            return;
                        }
                        else
                        {
                            PushReadFlag(ReadFlag.InKeywordContent);
                        }
                    }
                }
            }
            while (reader.Read());
        }

        protected void WriteTypeDeclarationContent(XmlWriter writer, TypeDeclaration decl)
        {
            writer.WriteStartElement("TypeContent");

            writer.WriteEndElement();
        }

        protected void ReadTypeDeclarationContent(XmlReader reader, TypeDeclaration decl)
        {
            do
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "TypeContent")
                {
                    PopReadFlag(ReadFlag.InTypeContent);
                    return;
                }

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "TypeContent")
                    {
                        if (reader.IsEmptyElement)
                        {
                            return;
                        }
                        else
                        {
                            PushReadFlag(ReadFlag.InTypeContent);
                        }
                    }
                }
            }
            while (reader.Read());
        }

        protected void WriteLuaClassDeclarationContent(XmlWriter writer, LuaClassDeclaration decl)
        {
            writer.WriteStartElement("LuaClassContent");

            if (!string.IsNullOrEmpty(decl.BaseType))
            {
                writer.WriteElementString("BaseType", decl.BaseType);
            }

            writer.WriteElementString("FullName", decl.FullName);

            var members = decl.Members;
            if (members != null && members.Count != 0)
            {
                writer.WriteStartElement("Members");

                foreach (var item in members)
                {
                    WriteDeclarationNode(writer, item);
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        protected void ReadLuaClassDeclarationContent(XmlReader reader, LuaClassDeclaration decl)
        {
            do
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "LuaClassContent")
                {
                    PopReadFlag(ReadFlag.InLuaClassContent);
                    return;
                }

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "LuaClassContent")
                    {
                        if (reader.IsEmptyElement)
                        {
                            return;
                        }
                        else
                        {
                            PushReadFlag(ReadFlag.InLuaClassContent);
                        }
                    }

                    if(reader.Name == "BaseType" && CheckReadFlag(ReadFlag.InLuaClassContent) )
                    {
                        decl.BaseType = reader.ReadString();
                    }

                    if (reader.Name == "FullName" && CheckReadFlag(ReadFlag.InLuaClassContent))
                    {
                        decl.FullName = reader.ReadString();
                    }

                    /// should after with FullName
                    if (reader.Name == "Members" && CheckReadFlag(ReadFlag.InLuaClassContent))
                    {
                        do
                        {
                            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Members" && CheckReadFlag(ReadFlag.InLuaClassContent))
                            {
                                break;
                            }

                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "Declaration")
                            {
                                Declaration memberDecl = ReadDeclarationNode(reader);
                                decl.AddMember(memberDecl);
                            }
                        }
                        while (reader.Read());
                    }
                }
            }
            while (reader.Read());
        }


    }
}

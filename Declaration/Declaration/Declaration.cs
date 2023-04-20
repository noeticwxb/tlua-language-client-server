using System;
using System.Collections.Generic;
using System.Text;

namespace TLua.Analysis
{
    /// <summary>
    /// Declaration放在SharpLua库中
    /// </summary>
    public abstract class Declaration
    {
        string _name = string.Empty;
        string _lowerName = string.Empty;

        /// 所在代码文件名
        public virtual string FileName { get; set; }

        /// 行数
        public virtual int Line { get; set; }

        /// 列数
        public virtual int Col { get; set; }

        /// <summary>
        /// 标识符名字
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                _lowerName = _name.ToLower();
            }
        }

        public string LowerName
        {
            get
            {
                return _lowerName;
            }
        }

        /// <summary>
        /// 表达标识符的图标索引
        /// </summary>
        public virtual int TypeImageIndex { get; set; }

        /// <summary>
        /// 标识符的提示/说明文字
        /// </summary>
        public virtual string Description { get; set; }

        /// <summary>
        /// 标识符显示出来的内容。
        /// </summary>
        public string DisplayText { get; set; }

        public virtual string CommentText { get; set; }


        public Declaration DeepClone()
        {
            Declaration decl = this.MemberwiseClone() as Declaration;
            decl.DeepCopy(this);
            return decl;
        }

        protected virtual void DeepCopy(Declaration src)
        {
            return;
        }

        public Declaration() { }

        public Declaration(string name, string displayText, int imageIndex, string desc)
        {
            Name = name;
            DisplayText = displayText;
            TypeImageIndex = imageIndex;
            Description = desc;            
        }


        public Declaration(string name, int imageIndex, string desc)
            : this(name, name, imageIndex, desc)
        {

        }

        public Declaration(string name, int imageIndex)
            : this(name, name, imageIndex, name)
        {

        }

        public Declaration(string name)
            : this(name, name, 0, name)
        {

        }

        public virtual string GetDescriptionWithComment()
        {
            if (string.IsNullOrEmpty(this.CommentText))
            {
                return this.Description;
            }
            else
            {
                return this.Description + "\n" + this.CommentText;
            }
        }

        public string ReplaceTemplateType(string tempT, List<string> realType){
            if(string.IsNullOrEmpty(tempT)){
                return tempT;
            }

            if(!tempT.StartsWith("T_")){
                return tempT;
            }

            int count = realType.Count;
            if (count == 0)
            {
                return tempT;
            }

            switch (tempT)
            {
                case "T_1":
                    return realType[0];
                case "T_2":
                    if (count >= 1)
                    {
                        return realType[1];
                    }
                    break;
                case "T_3":
                    if (count >= 2)
                    {
                        return realType[2];
                    }
                    break;
                case "T_4":
                    if (count >= 3)
                    {
                        return realType[3];
                    }
                    break;
                case "T_5":
                    if (count >= 4)
                    {
                        return realType[4];
                    }
                    break;
                default:
                    return tempT;
            }


            return tempT;
        }

        public virtual void ReplaceTemplateTypes(string parentName, List<string> realTypes)
        {
            return;
        }

        public virtual void Accept(DeclarationVisitor nv)
        {
            nv.Apply(this);
        }

        public virtual void Traverse(DeclarationVisitor nv)
        {

        }

    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Object = System.Object;

namespace TLuaServer
{
    // 用来管理一个文档，局部更新，分析行列啥的。
    // 第一版先做全文更新，以后优化
    class StringBuffer 
    {
        protected string mText;

        public StringBuffer() { 
        
        }

        public StringBuffer(string text)
        {
            mText = text;
        }


        public string Text {
            get
            {
                return mText;
            }
        }

        public override string ToString()
        {
            return this.Text;
        }

        public void UpdateText(string text)
        {
            mText = text; 
        }
        public string GetText(int lineNumber, int colNumber)
        {
            return GetText(this.Text, lineNumber, colNumber)
        }

        public static string GetText(string text, int lineNumber, int colNumber)
        {
            var reader = new System.IO.StringReader(text);

            System.Text.StringBuilder newText = new StringBuilder();
            int currentLineNumber = 0;

            do
            {
                currentLineNumber += 1;
                newText.AppendLine(reader.ReadLine());
            }
            while (currentLineNumber < lineNumber);

            // read colNumber in last line
            string lastLine = reader.ReadLine();
            lastLine = lastLine.Substring(0, colNumber);
            newText.Append(lastLine);

            return newText.ToString();
        }
    }



}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPhoneApp
{
    public class LineReader : BinaryReader
    {
        public LineReader(Stream stream) : base(stream) { }
        public LineReader(Stream stream, Encoding encoding) : base(stream, encoding) { }
        public LineReader(Stream stream, Encoding encoding, Boolean lvOpen) :
            base(stream, encoding, lvOpen) { }

        private bool hasHoldoverChar = false;
        private char holdoverChar;
        
        public String ReadLine()
        {
            var result = new StringBuilder();
            bool foundEndOfLine = false;
            char ch;
            while (!foundEndOfLine)
            {
                if (hasHoldoverChar)
                {
                    ch = holdoverChar;
                    hasHoldoverChar = false;
                }
                else
                {
                    try
                    {
                        ch = ReadChar();
                    }
                    catch (EndOfStreamException)
                    {
                        if (result.Length == 0) return null;
                        else break;
                    }
                }

                switch (ch)
                {
                    case '\r':
                        try
                        {
                            int pch = ReadChar();
                            if (pch != '\n')
                            {
                                holdoverChar = (char)pch;
                                hasHoldoverChar = true;
                            }
                        }
                        catch (EndOfStreamException)
                        {
                        }
                        foundEndOfLine = true;
                        break;
                    case '\n':
                        foundEndOfLine = true;
                        break;
                    default:
                        result.Append(ch);
                        break;
                }
            }
            return result.ToString();
        }

    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager
{
    public static class StreamHelper
    {
        static Encoding utf8enc;
        static byte[] buffer;

        static StreamHelper()
        {
            // Static initialization goes here.
            utf8enc = Encoding.UTF8;
            //utf8enc.DecoderFallback = DecoderFallback.ExceptionFallback;
            buffer = new byte[4];
        }

        public static string ReadLine(Stream s)
        {
            StringBuilder sb = new StringBuilder();
            bool done = false;
            string lastChar = "";
            int bomCounter = 0;
            while (!done)
            {
                for (int i = 0; i < 4; i++)
                {
                    buffer[i] = (byte)s.ReadByte();
                    try
                    {
                        // Handle / ignore UTF-8 Byte Order Mark.
                        if (sb.Length == 0 && bomCounter == 0 && buffer[i] == 239) { bomCounter++; break; }
                        if (sb.Length == 0 && bomCounter == 1 && buffer[i] == 187) { bomCounter++; break; }
                        if (sb.Length == 0 && bomCounter == 2 && buffer[i] == 191) { bomCounter++; break; }

                        string thisChar = utf8enc.GetString(buffer, 0, i + 1);
                        sb.Append(thisChar);
                        lastChar = thisChar;
                        break;
                    }
                    catch (DecoderFallbackException)
                    {
                        // If we get an exception, this may be a multi-byte character, so get another byte.
                    }
                }
                if (String.Equals(lastChar, "\n"))
                {
                    done = true;
                }
            }

            // Remove line terminators.
            string lineText = sb.ToString();
            if (lineText.EndsWith("\r\n")) lineText = lineText.Substring(0, lineText.Length - 2);
            else if (lineText.EndsWith("\n")) lineText = lineText.Substring(0, lineText.Length - 1);

            // Return the line string.
            return lineText;
        }

        public static void Write(Stream s, string str)
        {
            byte[] bytes = utf8enc.GetBytes(str);
            s.Write(bytes, 0, bytes.Length);
            s.Flush();
        }
    }
}

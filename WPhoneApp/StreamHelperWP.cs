using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace WPhoneApp
{
    class StreamHelperWP
    {
        static Encoding utf8enc;
        static byte[] buffer;

        static StreamHelperWP()
        {
            // Static initialization goes here.
            utf8enc = Encoding.UTF8;
            //utf8enc.DecoderFallback = DecoderFallback.ExceptionFallback;
            buffer = new byte[4];
        }

        public static async Task<string> ReadLine(IInputStream inputStream)
        {
            StringBuilder sb = new StringBuilder();
            bool done = false;
            string lastChar = "";
            int bomCounter = 0;
            IBuffer ibuffer = new Windows.Storage.Streams.Buffer(1);
            byte[] tempByteBuf = new byte[1];
            while (!done)
            {
                for (int i = 0; i < 4; i++)
                {
                    IBuffer outBuffer = await inputStream.ReadAsync(ibuffer, 1, InputStreamOptions.Partial);
                    CryptographicBuffer.CopyToByteArray(outBuffer, out tempByteBuf);
                    buffer[i] = tempByteBuf[0];
                    try
                    {
                        // Handle / ignore UTF-8 Byte Order Mark.
                        if (sb.Length == 0 && bomCounter == 0 && buffer[i] == 239) { bomCounter++; break; }
                        if (sb.Length == 0 && bomCounter == 1 && buffer[i] == 187) { bomCounter++; break; }
                        if (sb.Length == 0 && bomCounter == 2 && buffer[i] == 191) { bomCounter++; break; }

                        string thisChar = utf8enc.GetString(buffer, 0, i + 1);

                        /*
                        byte[] candidateByteArray = new byte[i + 1];
                        Array.Copy(buffer, 0, candidateByteArray, 0, i + 1);
                        string thisChar =
                            CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8,
                            CryptographicBuffer.CreateFromByteArray(candidateByteArray));
                        */

                        sb.Append(thisChar);
                        lastChar = thisChar;
                        break;
                    }
                    catch (Exception)
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

        public static async Task<bool> Write(IOutputStream outputStream, string str)
        {
            byte[] bytes = utf8enc.GetBytes(str);
            await outputStream.WriteAsync(CryptographicBuffer.CreateFromByteArray(bytes));
            await outputStream.FlushAsync();
            return true;
        }
    }
}

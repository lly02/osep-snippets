using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Encode
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: Encode.exe <encode type> <key> <url> <-v>");
                Console.WriteLine("-v: output to console");
                return;
            }
            if (args[1].Length > 1)
            {
                throw new Exception("Key must be char.");
            }

            string type = args[0];
            char key = char.Parse(args[1]);
            string url = args[2];
            bool verbose = args.Contains("-v");
            bool output = args.Contains("-o");

            byte[] buf = new System.Net.WebClient().DownloadData(url);
            byte[] encoded = null;

            switch(type)
            {
                case "xor":
                    encoded = xor(buf, key);
                    break;

                case "caesar":
                    encoded = caesar(buf, key);
                    break;

                default:
                    Console.WriteLine("Type not found.");
                    return;
            }

            if (verbose)
            {
                StringBuilder hex = new StringBuilder(encoded.Length * 2);
                foreach (byte b in encoded)
                {
                    hex.AppendFormat("0x{0:x2}, ", b);
                }

                Console.WriteLine("The payload is: " + hex.ToString());
            } 
            else
            {
                string file = $"{Directory.GetCurrentDirectory()}\\{Path.GetRandomFileName()}";
                File.WriteAllBytes(file, encoded);
                Console.WriteLine($"Encoded payload written to {file}");
            }
        }

        private static byte[] xor(byte[] buf, char key)
        {
            byte[] encoded = new byte[buf.Length];
            for (int i = 0; i < buf.Length; i++)
            {
                encoded[i] = (byte)(((uint)buf[i] ^ (byte)key));
            }

            return encoded;
        }

        private static byte[] caesar(byte[] buf, char key)
        {
            byte[] encoded = new byte[buf.Length];
            for (int i = 0; i < buf.Length; i++)
            {
                encoded[i] = (byte)(((uint)buf[i] + (byte)key) & 0xFF);
            }

            return encoded;
        }
    }
}

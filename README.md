Qopy
====

A Powershell File Copy Module with CRC Check.

XCopy, Robocopy and Teracopy are all good Windows utilities for copying a large amount of files, but none of them offer easily machine-readable results report. An efficient, reliable and audit-able _copy utility_ is critical for large-scale, automated software deployments.

Qopy is a binary Powershell Module that provides some the functionality of all of those other utilities with an object output for advanced reporting and auditing.

Two Cmdlets are exported.

	NAME
		Copy-Files

	SYNOPSIS
		A Powershell File Copy Module with CRC Check.

	SYNTAX
		Copy-Files [-Source] <string> [-Destination] <string> [[-Filter] <string>]
		[[-Recurse]] [[-Overwrite]] [[-ShowProgress]]  [<CommonParameters>]


	NAME
		Get-CopyResultsReport

	SYNOPSIS
		Aggregate output of Copy-Files

	SYNTAX
		Get-CopyResultsReport -InputObject <FileCopyResultsItem>
		[<CommonParameters>]		

The output is an object list with the following properties&#151;One object per source file.

	   TypeName: Qopy.FileCopyResultsItem

	Name           MemberType Definition
	----           ---------- ----------
	Destination    Property   string Destination {get;set;}
	DestinationCRC Property   string DestinationCRC {get;set;}
	Match          Property   bool Match {get;set;}
	Size           Property   long Size {get;set;}
	Source         Property   string Source {get;set;}
	SourceCRC      Property   string SourceCRC {get;set;}
	Time           Property   timespan Time {get;set;}

Get-CopyResultsReport returns the following properties.

    TypeName: Qopy.FileCopyResultsReport

	Name           MemberType Definition
	----           ---------- ----------
	Bytes          Property   long Bytes {get;set;}
	FailedItemList Property   System.Collections.Generic.List[Qopy.FileCopyResul...
	FileCount      Property   int FileCount {get;set;}
	TotalTime      Property   timespan TotalTime {get;set;}	
Features
====
* Attempts to not copy files when unnecessary.
    * If file size is different and `Overwrite` switch is not enabled, do not copy.
    * If file size is the same and CRC check is the same, do not copy.
* Validates source and destination contents match

CRC-32 functionality was developed by Damien Guard - [Calculating CRC-32 in C# and .NET](http://damieng.com/blog/2006/08/08/calculating_crc32_in_c_and_net)

Performance
====
Test Case:
1,797 Files in 145 Folders
108MB

_Local System_<br />
**TeraCopy w/Test** 1:01<br />
**Qopy** 0:27<br />

_LAN_<br />
**TeraCopy w/Test** 2:02<br />
**Qopy** 1:31<br />

Todo
====
* Async operations
* Directory Check/Creation only once for each distinct destination path
* Move (delete source)?
* Validate only?

Use
====
**Requires Powershell v3.**

[Binary Download](http://www.christophr.me/blog/2013/04/02/new-project-qopy/)

1. Extract to `C:\Users\[username]\Documents\WindowsPowerShell\Modules` 
1.  `Import-Module Qopy`

If you use the `ShowProgress` switch without capturing the Output, the screen will get very flashy.


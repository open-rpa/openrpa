@{ 
ModuleToProcess = '..\OpenRPA.PS.dll'
ModuleVersion = '1.0.1'
GUID = '8212A209-EFF9-4EBE-9972-680A4A6257BE'
Author = 'Allan Zimmermann'
CompanyName = 'OpenRPA ApS'
Copyright = '(c) 2020 Allan Zimmermann. All rights reserved.'

Description = 'This module contains functions to manage all aspects of OpenRPA and OpenFlow'
PowerShellVersion = '2.0'

# Name of the Windows PowerShell host required by this module
PowerShellHostName = ''

# Minimum version of the Windows PowerShell host required by this module
PowerShellHostVersion = ''

# Minimum version of the .NET Framework required by this module
#DotNetFrameworkVersion = '2.0.50727'
DotNetFrameworkVersion = '4.0.30319'

# Minimum version of the common language runtime (CLR) required by this module
# CLRVersion = '4.0'
# CLRVersion = '2.0'

# Processor architecture (None, X86, Amd64, IA64) required by this module
ProcessorArchitecture = ''

# Modules that must be imported into the global environment prior to importing this module
RequiredModules = @()

# Assemblies that must be loaded prior to importing this module
RequiredAssemblies = @()

# Script files (.ps1) that are run in the caller's environment prior to importing this module
#ScriptsToProcess = @('openrpa.ps1')
ScriptsToProcess = @()

# Type files (.ps1xml) to be loaded when importing this module
TypesToProcess = @()

# Format files (.ps1xml) to be loaded when importing this module
FormatsToProcess = @('openrpa.ps1xml')

# Modules to import as nested modules of the module specified in ModuleToProcess
NestedModules = @()

# Functions to export from this module
FunctionsToExport = '*'

# Cmdlets to export from this module
CmdletsToExport = '*'

# Variables to export from this module
VariablesToExport = '*'

# Aliases to export from this module
AliasesToExport = '*'

# List of all modules packaged with this module
ModuleList = @()

# List of all files packaged with this module
FileList = @()

# Private data to pass to the module specified in ModuleToProcess
PrivateData = ''
}

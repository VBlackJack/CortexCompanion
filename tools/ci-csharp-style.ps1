#Requires -Version 5.1
<#
Copyright 2026 Julien Bombled

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

.SYNOPSIS
    Verifies explicit C# local-variable declarations in tracked source files.
.DESCRIPTION
    Scans Git-tracked C# files for the forbidden var declaration forms that local
    builds do not consistently promote to errors on every platform.
.EXAMPLE
    .\tools\ci-csharp-style.ps1
.NOTES
    Exit codes: 0 success, 1 style violation, 2 missing prerequisite, 3 scan failure.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path -Path $PSScriptRoot -ChildPath '..'))
$forbiddenPatterns = @(
    '^\s*var\s+',
    'foreach\s*\(\s*var\s+',
    '\bout\s+var\s+'
)

if ($null -eq (Get-Command -Name 'git' -ErrorAction SilentlyContinue)) {
    Write-Error 'git is required to enumerate tracked C# source files.'
    exit 2
}

try {
    [string[]]$relativePaths = @(
        & git -C $repositoryRoot ls-files -- '*.cs'
    )
    if ($LASTEXITCODE -ne 0) {
        throw [System.InvalidOperationException]::new('git ls-files failed.')
    }

    [string[]]$violations = @()
    foreach ($relativePath in $relativePaths) {
        [string]$absolutePath = Join-Path -Path $repositoryRoot -ChildPath $relativePath
        [string[]]$lines = [System.IO.File]::ReadAllLines($absolutePath)
        for ([int]$index = 0; $index -lt $lines.Length; $index++) {
            foreach ($pattern in $forbiddenPatterns) {
                if ($lines[$index] -match $pattern) {
                    $violations += '{0}:{1}: {2}' -f $relativePath, ($index + 1), $lines[$index].Trim()
                    break
                }
            }
        }
    }

    if ($violations.Count -gt 0) {
        $violations | Write-Output
        Write-Error ('Explicit C# type gate failed with {0} violation(s).' -f $violations.Count)
        exit 1
    }
}
catch {
    Write-Error ('C# style scan failed: {0}' -f $_.Exception.Message)
    exit 3
}

Write-Output ('Explicit C# type gate passed for {0} tracked file(s).' -f $relativePaths.Count)
exit 0

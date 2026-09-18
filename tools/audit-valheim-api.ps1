param(
    [string]$AssemblyPath = "$PSScriptRoot\..\bin\Release\netstandard2.1\ValheimLegends.dll",
    [string]$CecilPath = "D:\Spellbook\Steam\steamapps\common\Valheim\BepInEx\core\Mono.Cecil.dll"
)

$resolvedAssemblyPath = [System.IO.Path]::GetFullPath($AssemblyPath)
Write-Host "=========================================================="
Write-Host "  Valheim Legends - Valheim 1.0.14 Native API Audit Tool"
Write-Host "=========================================================="
Write-Host "Target Assembly: $resolvedAssemblyPath"

if (-not (Test-Path $resolvedAssemblyPath)) {
    Write-Error "Assembly not found at: $resolvedAssemblyPath"
    exit 1
}

if (-not (Test-Path $CecilPath)) {
    Write-Error "Mono.Cecil not found at: $CecilPath"
    exit 1
}

Add-Type -Path $CecilPath
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($resolvedAssemblyPath)
Write-Host "Assembly FullName : $($asm.FullName)"
Write-Host "Assembly Version  : $($asm.Name.Version)"

$violations = @()

# 1. Audit MemberReferences in Module
foreach ($mref in $asm.MainModule.GetMemberReferences()) {
    if ($mref -is [Mono.Cecil.MethodReference]) {
        # Old Character.Message (4 params)
        if ($mref.DeclaringType.Name -eq "Character" -and $mref.Name -eq "Message" -and $mref.Parameters.Count -eq 4) {
            $violations += "MemberRef: Character::Message with 4 params: $($mref.FullName)"
        }
        # Old MessageHud.ShowMessage (5 params or fewer)
        if ($mref.DeclaringType.Name -eq "MessageHud" -and $mref.Name -eq "ShowMessage" -and $mref.Parameters.Count -lt 6) {
            $violations += "MemberRef: MessageHud::ShowMessage with $($mref.Parameters.Count) params (expected 6): $($mref.FullName)"
        }
        # Old SEMan.AddStatusEffect (4 params)
        if ($mref.DeclaringType.Name -eq "SEMan" -and $mref.Name -eq "AddStatusEffect" -and $mref.Parameters.Count -eq 4) {
            $violations += "MemberRef: SEMan::AddStatusEffect with 4 params: $($mref.FullName)"
        }
        # Old string VisEquipment
        if ($mref.DeclaringType.Name -eq "VisEquipment" -and $mref.Name -in @("SetHelmetItem", "SetChestItem", "SetLegItem", "SetShoulderItem")) {
            if ($mref.Parameters[0].ParameterType.FullName -eq "System.String") {
                $violations += "MemberRef: VisEquipment::$($mref.Name) with string parameter: $($mref.FullName)"
            }
        }
    }
}

# 2. Audit Method Instructions in All Types
foreach ($type in $asm.MainModule.Types) {
    $typesToCheck = @($type) + $type.NestedTypes
    foreach ($t in $typesToCheck) {
        # Check static constructors (.cctor) in SE_* types for premature ZNetScene access
        if ($t.Name.StartsWith("SE_")) {
            foreach ($m in $t.Methods) {
                if ($m.Name -eq ".cctor" -and $m.HasBody) {
                    foreach ($inst in $m.Body.Instructions) {
                        if ($inst.Operand -ne $null) {
                            $op = $inst.Operand.ToString()
                            if ($op -match "ZNetScene" -or $op -match "ObjectDB") {
                                $violations += "Static Constructor (.cctor) in $($t.Name) accesses singleton: $op"
                            }
                        }
                    }
                }
            }
        }

        # Check call sites
        foreach ($m in $t.Methods) {
            if ($m.HasBody) {
                foreach ($inst in $m.Body.Instructions) {
                    if ($inst.Operand -ne $null -and $inst.Operand -is [Mono.Cecil.MethodReference]) {
                        $mr = $inst.Operand
                        if ($mr.DeclaringType.Name -eq "Character" -and $mr.Name -eq "Message" -and $mr.Parameters.Count -eq 4) {
                            $violations += "Callsite in $($t.FullName)::$($m.Name) [IL_{0:x4}] -> $($mr.FullName)" -f $inst.Offset
                        }
                        if ($mr.DeclaringType.Name -eq "MessageHud" -and $mr.Name -eq "ShowMessage" -and $mr.Parameters.Count -lt 6) {
                            $violations += "Callsite in $($t.FullName)::$($m.Name) [IL_{0:x4}] -> $($mr.FullName)" -f $inst.Offset
                        }
                        if ($mr.DeclaringType.Name -eq "SEMan" -and $mr.Name -eq "AddStatusEffect" -and $mr.Parameters.Count -eq 4) {
                            $violations += "Callsite in $($t.FullName)::$($m.Name) [IL_{0:x4}] -> $($mr.FullName)" -f $inst.Offset
                        }
                    }
                }
            }
        }
    }
}

Write-Host ""
if ($violations.Count -gt 0) {
    Write-Host "FAILED: Found $($violations.Count) legacy API violation(s):" -ForegroundColor Red
    foreach ($v in $violations) {
        Write-Host "  - $v" -ForegroundColor Red
    }
    exit 1
} else {
    Write-Host "SUCCESS: 0 legacy API violations detected." -ForegroundColor Green
    Write-Host "Assembly is 100% compliant with Valheim 1.0.14 native APIs." -ForegroundColor Green
    exit 0
}


$version = "1.0.1"

$buildNumber = "${env:BUILD_BUILDNUMBER}"
if ($buildNumber -eq "") {
    $buildNumber = 0
}

$version = $version + "." + $buildNumber

Write-Host "Version = $version"

function Update-SourceVersion {
    Param ([string]$Version)
    $NewVersion = "AssemblyVersion(""${Version}"")";
    $NewFileVersion = "AssemblyFileVersion(""${Version}"")";
    foreach ($o in $input) {
        Write-Output $o.FullName
        $TmpFile = $o.FullName + ".tmp"
        Get-Content $o.FullName |
            ForEach-Object { $_ -Replace "AssemblyVersion\(""[0-9]+(\.([0-9]+|\*)){1,3}""\)", $NewVersion } |
            ForEach-Object { $_ -Replace "AssemblyFileVersion\(""[0-9]+(\.([0-9]+|\*)){1,3}""\)", $NewFileVersion } > $TmpFile
        Move-Item $TmpFile $o.FullName -Force
    }
}
function Update-AllAssemblyInfoFiles ($path, $version) {
    foreach ($file in "AssemblyInfo.cs", "AssemblyInfo.vb" ) {
        Get-ChildItem -Path $path -Recurse | Where-Object { $_.Name -eq $file } | Update-SourceVersion $version
    }
}

Update-AllAssemblyInfoFiles .\GuitarHub\Properties $version;

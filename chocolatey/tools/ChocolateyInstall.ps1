$ErrorActionPreference = 'Stop'

$version = "${env:ChocolateyPackageVersion}"

$url = "https://github.com/fredimachado/GuitarHub/releases/download/v${version}/GuitarHub-${version}.zip"
$toolsPath = Split-Path $MyInvocation.MyCommand.Definition

# Create GuitarHub.exe.ui
Set-Content "${toolsPath}\${env:ChocolateyPackageTitle}.exe.ui" ""

# Install Zip Package
Install-ChocolateyZipPackage -PackageName ${env:ChocolateyPackageName} `
 -Url "$url" `
 -UnzipLocation "${toolsPath}"

# GuitarHub Chocolatey Package

This folder contains the nuspec used to generate a Chocolatey package and the Powershell script used to install it.

Official package can be found in https://chocolatey.org/packages/guitarhub

To create the package yourself, change directories to the folder containing the nuspec file and execute:

    choco pack

To set the version during package generation:

    choco pack --version=1.1.1

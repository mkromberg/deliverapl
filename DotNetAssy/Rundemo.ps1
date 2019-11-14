Add-Type -Path C:\devt\deliverapl\dotnetassy\bridge171-64_unicode.dll
Add-Type -Path C:\devt\deliverapl\dotnetassy\dyalognet.dll
[Reflection.Assembly]::LoadFile("c:\devt\deliverapl\dotnetassy\Paths.dll")
Write-Output ""

$test = [Mortens.Paths]::Involute(5)
Write-Output $test
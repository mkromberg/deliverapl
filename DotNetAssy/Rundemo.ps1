Add-Type -Path C:\devt\deliverapl\dotnetassy\bridge171-64_unicode.dll
Add-Type -Path C:\devt\deliverapl\dotnetassy\dyalognet.dll
[Reflection.Assembly]::LoadFile("c:\devt\deliverapl\dotnetassy\Paths.dll")
$text = [Mortens.Paths]::Involute(5)
Write-Output $text
Write-Output "Done"
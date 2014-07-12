$scriptDir      = split-path $script:MyInvocation.MyCommand.Path
$rootDir        = (get-item $scriptDir ).parent.parent.parent.FullName
$samplesDir     = Join-Path (get-item $scriptDir ).parent.FullName "sample"
$sampleCsxFile  = "Nake.csx" 
$sampleBatFile  = "Nake.bat" 

if ((Test-Path "$rootDir\Nake.csx") -or (Test-Path "$rootDir\Nake.bat")) {
	Exit
}

Write-Host "Copying sample script and runner to root folder ..." -ForegroundColor DarkYellow
Copy-Item "$samplesDir\$sampleCsxFile" $rootDir
Copy-Item "$samplesDir\$sampleBatFile" $rootDir
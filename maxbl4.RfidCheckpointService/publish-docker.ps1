#1.0.9

function Main()
{
    $version = GetNextVersion
    
    $publishRoot = "./_build"
    $containerRoot = "./_build/bin"
    $imageName = "checkpoint-service"    
    $platforms = @("amd64", "armhf")
    rmdir -Force -Recurs $publishRoot
    cp -r ./lib "$publishRoot/lib" 
    dotnet publish -c Release /p:Version=$version -o $containerRoot
    pushd
    cd checkpoint-service-ui
    ng build --prod --output-path ..\_build\wwwroot
    popd
    foreach ($p in $platforms)
    {
        $version
        docker build --pull --platform $p -t "maxbl4/$imageName-$($p):$version" -t "maxbl4/$imageName-$($p):latest" -f dockerfile $publishRoot
        docker push "maxbl4/$imageName-$($p):$version"
        docker push "maxbl4/$imageName-$($p):latest"
    }

    UpdateVersion $version
}

function GetNextVersion()
{
    $lines = Get-Content $MyInvocation.ScriptName
    $version = [System.Version]::Parse($lines[0].Substring(1))
    return "$($version.Major).$($version.Minor).$($version.Build + 1)"
}

function UpdateVersion($version)
{
    $lines = Get-Content $MyInvocation.ScriptName
    $lines[0] = "#$version"
    $lines > $MyInvocation.ScriptName
}

Main

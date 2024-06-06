
param (
    $P3RProjectPath = "",
    $GenericProjectPath = "",
    $GithubReleasePath = "",
    $NugetReleasePath = ""
)
function New-Folder {
    param (
        $folder_name
    )
    Remove-Item $folder_name -Recurse -ErrorAction SilentlyContinue
    New-Item $folder_name -ItemType Directory -ErrorAction SilentlyContinue
}

function New-Dotnet-Project {
    param (
      $project_name,
      $output_name
    )
    dotnet restore $project_name/$project_name.csproj
    dotnet clean $project_name/$project_name.csproj
    dotnet build $project_name/$project_name.csproj -c Release -r win-x64 --self-contained false -o $output_name 
    Compress-Archive -Path (Get-ChildItem -Path $output_name* -Exclude "*.nupkg") -Force $GithubReleasePath/$project_name"_1.5.0.zip"
    Copy-Item -Path $output_name/* -Include "*.nupkg" -Destination $NugetReleasePath -Recurse
}

New-Folder $GenericProjectPath
New-Folder $P3RProjectPath

New-Folder $GithubReleasePath
New-Folder $NugetReleasePath

New-Dotnet-Project riri.commonmodutils $GenericProjectPath
New-Dotnet-Project p3rpc.commonmodutils $P3RProjectPath
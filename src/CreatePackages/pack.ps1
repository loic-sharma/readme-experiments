dotnet build

$readmes = Get-ChildItem -File -Recurse -Filter *README.md D:\Code\readme-experiments\readmes\

$readmes | % {
  $owner = $_.Directory.Parent.Name
  $repo = $_.Directory.Name
  $readmePath = $_.FullName

  $packageId = "Loic.$owner.$repo"
  $repoUrl = "https://www.github.com/$owner/$repo#readme"

  dotnet pack `
    -p:PackageId=$packageId `
    -p:Readme=$readmePath `
    -p:PackageProjectUrl=$repoUrl `
    -o "D:\Code\readme-experiments\nupkgs" `
    --no-build
}
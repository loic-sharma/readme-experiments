The `packages-from-github.csv` is generated using the following Kusto query:

```kusto
let latestVersions = NiPackageVersions
    | where IsLatest
    | where IsListed
    | project Identity;
NiPackageManifests
| where Identity in ( latestVersions )
| join ( NiPackageDownloads | project Identity, TotalDownloads ) on Identity
| extend Readme = iff(Readme != "", "Yes", "No")
| project LowerId, ProjectUrl, RepositoryUrl = RepositoryMetadata["Url"], Readme, TotalDownloads
| where ProjectUrl contains "github.com" or RepositoryUrl contains "github.com"
| order by TotalDownloads desc
```
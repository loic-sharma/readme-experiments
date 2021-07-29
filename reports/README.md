The `community-packages-from-github.csv` is generated using the following Kusto query:

```kusto
let latestVersions = NiPackageVersions
    | where IsLatest
    | where IsListed
    | where LowerId !startswith "microsoft."
    | where LowerId !startswith "system."
    | where LowerId !startswith "runtime."
    | project Identity;
NiPackageManifests
| where Identity in ( latestVersions )
| join ( NiPackageDownloads | project Identity, TotalDownloads ) on Identity
| extend Readme = iff(Readme != "", "Yes", "No")
| project LowerId, ProjectUrl, RepositoryUrl = RepositoryMetadata["Url"], Readme, TotalDownloads
| where ProjectUrl contains "github.com" or RepositoryUrl contains "github.com"
| order by TotalDownloads desc
| serialize CumulativeTotalDownloads = row_cumsum(TotalDownloads)
| as x
| extend CumulativeTotalDownloadsPct = bin(100.0 * CumulativeTotalDownloads / toscalar(x | summarize sum(TotalDownloads)), 0.001)
| project LowerId, ProjectUrl, RepositoryUrl, TotalDownloads, CumulativeTotalDownloadsPct, Readme
```


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
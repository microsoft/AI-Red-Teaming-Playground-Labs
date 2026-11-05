# Filesystem storage regression tests

Run from the repository root:

```shell
dotnet test tests/FileSystemStorage.Tests/FileSystemStorage.Tests.csproj
```

The tests reopen existing storage before changing a value or deleting records,
then reload it again to check the persisted JSON. They cover shorter and longer
values and deletion of one or all records. They use temporary files and synthetic data;
no model API, credentials, running application, or Docker is required.

The project targets the backend's .NET 6 framework and permits runtime
roll-forward when a newer runtime is installed.

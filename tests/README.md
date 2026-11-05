# Backend regression tests

Run from the repository root:

```shell
dotnet test tests/CopilotChatWebApi.Tests/CopilotChatWebApi.Tests.csproj
```

These tests use temporary filesystem storage and synthetic instructions. They do
not start the playground, invoke a model, read API keys, or require Docker. The
test project targets the same .NET 6 framework as the backend; its roll-forward
setting also permits running with a newer installed runtime.

The tests cover response redaction without mutation, filesystem persistence,
session creation/read/list operations, and title/memory edits with hidden versus
editable descriptions. SignalR edit notifications must stay redacted while the
server-owned instructions remain intact.

The edit cases cover empty, omitted, and non-empty descriptions. Hidden empty
values are treated as a redacted echo, while explicit non-empty replacements
keep their existing behavior. Clearing a visible description remains supported.

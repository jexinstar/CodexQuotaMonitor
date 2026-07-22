# CodexQuotaMonitor Architecture

## Runtime data flow

```text
%USERPROFILE%\.codex\sessions\**\*.jsonl
  -> Services/LocalLogs/CodexFileScanner
  -> Services/LocalLogs/UsageParser
  -> Services/LocalLogs/LocalCodexQuotaService
  -> ViewModels/MainViewModel
  -> Views/MainWindow + Controls
```

The application only reads `event_msg` records whose payload is a
`token_count` event with a `rate_limits` object. The newest event timestamp is
the quota-change time shown in the dashboard. No CLI, app-server, network, or
authentication storage is part of the runtime path.

## Folder responsibilities

| Folder | Responsibility |
| --- | --- |
| `Commands` | UI command primitives. |
| `Controls` | Reusable visual controls only. |
| `Converters` | Stateless XAML value conversion. |
| `Models` | Display and persisted-data contracts. |
| `Native` | Windows/DWM interop and backdrop fallback. |
| `Services/LocalLogs` | Read-only Codex session-log discovery, parsing, and data-source implementation. |
| `Services` | Shared infrastructure: settings, logging, themes, mock data, and service contracts. |
| `Themes` | Theme resources and reusable styles. |
| `ViewModels` | UI state, commands, and service orchestration. |
| `Views` | Window layout and interaction wiring. |

## Dependency rules

- Views bind to ViewModels and do not access services directly.
- Controls expose dependency properties and do not access ViewModels or files.
- The local-log service is read-only and returns `QuotaSummary` through
  `IQuotaService`.
- Services do not construct WPF controls or call into window code.
- Logging records only operational metadata; it never records session-log
  content, account identity, tokens, cookies, or credentials.


# SyncTool

[![CI](https://github.com/equ1p/sync-tool/actions/workflows/ci.yml/badge.svg)](https://github.com/equ1p/sync-tool/actions/workflows/ci.yml)

A command line tool that keeps a replica folder as an exact, one-way copy of a source folder.
The source folder is only ever read.

## Usage

```bash
dotnet run --project SyncTool -- --source ./data --replica ./backup --interval 30 --log ./sync.log
```

| Option | Short | Description |
| --- | --- | --- |
| `--source` | `-s` | Folder to read from. Must exist. Never modified. |
| `--replica` | `-r` | Folder kept identical to the source. Created if missing. |
| `--interval` | `-i` | Delay between runs, in whole seconds. |
| `--log` | `-l` | Log file. Appended to; parent folders are created. |
| `--once` | | Synchronize once and exit instead of looping. |
| `--help` | `-h` | Show usage. |

The first run happens immediately, then once per interval. Ctrl+C stops the tool after the
current operation and closes the log file.

Exit codes: `0` normal, `1` invalid arguments, `2` the log file could not be opened.

```
2026-10-04 19:12:07.114 [INFO] Synchronization started.
2026-10-04 19:12:07.118 [INFO] Created directory: /home/ivan/backup/docs
2026-10-04 19:12:07.121 [INFO] Copied file: /home/ivan/data/docs/a.txt -> /home/ivan/backup/docs/a.txt
2026-10-04 19:12:07.122 [INFO] Deleted file: /home/ivan/backup/old.txt
2026-10-04 19:12:07.122 [INFO] Synchronization finished: 1 directory(ies) created, 1 file(s) copied, 0 file(s) updated, 1 file(s) deleted, 0 directory(ies) deleted, 0 error(s)
```

## Build and test

```bash
dotnet build
dotnet test
```

## How it works

Every run snapshots both trees as sets of paths relative to their own root, then:

1. deletes replica files the source does not have;
2. deletes replica directories the source does not have, deepest first;
3. creates missing directories, shallowest first;
4. copies files that are new or whose content differs.

Deleting before creating is what makes a path that changed kind — a file that became a folder,
or the reverse — work with no special case. Nothing is remembered between runs, so the tool
recovers by itself from a crash or from anything that changed the replica behind its back.

## Design decisions

**Change detection compares length, then an MD5 hash.** Timestamps are cheaper but a file
rewritten with the same size and modification time would leave the replica stale, and the
requirement is an exact match. MD5 only detects changes here and is not used for security.

**An unavailable source aborts the run instead of emptying the replica.** An unmounted share
enumerates exactly like an empty folder, so without this guard the tool would faithfully delete
the whole replica. The run fails loudly, the error is logged, and the next run retries.

**Hidden and system files are copied.** `EnumerationOptions` skips them by default, which would
silently make the replica not an exact copy.

**Failures are isolated per entry.** A locked or unreadable file is logged, counted and skipped;
the rest of the run continues.

**No file system abstraction.** Almost every line of the synchronizer is a `System.IO` call, so
the 30 tests run against real folders in the system temp directory instead of mocks.

**Path comparison follows the host**: ordinal on Linux, case-insensitive on Windows and macOS.
CI runs the suite on both Linux and Windows.

No third-party dependencies in the tool itself; NUnit is used only in the test project.

## Known limitations

- Directory symlinks are followed, so a link pointing at one of its own ancestors would loop.
- Permissions, ownership and alternate data streams are not replicated; content, names and
  structure are.
- A file being written while it is copied may be copied partially; the next run corrects it.
- Shutdown is wired to Ctrl+C only.

## Layout

```
SyncTool/             the tool (Cli/ Logging/ Sync/)
SyncTool.Tests/       NUnit tests
.github/workflows/    CI
```

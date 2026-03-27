# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build System

This project uses [NUKE](https://nuke.build/) as its build system. Build scripts are at the root:

```bash
./build.sh [target]        # Linux/macOS
./build.ps1 [target]       # PowerShell
./build.cmd [target]       # Windows CMD
```

Common targets:
- `./build.sh Restore` — restore NuGet packages and secrets
- `./build.sh Build` — compile the solution
- `./build.sh Test` — run all tests
- `./build.sh Rebuild` — clean + build
- `./build.sh Release` — build and package NuGet artifacts (output to `.output/`)

The main solution is `src/K4os.Compression.LZ4.sln`. You can also use `dotnet` directly:

```bash
dotnet build src/K4os.Compression.LZ4.sln
dotnet test src/K4os.Compression.LZ4.sln
# Run a specific test project:
dotnet test src/K4os.Compression.LZ4.Tests/K4os.Compression.LZ4.Tests.csproj
dotnet test src/K4os.Compression.LZ4.Streams.Tests/K4os.Compression.LZ4.Streams.Tests.csproj
dotnet test src/K4os.Compression.LZ4.Legacy.Tests/K4os.Compression.LZ4.Legacy.Tests.csproj
# Run a specific test:
dotnet test src/K4os.Compression.LZ4.Tests/ --filter "FullyQualifiedName~TestName"
```

Tests use xUnit. CI runs on `windows-latest` via GitHub Actions (`.github/workflows/continuous.yml`).

**Note**: The build system (`./build.ps1`) is Windows-only — it downloads Windows tools (`7z.exe`, `lz4.exe`). `dotnet` commands work cross-platform.

## Architecture

### Three NuGet Packages / Projects

**K4os.Compression.LZ4** (`src/K4os.Compression.LZ4/`) — block compression
- Targets: net462, net8.0, net10.0
- Public API: `LZ4Codec` (static compress/decompress), `LZ4Pickler` (simple length-prefixed messages)
- Internally split into `Engine/x32/` and `Engine/x64/` — separate 32-bit (memory-aligned) and 64-bit (unaligned/optimized) algorithm implementations
- `Encoders/` contains strategy implementations for each compression level: Fast, HC (High Compression), Opt (Optimized), Max

**K4os.Compression.LZ4.Streams** (`src/K4os.Compression.LZ4.Streams/`) — LZ4 Frame format streaming
- Depends on: `K4os.Compression.LZ4`, `K4os.Hash.xxHash`, `System.IO.Pipelines`
- Implements the LZ4 frame format spec (interoperable with the official `lz4` CLI tool)
- `Abstractions/` — `ILZ4FrameReader` / `ILZ4FrameWriter` interfaces
- `Adapters/` — implementations for Stream, Memory, Span, ReadOnlySequence, PipeReader/PipeWriter, IBufferWriter
- `Frames/` — frame header/footer parsing and writing logic
- Supports both sync and async I/O

**K4os.Compression.LZ4.Legacy** (`src/K4os.Compression.LZ4.Legacy/`) — compatibility with old `lz4net` format
- Targets net462 only; for migrating codebases that used the original `lz4net` library

### Dependency Graph

```
K4os.Compression.LZ4.Streams
    └── K4os.Compression.LZ4
    └── K4os.Hash.xxHash

K4os.Compression.LZ4.Legacy  (independent)
```

### Internal Code Generation

The x32/x64 engine split is maintained via code preprocessing (NUKE `Preprocess` target converts 64→32-bit code). Do not manually edit generated files in `Engine/x32/` — they are derived from `Engine/x64/`.

### Multi-targeting Notes

- `System.Memory` and `System.Runtime.CompilerServices.Unsafe` are polyfilled for older targets
- `PolySharp` is used to backport newer C# language features to older TFMs
- Unsafe code is enabled project-wide; pointer arithmetic is common in the engine layer
- ARMv7 / IL2CPP / Unity have known constraints documented in README.md

## Versioning and Publishing

- Version is derived from `CHANGES.md` (top entry) via a custom NUKE task — bump the version there before releasing
- Strong-name signing is enabled for all public assemblies (key in `res/`)
- Packages are published to nuget.org via `./build.sh PublishToNuget` (requires API key in secrets)

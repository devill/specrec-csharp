# Fixture Testing Strategy

## Overview

Fixture testing validates refactoring tools by running commands against real C# solutions and comparing results against expected outputs.

## Structure

```
SpecRec.CLI.Tests/fixtures/[command-name]/
├── fixture.config.json           # Test configurations
├── input/                        # Complete C# solution to test against
│   ├── Project.sln
│   ├── Project.csproj
│   └── *.cs                      # Source files
├── [test-id].expected/           # Expected result after refactoring
│   └── *.cs                      # Only new/modified files (unchanged files omitted)
├── [test-id].expected.out        # Expected console output
└── [test-id].expected.err        # Expected error output
```

## Configuration Format

`fixture.config.json` contains test cases:

```json
[
  {
    "id": "basic-wrapper",
    "description": "Generate wrapper for DatabaseService",
    "command": "generate-wrapper DatabaseService.cs",
    "skip": false
  }
]
```

## Test Execution Flow

1. **Setup**: Copy `input/` → `[test-id].received/`
2. **Execute**: Run command in received directory, capture stdout/stderr
3. **Validate**: 
   - Compare console output with `[test-id].expected.out` and `[test-id].expected.err`
   - For files in `expected/`: must exist and match in `received/`
   - For files missing from `expected/`: if unchanged from `input/`, test passes
   - For `*.removed` files in `expected/`: corresponding file must not exist in `received/`
4. **Cleanup**: Remove temp files on success, preserve on failure

## Creating New Fixtures

1. Create command directory under `fixtures/`
2. Build minimal but complete C# solution in `input/`
3. Create `[test-id].expected/` with only new/modified files
4. Create `[test-id].expected.out` and `[test-id].expected.err` files
5. Add test case to `fixture.config.json`

Example input solution:
- `.sln` and `.csproj` files for proper MSBuild context
- Source class to refactor
- Another class using it (shows realistic usage)
- Reference to SpecRec package if needed

The expected directory contains only files that are new or modified - unchanged files are omitted and automatically validated by comparing with input.
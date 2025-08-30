# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Build and Test
- Build: `dotnet build`
- Run all tests: `dotnet test`
- Run specific test: `dotnet test --filter "TestClassName.TestMethodName"`
- Run single test with specific case: `dotnet test --filter "TestClassName.TestMethodName.CaseName"`

### Important Testing Notes
- **NEVER** prepend `dotnet test` with `CLAUDECODE=1`! The environment variable is already set for you
- Tests use Verify framework - failed tests create `.received.txt` files to review
- SpecRec tests use data-driven testing via `.verified.txt` files with test scenarios

## Architecture Overview

SpecRec is a C# library for testing legacy code by recording and replaying method interactions. The architecture consists of several key components:

### Core Components

**ObjectFactory (`ObjectFactory.cs`)**
- Singleton pattern for dependency injection without constructor changes
- Replaces `new` instantiation with `Create<T>()` calls
- Supports queued objects (`SetOne`), persistent objects (`SetAlways`), and auto-parrot mode
- Handles object ID registration for clean specification formatting
- Global convenience methods in `GlobalObjectFactory` class

**Context (`Context.cs`)**
- High-level API that coordinates ObjectFactory, CallLogger, and Parrot
- Primary entry point for SpecRec test scenarios via `Context.Verify()`
- Fluent interface for setting up test doubles: `Substitute<T>()`, `Wrap<T>()`, etc.
- Manages test case naming and object lifecycle

**CallLogger (`CallLogger.cs`)**
- Records method calls and their parameters/return values
- Uses Castle.Core DynamicProxy (`CallLoggerProxy<T>`) for transparent interception
- Outputs human-readable specifications in emoji-decorated format
- Supports filtering sensitive data via `CallLogFormatterContext`
- Thread-safe logging context management

**Parrot (`Parrot.cs`)**
- Replays recorded interactions from verified specification files
- Creates dynamic proxies that return pre-defined values based on method signature matching
- Works with `CallLog.GetNextReturnValue()` for sequential replay
- Throws specific exceptions for mismatched calls or missing return values

**CallLog (`CallLog.cs`)**
- Data structure for storing and parsing call specifications
- Handles type-safe value parsing from text format
- Supports object ID resolution via ObjectFactory integration
- Core parsing logic for verified specification files

### Key Patterns

**Proxy System**
- Uses unified `ProxyFactory` with Castle.Core DynamicProxy
- Single `UnifiedProxyInterceptor` handles both logging and parrot modes
- `CallLoggerProxy<T>` extends DispatchProxy for method interception
- Supports `IConstructorCalledWith` interface for constructor parameter logging

**Specification Format**
- Emoji-decorated human-readable format: `📧 SendEmail:` with `🔸` for parameters, `🔹` for returns
- Type-safe parsing: strings need quotes, booleans are `True`/`False`, arrays use `[1,2,3]` format
- Object references use `<id:objectName>` syntax for complex types
- Test inputs can be embedded in verified files with `📋 <Test Inputs>` sections

**Testing Framework Integration**
- `SpecRecLogsAttribute` for data-driven testing from verified files
- `SpecRecExecutor` handles xUnit theory execution with file discovery
- `FileDiscoveryService` automatically finds test cases from `.verified.txt` files
- Integrates with Verify framework for approval-style testing

### Thread Safety
- `CallLogFormatterContext` uses ThreadLocal storage for logging state
- ObjectFactory registry operations are protected with locks
- Context isolation prevents test interference in parallel execution
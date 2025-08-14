# C# Wrapper Generation Tool Implementation Plan

## Status Update

✅ **Completed (2025-08-08)**: Basic CLI project structure with NuGet tool packaging, command parsing using System.CommandLine, and working test infrastructure - ready for implementing actual wrapper generation logic.

**Suggested next steps**: Implement a minimal working version of `generate-wrapper` command that can parse a simple C# class and generate basic interface + wrapper files, focusing on learning the Roslyn API through concrete implementation rather than premature abstractions.

## Overview

This document outlines the implementation plan for an automated refactoring tool that generates wrapper classes and interfaces for existing C# classes, enabling dependency injection and testing with the existing SpecRec ObjectFactory infrastructure.

## Problem Statement

C# doesn't provide a way to implement two different interfaces that are functionally the same but not explicitly implementing the same interface. This makes it impossible to use test doubles for dependencies that don't provide interfaces we can implement.

## Solution

Create an automated refactoring tool using Roslyn syntax trees to:
1. Generate wrapper classes and interfaces from existing classes
2. Replace direct instantiation with ObjectFactory.Create calls throughout the codebase
3. Support complex scenarios including inheritance hierarchies and external dependencies

## **CRITICAL Requirements**

### Code Formatting
- **NEVER implement custom formatting logic**
- **ALWAYS** rely on Roslyn's `NormalizeWhitespace()` for all code formatting
- Test fixtures must match exactly what Roslyn produces by default
- Custom formatting leads to maintenance burden and inconsistencies
- Let the established .NET tooling handle formatting concerns

### Approval Test Strategy
- **Expected files may be incorrect** - They are best guesses and can contain mistakes
- **When tests fail, analyze intelligently**:
  - Compare the expected vs received behavior
  - Determine which behavior is actually preferable
  - Update expected files if the received behavior is better
  - Only change implementation if the expected behavior is clearly correct
- **Examples of when to update expected files**:
  - CLI error messages that are clearer or more standard
  - Output formatting that follows .NET conventions better
  - Generated code that is more idiomatic or readable
- **Think critically** - Don't blindly implement whatever the expected file says

## Architecture

### CLI Tool Structure
```
SpecRec.sln
├── SpecRec/ (existing library)
├── SpecRec.Tests/ (existing tests)  
└── SpecRec.CLI/ (new CLI tool project)
    ├── Program.cs
    ├── Commands/
    │   ├── GenerateWrapperCommand.cs
    │   └── ReplaceReferencesCommand.cs
    ├── Core/
    │   ├── WrapperGenerator.cs
    │   ├── TypeAnalyzer.cs
    │   ├── InterfaceGenerator.cs
    │   ├── WrapperImplementationGenerator.cs
    │   ├── InheritanceAnalyzer.cs
    │   ├── CodeReplacementEngine.cs
    │   └── SafeFileModifier.cs
    └── SpecRec.CLI.csproj
```

### Distribution Strategy
- CLI tool packaged as NuGet global tool via `PackAsTool=true`
- Installation: `dotnet tool install -g SpecRec.CLI`
- Usage: `specrec generate-wrapper MyClass --hierarchy-mode full`

## Phase 1: Core Wrapper Generation

### Acceptance Criteria
- Generate wrappers from any C# class
- Support classes from external packages
- Support COM interoperability interfaces (VB.NET via COM)
- Handle inheritance hierarchies with user choice (single class vs full hierarchy)

### Generated Code Pattern
```csharp
// Original: ExternalLibrary.DatabaseService
public interface IDatabaseService
{
    void Connect(string connectionString);
    DataSet Query(string sql);
}

public class DatabaseServiceWrapper : IDatabaseService  
{
    private readonly DatabaseService _wrapped;
    
    public DatabaseServiceWrapper(DatabaseService wrapped) => _wrapped = wrapped;
    
    public void Connect(string connectionString) => _wrapped.Connect(connectionString);
    public DataSet Query(string sql) => _wrapped.Query(sql);
}
```

### Inheritance Hierarchy Handling
- Default behavior: prompt user to wrap entire hierarchy
- Mirror inheritance in generated interfaces
- Example:
  ```
  ? Class SqlServerDatabaseService has inheritance hierarchy:
    BaseService -> DatabaseService -> SqlServerDatabaseService
    
    Wrap entire hierarchy? [Y/n] 
    (Y = wrap all 3 classes, n = wrap SqlServerDatabaseService only)
  ```

## Phase 2: Code Replacement System

### Acceptance Criteria
- Find and replace all references to wrapped classes
- Replace `new SomeClass()` with `ObjectFactory.Instance().Create<ISomeClass, SomeClassWrapper>()`
- Handle constructor parameters correctly
- Support existing classes that already implement interfaces

### Replacement Patterns
```csharp
// Before
var service = new DatabaseService(connectionString);

// After  
var service = ObjectFactory.Instance().Create<IDatabaseService, DatabaseServiceWrapper>(
    new DatabaseService(connectionString));
```

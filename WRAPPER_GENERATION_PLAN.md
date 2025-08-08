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

public class DatabaseServiceWrapper : IDatabaseService, IConstructorCalledWith  
{
    private readonly DatabaseService _wrapped;
    
    public DatabaseServiceWrapper(DatabaseService wrapped) => _wrapped = wrapped;
    
    public void Connect(string connectionString) => _wrapped.Connect(connectionString);
    public DataSet Query(string sql) => _wrapped.Query(sql);
    
    public void ConstructorCalledWith(ConstructorParameterInfo[] parameters) 
    {
        // Integration with existing SpecRec logging infrastructure
    }
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

## Implementation Strategy

### Safety-First File Modification
```csharp
public class SafeFileModifier 
{
    public async Task<ModificationResult> ModifyFiles(List<FileModification> modifications)
    {
        // 1. Pre-flight validation
        ValidateGitRepository(); // Must be in git repo
        ValidateNoUncommittedChanges(); // Working directory must be clean
        
        // 2. Create backup branch
        CreateBackupBranch($"specrec-backup-{DateTime.Now:yyyyMMdd-HHmmss}");
        
        // 3. Dry run validation
        var dryRunResult = await ValidateAllModifications(modifications);
        if (!dryRunResult.IsValid) return dryRunResult;
        
        // 4. Apply changes atomically
        return await ApplyModifications(modifications);
    }
}
```

### Required NuGet Dependencies
```xml
<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" />
<PackageReference Include="Microsoft.CodeAnalysis.Common" Version="4.8.0" />  
<PackageReference Include="System.Reflection.Metadata" Version="8.0.0" />
```

## Testing Strategy

### Multi-Layer Testing Approach

#### Layer 1: Unit Tests
- Individual component tests using existing Verify approval testing pattern
- Test each generator component in isolation
- Comprehensive edge case coverage

#### Layer 2: Integration Tests  
- Full file modification tests with real C# files
- Compilation validation after modifications
- Approval testing for complete file transformations

#### Layer 3: End-to-End CLI Tests
- Complete workflow testing with real git repositories
- Validation that modified projects compile and tests pass
- Safety mechanism testing (backup branch creation, rollback scenarios)

### Critical Test Scenarios
- Complex inheritance hierarchies (5+ levels)
- Generic classes with constraints
- COM interop classes from VB.NET
- Files with existing ObjectFactory usage
- Large files (1000+ lines)
- Edge cases: partial classes, nested classes, async methods

## Implementation Timeline

### Phase 1: Core CLI Infrastructure (Week 1)
- Set up SpecRec.CLI project with NuGet tool packaging
- Implement command structure with argument parsing
- Create SafeFileModifier with git integration and safety checks
- TDD: Basic file modification with comprehensive test coverage

### Phase 2: Wrapper Generation Engine (Week 2)  
- Implement Roslyn-based wrapper generation
- Add inheritance hierarchy analysis and user prompting
- Support external assemblies and COM interop
- TDD: Complete approval testing for all wrapper generation scenarios

### Phase 3: Code Replacement Engine (Week 3)
- Implement in-place code replacement using syntax tree rewriting
- ObjectFactory integration with proper parameter handling
- Handle complex scenarios (generics, inheritance, nested classes)
- TDD: End-to-end CLI tests with real projects

### Phase 4: Production Readiness (Week 4)
- Performance optimization for large codebases
- Enhanced error messages and user experience
- Complete documentation and examples
- Final integration testing with external COM libraries

## Risk Mitigation
- Week 1 focus on safety mechanisms prevents data loss
- Comprehensive approval testing catches regressions
- Git integration ensures easy rollback
- Dry-run validation prevents broken code generation
- Atomic file operations prevent partial updates

## Integration with Existing SpecRec Infrastructure

The wrapper generation tool integrates seamlessly with existing SpecRec components:

- **ObjectFactory**: Generated wrappers use `ObjectFactory.Create<I,T>()` pattern
- **IConstructorCalledWith**: All wrappers implement this interface for constructor logging
- **CallLogger**: Method calls can be logged using existing infrastructure
- **Verify Testing**: All generated code validated using existing approval testing patterns

This ensures the tool enhances rather than replaces the existing SpecRec ecosystem.
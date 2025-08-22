# Phase 3: Parrot Updates

## Overview
Update Parrot to parse `<id:string_id>` format from verified files and resolve objects from ObjectFactory registry, enabling object replay functionality.

## Current State Analysis
- `CallLog.ParseValue()` at line 283 handles value parsing from verified files
- `Parrot.ConvertReturnValue()` at line 44 handles type conversion
- Current parsing: null, booleans, numbers, quoted strings, arrays
- No object ID parsing or registry integration exists

## Goals
1. Parse `<id:string_id>` format in verified files
2. Resolve objects from ObjectFactory registry
3. Handle `<unknown>` values with appropriate errors
4. Maintain existing parsing for primitives
5. Add new exception type for unknown objects

---

## Tests to Implement First (TDD)

### Test File: `ParrotObjectIdTests.cs`

#### 1. Basic Object ID Parsing Tests
```csharp
[Fact]
public async Task ParseValue_WithIdFormat_ShouldResolveRegisteredObject()
[Fact]
public async Task ParseValue_WithMultipleIds_ShouldResolveCorrectObjects()
[Fact]
public async Task ParseValue_WithInvalidIdFormat_ShouldThrowException()
[Fact]
public async Task ParseValue_WithEmptyId_ShouldThrowException()
```

#### 2. Unknown Object Handling Tests
```csharp
[Fact]
public async Task ParseValue_WithUnknownMarker_ShouldThrowParrotUnknownObjectException()
[Fact]
public async Task Parrot_WithUnknownInVerifiedFile_ShouldFailImmediately()
[Fact]
public async Task Parrot_WithUnknownReturnValue_ShouldProvideHelpfulErrorMessage()
```

#### 3. Object Registry Integration Tests
```csharp
[Fact]
public async Task Parrot_WithRegisteredObjects_ShouldReturnCorrectInstances()
[Fact]
public async Task Parrot_WithUnregisteredId_ShouldThrowParrotCallMismatchException()
[Fact]
public async Task Parrot_WithChangedRegistry_ShouldReflectCurrentState()
```

#### 4. Type Conversion Tests
```csharp
[Fact]
public async Task ConvertReturnValue_WithRegisteredObject_ShouldMaintainType()
[Fact]
public async Task ConvertReturnValue_WithWrongType_ShouldThrowTypeConversionException()
[Fact]
public async Task ConvertReturnValue_WithInterfaceType_ShouldReturnImplementation()
```

#### 5. End-to-End Parrot Workflow Tests
```csharp
[Fact]
public async Task ParrotWorkflow_RegisterCreateReplay_ShouldWork()
[Fact]
public async Task ParrotWorkflow_WithMixedPrimitivesAndObjects_ShouldWork()
[Fact]
public async Task ParrotWorkflow_WithNestedObjectCalls_ShouldWork()
```

#### 6. Preserve Existing Behavior Tests
```csharp
[Fact]
public async Task ParseValue_WithPrimitives_ShouldKeepExistingBehavior()
[Fact]
public async Task ParseValue_WithArrays_ShouldKeepExistingBehavior()
[Fact]
public async Task ParseValue_WithNullValues_ShouldKeepExistingBehavior()
```

---

## Implementation Plan

### Step 1: Add ObjectFactory Reference to CallLog
```csharp
private readonly ObjectFactory? _objectFactory;

// Update constructors
public CallLog(string? verifiedContent = null, ObjectFactory? objectFactory = null)

// Update static factory methods
public static CallLog FromFile(string filePath, ObjectFactory? objectFactory = null)
public static CallLog FromVerifiedFile(ObjectFactory? objectFactory = null, [CallerMemberName] string? testName = null, [CallerFilePath] string? sourceFilePath = null)
// ... other factory methods
```

### Step 2: Add New Exception Type
```csharp
public class ParrotUnknownObjectException : ParrotException
{
    public ParrotUnknownObjectException(string message) : base(message) { }
    public ParrotUnknownObjectException(string message, Exception innerException) : base(message, innerException) { }
}
```

### Step 3: Modify ParseValue Method
```csharp
private object? ParseValue(string valueStr)
{
    if (valueStr == "null") return null;
    if (valueStr == "<null>") return null; // Legacy support
    
    // NEW: Handle object ID format
    if (valueStr == "<unknown>")
    {
        throw new ParrotUnknownObjectException(
            "Encountered <unknown> object in verified file. " +
            "Register all objects with ObjectFactory before running tests.");
    }
    
    // NEW: Parse <id:string_id> format
    if (TryParseObjectId(valueStr, out var objectId))
    {
        return ResolveObjectById(objectId);
    }
    
    // Existing parsing logic...
    if (valueStr == "<missing_value>") return "<missing_value>";
    if (valueStr == "true") return true;
    // ... rest of existing logic
}

private bool TryParseObjectId(string valueStr, out string objectId)
{
    objectId = "";
    var pattern = @"^<id:(.+)>$";
    var match = Regex.Match(valueStr, pattern);
    if (match.Success)
    {
        objectId = match.Groups[1].Value;
        return true;
    }
    return false;
}

private object ResolveObjectById(string objectId)
{
    if (_objectFactory == null)
    {
        throw new ParrotCallMismatchException(
            $"Cannot resolve object ID '{objectId}' - no ObjectFactory provided to CallLog.");
    }
    
    var obj = _objectFactory.GetRegisteredObject<object>(objectId);
    if (obj == null)
    {
        throw new ParrotCallMismatchException(
            $"Object with ID '{objectId}' not found in ObjectFactory registry.");
    }
    
    return obj;
}
```

### Step 4: Update Parrot.Create Method
```csharp
public static T Create<T>(CallLog callLog, string emoji = "🦜", ObjectFactory? objectFactory = null) where T : class
{
    var stub = ParrotStub<T>.Create(callLog);
    
    // Ensure CallLog has ObjectFactory reference
    if (objectFactory != null && callLog._objectFactory == null)
    {
        // Update callLog to use objectFactory
    }
    
    var callLogger = new CallLogger(callLog.SpecBook, emoji, objectFactory);
    return callLogger.Wrap<T>(stub, emoji, objectFactory);
}
```

### Step 5: Update ConvertReturnValue for Object Types
```csharp
private object? ConvertReturnValue(object? value, Type targetType)
{
    if (value == null) { /* existing logic */ }
    
    // NEW: Handle resolved objects
    if (value is object obj && !IsPrimitiveType(obj))
    {
        if (targetType.IsAssignableFrom(obj.GetType()))
            return obj;
            
        throw new ParrotTypeConversionException(
            $"Resolved object of type {obj.GetType().Name} cannot be assigned to expected type {targetType.Name}.");
    }
    
    // Existing conversion logic...
}

private bool IsPrimitiveType(object obj)
{
    var type = obj.GetType();
    return type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(decimal);
}
```

---

## API Design Changes

### New Exception
```csharp
public class ParrotUnknownObjectException : ParrotException
```

### Updated CallLog Constructors
```csharp
public CallLog(string? verifiedContent = null, ObjectFactory? objectFactory = null)
```

### Updated Static Factory Methods
```csharp
public static CallLog FromFile(string filePath, ObjectFactory? objectFactory = null)
public static CallLog FromVerifiedFile(ObjectFactory? objectFactory = null, ...)
// etc.
```

### Updated Parrot.Create
```csharp
public static T Create<T>(CallLog callLog, string emoji = "🦜", ObjectFactory? objectFactory = null)
```

---

## Format Examples

### Verified File With Object IDs
```
🦜 ProcessUser:
  🔸 user: <id:testUser>
  🔸 service: <id:emailSvc>
  🔸 timeout: 30
  🔹 Returns: <id:result>

🦜 SendEmail:
  🔸 recipient: "test@example.com"
  🔸 sender: <id:emailSvc>
  🔹 Returns: true
```

### Error Cases
```
🦜 ProcessUser:
  🔸 user: <unknown>           # Throws ParrotUnknownObjectException
  🔸 service: <id:missing>     # Throws ParrotCallMismatchException
  🔹 Returns: true
```

---

## Integration Points

### With ObjectFactory
- CallLog needs ObjectFactory reference to resolve IDs
- Parrot.Create should accept ObjectFactory parameter
- Registry lookups must be thread-safe

### With CallLogger
- Ensure consistent ObjectFactory instance across components
- CallLogger → CallLog → Parrot pipeline must share registry

### Error Handling Strategy
- `<unknown>` → `ParrotUnknownObjectException` (immediate failure)
- Invalid ID → `ParrotCallMismatchException` (ID not found)
- Type mismatch → `ParrotTypeConversionException` (wrong type)

---

## Test Verification Strategy

1. **Write failing tests first** - verify current parsing behavior
2. **Implement object ID parsing** incrementally
3. **Test error scenarios** thoroughly
4. **Verify type conversion** works correctly
5. **Integration testing** with real ObjectFactory and CallLogger

## Expected Test Failures
Before implementation:
- No `<id:...>` pattern recognition
- No `<unknown>` handling
- Missing ObjectFactory integration
- New exception types don't exist

## Success Criteria
- All Phase 3 tests pass
- Existing Parrot tests still pass
- Object ID resolution works correctly
- Error handling is comprehensive
- Clean integration with ObjectFactory
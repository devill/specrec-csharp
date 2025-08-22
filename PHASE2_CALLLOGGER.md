# Phase 2: CallLogger Modifications

## Overview
Modify CallLogger's `FormatValue` method to output `<id:string_id>` for registered objects and `<unknown>` for unregistered objects, replacing verbose object serialization.

## Current State Analysis
- `CallLogger.FormatValue()` at line 485 handles object serialization
- Current logic: null → "null", collections → formatted arrays/dictionaries, primitives → string representation
- No object registry integration exists
- CallLogger needs access to ObjectFactory's registry

## Goals
1. Integrate CallLogger with ObjectFactory's object registry
2. Format registered objects as `<id:string_id>`
3. Format unregistered objects as `<unknown>`
4. Preserve existing formatting for primitives, strings, collections
5. Maintain backward compatibility for non-object values

---

## Tests to Implement First (TDD)

### Test File: `CallLoggerIdFormattingTests.cs`

#### 1. Basic Object ID Formatting Tests
```csharp
[Fact]
public async Task FormatValue_WithRegisteredObject_ShouldReturnIdFormat()
[Fact]
public async Task FormatValue_WithUnregisteredObject_ShouldReturnUnknown()
[Fact]
public async Task FormatValue_WithMultipleRegisteredObjects_ShouldUseCorrectIds()
```

#### 2. Integration with Wrapped Objects Tests  
```csharp
[Fact]
public async Task Wrap_WithRegisteredService_ShouldLogIdInArguments()
[Fact]
public async Task Wrap_WithRegisteredService_ShouldLogIdInReturnValue()
[Fact]
public async Task Wrap_WithUnregisteredService_ShouldLogUnknownInArguments()
[Fact]
public async Task Wrap_WithMixedRegisteredAndPrimitiveArgs_ShouldFormatCorrectly()
```

#### 3. Preserve Existing Behavior Tests
```csharp
[Fact]
public async Task FormatValue_WithPrimitives_ShouldKeepExistingBehavior()
[Fact]
public async Task FormatValue_WithCollections_ShouldKeepExistingBehavior()
[Fact]
public async Task FormatValue_WithNullValues_ShouldKeepExistingBehavior()
[Fact]
public async Task FormatValue_WithStrings_ShouldKeepExistingBehavior()
```

#### 4. ObjectFactory Integration Tests
```csharp
[Fact]
public async Task CallLogger_WithCustomObjectFactory_ShouldUseProvidedRegistry()
[Fact]
public async Task CallLogger_WithGlobalObjectFactory_ShouldUseGlobalRegistry()
[Fact]
public async Task CallLogger_WithNoObjectFactory_ShouldFormatAsUnknown()
```

#### 5. Edge Cases Tests
```csharp
[Fact]
public async Task FormatValue_WithObjectThatBecomesUnregistered_ShouldHandleGracefully()
[Fact]
public async Task FormatValue_WithObjectFactoryChanges_ShouldReflectCurrentState()
[Fact]
public async Task FormatValue_WithNestedObjectsInCollections_ShouldFormatEachCorrectly()
```

---

## Implementation Plan

### Step 1: Add ObjectFactory Reference to CallLogger
```csharp
private readonly ObjectFactory? _objectFactory;

// Add new constructor overload
public CallLogger(StringBuilder? specbook = null, string emoji = "", ObjectFactory? objectFactory = null)

// Update Wrap method to pass ObjectFactory
public T Wrap<T>(T target, string emoji = "🔧", ObjectFactory? objectFactory = null) where T : class
```

### Step 2: Modify FormatValue Method
```csharp
private string FormatValue(object? value)
{
    if (value == null) return "null";

    // NEW: Check if object is registered first
    if (_objectFactory != null && IsComplexObject(value))
    {
        var registeredId = _objectFactory.GetRegisteredId(value);
        if (registeredId != null)
            return $"<id:{registeredId}>";
        else
            return "<unknown>";
    }

    // Existing logic for primitives, collections, etc.
    // ... rest of existing FormatValue implementation
}

private bool IsComplexObject(object value)
{
    // Returns true for objects that should be tracked by ID
    // Returns false for primitives, strings, collections that should use existing formatting
}
```

### Step 3: Update CallLoggerProxy Creation
```csharp
// In CallLoggerProxy<T>.Create method
public static T Create(T target, CallLogger logger, string emoji, ObjectFactory? objectFactory = null)
{
    // Pass ObjectFactory reference to proxy
}
```

### Step 4: Integration Points
```csharp
// Update all CallLogger creation points to optionally pass ObjectFactory
// Ensure global ObjectFactory instance can be used as fallback
```

---

## API Design Changes

### Constructor Overload
```csharp
public CallLogger(StringBuilder? specbook = null, string emoji = "", ObjectFactory? objectFactory = null)
```

### Wrap Method Overload  
```csharp
public T Wrap<T>(T target, string emoji = "🔧", ObjectFactory? objectFactory = null) where T : class
```

### Object Detection Logic
- **Complex Objects**: Custom classes, interfaces, non-primitive types
- **Preserve Existing**: strings, numbers, booleans, DateTime, collections, null
- **Collections with Objects**: Recursively format each element

---

## Format Examples

### Before (Current)
```
🔧 ProcessUser:
  🔸 user: SpecRec.Tests.UserData { Id = 123, Name = "John" }
  🔸 service: SpecRec.Tests.EmailService { Host = "smtp.example.com" }
  🔹 Returns: true
```

### After (With Object IDs)
```
🔧 ProcessUser:
  🔸 user: <id:testUser>
  🔸 service: <id:emailSvc>
  🔹 Returns: true
```

### With Unknown Objects
```
🔧 ProcessUser:
  🔸 user: <unknown>
  🔸 age: 25
  🔸 service: <id:emailSvc>
  🔹 Returns: true
```

---

## Test Verification Strategy

1. **Write failing tests first** - verify current FormatValue behavior
2. **Implement ObjectFactory integration** incrementally
3. **Test each format scenario** - registered, unregistered, mixed
4. **Verify existing functionality** still works for primitives
5. **Integration testing** with real ObjectFactory instances

## Expected Test Failures
Before implementation:
- Missing ObjectFactory parameter in constructor
- FormatValue not recognizing registered objects
- No `<id:...>` or `<unknown>` output

## Success Criteria
- All Phase 2 tests pass
- Existing CallLogger tests still pass  
- Clean integration with ObjectFactory
- Proper object vs primitive detection
- Readable specification output
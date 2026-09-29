---
title: NoTrimAttribute
category: API Reference
order: 8
description: Attribute that keeps a string property out of the trimming applied on write.
apiRef: NoTrimAttribute
---

## Overview

`ReadWriteRepository` trims leading and trailing whitespace from an entity's string properties before `Add`, `AddRange` and `Update`. `[NoTrim]` keeps a property out of that — for values whose whitespace carries meaning or has to round-trip unchanged, such as natural keys held outside the id, hashes, signatures or preformatted text.

The entity's key never needs it: `Id`, and whichever member is stored as `_id`, is not trimmed in the first place.

## Usage

```csharp
public class Document : IEntity<string>
{
    [BsonId]
    public string Id { get; set; }

    public string Title { get; set; }       // trimmed

    [NoTrim]
    public string Signature { get; set; }   // stored exactly as set
}
```

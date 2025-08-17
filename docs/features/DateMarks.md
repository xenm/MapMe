# DateMark CRUD & Duplicate Prevention

## Overview
Create, read, update, and delete DateMarks with safeguards against duplicates.

## User-facing capabilities
- Add DateMark from map or address search
- Edit DateMark fields (see DateMarkEdit)
- Delete DateMark
- Prevent duplicate entries (same place/user)

## Architecture
- Models: `DateMark`, `GeoPoint`
- Services: User profile service manages DateMarks collection
- Storage: localStorage (dev) with repository pattern compatibility

## Duplicate prevention
- Normalization of place identifiers
- Case-insensitive comparisons for categories/tags
- Optional spatial proximity threshold

## Testing
- Unit tests for business rules and normalization
- Integration tests for end-to-end CRUD

## Future enhancements
- Merge duplicates UI
- Conflict resolution

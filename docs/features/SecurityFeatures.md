# Security & Privacy Features

## Overview
Foundational privacy and security practices for the app.

## Current safeguards
- Edit permissions: only owner can edit their DateMarks (UI and service checks)
- Input validation and trimming for comma-separated fields
- System.Text.Json for safe serialization

## Planned/Recommended
- Authentication/authorization integration
- Rate limiting and request validation
- Secrets management for external APIs
- Content sanitization for rich text
- Audit logging for sensitive operations

## Testing
- Unit tests for validation
- Integration tests for authorization once auth is enabled

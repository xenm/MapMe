# Profiles & Activity Stats

## Overview
User profiles with editable details, lists of DateMarks, and activity/statistics views.

## User-facing capabilities
- View your profile and other users' profiles
- Edit profile details
- See lists of your DateMarks with quick actions (edit, delete)
- View activity statistics (counts, trends)

## Architecture
- Client pages: `MapMe.Client/Pages/User.razor`, `MapMe.Client/Pages/Profile.razor`
- Service: `MapMe.Client/Services/UserProfileService.cs`
- Storage: Local storage (dev), repository pattern for server

## Data model
- `UserProfile`: Username, DisplayName, Bio, AvatarUrl, Stats
- `ActivityStats`: DateMark counts, recent activity

## Key flows
- Load profile → fetch profile + stats → render
- Edit profile → validate → save via service → refresh
- DateMark list → perform actions (edit via modal, delete)

## Testing
- Unit tests for `UserProfileService` (parsing, validation, persistence)
- Integration tests for profile endpoints (if enabled)

## Future enhancements
- Followers/following
- Rich stats (heatmaps, timelines)
- Media gallery

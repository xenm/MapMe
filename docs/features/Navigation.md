# Navigation Flows

## Overview
Clean, predictable navigation between primary app surfaces.

## Routes
- `/` — Landing/home
- `/map` — Map experience
- `/profile` — Current user profile
- `/user/{username}` — Other user's profile
- `/chat` — Conversations hub (optional `?to=username` to open/ensure conversation)

## Patterns
- Pass context via query (e.g., `edit` for DateMark edit intent)
- Use state containers/services for cross-page state

## Testing
- Integration tests cover key flows (map → edit, user → chat)
- Manual testing: deep links, refresh behavior

## Future enhancements
- Back/forward state preservation
- Remember last map viewport

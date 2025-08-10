# Testing Strategy

Scopes
- Unit: C# services and helpers (if added)
- Integration: server endpoints like /config/maps
- UI/manual: map rendering, overlays, popovers, lightbox, info windows

Manual test checklist
- Map loads with API key and centers correctly
- Mock marks render via MapMe.debugRenderMockMarks()
- Grouping works: same placeId consolidates; proximity clusters within ~25m
- Marker overlay shows base place image, user chips, +N counter
- Hover label shows place title (or correct fallback)
- Clicking overlay opens info window with:
  - Place photos strip (scrollable)
  - Per-user sections with message list and user photos (scrollable)
  - Clicking images opens lightbox and navigates
- Hover/click on user name shows popover with profile details and recent photos
- Popover action buttons navigate correctly

Automated tests (suggested)
- Add minimal server integration tests for /config/maps
- Consider Playwright/E2E for UI flows if needed later

Performance checks
- Avoid excessive re-renders; ensure listeners are cleaned up
- Check image loading and caching behavior

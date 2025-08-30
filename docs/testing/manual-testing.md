# Manual Testing Guide

## Manual Test Checklist

### Map Functionality
- Map loads with API key and centers correctly
- Mock marks render via `MapMe.debugRenderMockMarks()`
- Grouping works: same placeId consolidates; proximity clusters within ~25m
- Marker overlay shows base place image, user chips, +N counter
- Hover label shows place title (or correct fallback)

### Interactive Features
- Clicking overlay opens info window with:
  - Place photos strip (scrollable)
  - Per-user sections with message list and user photos (scrollable)
  - Clicking images opens lightbox and navigates
- Hover/click on user name shows popover with profile details and recent photos
- Popover action buttons navigate correctly

### Performance Checks
- Avoid excessive re-renders; ensure listeners are cleaned up
- Check image loading and caching behavior
- Monitor memory usage during extended map interactions

### Authentication Testing
- Login page displays correctly without CSS conflicts
- User registration flow works end-to-end
- JWT token authentication persists across page refreshes
- Google OAuth integration (requires proper Client ID configuration)
- Session management and logout functionality

### Profile Management Testing
- Profile page editing capabilities (display name, bio, categories)
- Photo management (add/remove photos)
- Date Mark management (create, edit, delete)
- Activity statistics display correctly
- "View on Map" navigation from profile works

### Chat Functionality Testing
- Start new conversations from user profiles
- Send and receive messages
- Conversation list updates with unread counts
- Message history persistence
- Chat navigation and UI responsiveness

### Date Mark Testing
- Create Date Marks from Google Places
- Edit existing Date Marks (rating, notes, categories)
- Delete Date Marks
- Duplicate prevention when creating at same location
- Google Maps links work in popups and lists

### Cross-Browser Testing
- Test in Chrome, Firefox, Safari, Edge
- Mobile responsiveness on iOS/Android
- Touch interactions on mobile devices
- Performance on different screen sizes

### Error Handling Testing
- Network connectivity issues
- Invalid API responses
- Missing Google Maps API key
- Database connection failures
- Authentication token expiration

---

**Related Documentation:**
- [Testing Overview](README.md)
- [Testing Strategy](strategy.md)
- [Unit Testing](unit-testing.md)
- [Integration Testing](integration-testing.md)

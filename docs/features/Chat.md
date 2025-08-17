# Chat and Conversations

## Overview
A full chat system enabling private conversations between users, integrated across backend APIs, repositories, client services, and Blazor UI.

## User-facing capabilities
- Start a chat from a user profile ("Start Chat")
- View conversation list with unread counts
- Send/receive text messages (supports images/locations/DateMark shares in UI model)
- Mark messages as read, archive conversations, delete messages

## Architecture
- Backend: ASP.NET Core minimal APIs (.NET 10)
- Storage: In-memory repositories (production-ready pattern for Cosmos DB)
- Client: Blazor WebAssembly + `ChatService`
- Serialization: `System.Text.Json`

### Data models (server + client)
- Conversation: Id, Participants, LastMessageAt, Archived
- ChatMessage: Id, ConversationId, SenderUserId, Content, SentAt, ReadAt, MessageType

### Repositories
- `IConversationRepository` with in-memory implementation
- `IChatMessageRepository` with in-memory implementation

### API endpoints
- POST `/api/chat/messages` — send a message
- GET `/api/chat/conversations` — list conversations (by current user)
- GET `/api/chat/conversations/{id}/messages` — list messages
- POST `/api/chat/messages/read` — mark messages read
- POST `/api/chat/conversations/archive` — archive/unarchive
- DELETE `/api/chat/messages/{id}` — delete message

Authentication placeholder: current user simulated via `X-User-Id` header for tests/dev.

## Client integration
- `MapMe.Client/Services/ChatService.cs` handles API calls + local caching
- UI: `MapMe.Client/Pages/Chat.razor` with responsive layout
  - Conversation list + unread badges
  - Message pane
  - Input box with keyboard send

## Key flows
- Start chat from user page → navigates to Chat with target → creates/loads conversation
- Read status updates on opening a conversation
- Archive/hide conversations via actions

## Testing
- Integration tests fully cover endpoints (12/12 passing)
- Repository tests cover CRUD and state (12/12 passing)
- Client service unit tests cover basics

## Future enhancements
- Real auth integration
- SignalR real-time updates
- Persistent storage (Cosmos DB)
- Push notifications

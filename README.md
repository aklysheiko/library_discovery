# Library Discovery

A full-stack book discovery application that intelligently matches messy book queries to exact books in the Open Library.

## What it does

- **Smart Query Parsing**: Extracts title, author, year, and keywords from messy queries (e.g., "lord of rings tolkien 1954")
- **Comprehensive Search**: Searches Open Library for matching books
- **Intelligent Ranking**: Scores matches based on title (60pts), author (30pts), year (5pts), and popularity (3pts)
- **Deduplication**: Removes duplicate search results and keeps the best version
- **Explanation Generation**: Explains why each book matched the query

## Architecture

```
┌─────────────────────────────────────────────────────┐
│          React + Vite + Tailwind (Frontend)         │
│              http://localhost:5173                  │
└──────────────────────┬──────────────────────────────┘
                       │ HTTP/HTTPS
┌──────────────────────▼──────────────────────────────┐
│  .NET 8 API (Backend)                               │
│  http://localhost:5248                              │
│                                                     │
│  ┌─────────────────────────────────────────────┐   │
│  │ BooksController (/api/books/match)          │   │
│  └────────────────────┬────────────────────────┘   │
│                       │                             │
│  ┌────────────────────▼────────────────────────┐   │
│  │ BookMatchService (Orchestrator)             │   │
│  │ ┌──────────────────────────────────────┐   │   │
│  │ │ 1. Parse Query                       │   │   │
│  │ │ 2. Search Open Library               │   │   │
│  │ │ 3. Enrich & Deduplicate              │   │   │
│  │ │ 4. Rank by Match Score               │   │   │
│  │ │ 5. Generate Explanations             │   │   │
│  │ └──────────────────────────────────────┘   │   │
│  └─────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
                       │
                   Open Library
                   Public API
```

## Project Structure

```
library_discovery/
├── LibraryDiscovery/              # Main .NET 8 API project
│   ├── Controllers/               # HTTP endpoints
│   ├── Program.cs                 # Dependency injection
│   └── appsettings.json           # Configuration
├── src/
│   ├── LibraryDiscovery.Domain/   # Entities and value objects
│   ├── LibraryDiscovery.Application/  # Interfaces and DTOs
│   └── LibraryDiscovery.Infrastructure/  # Service implementations
├── tests/
│   └── LibraryDiscovery.UnitTests/    # 130 unit tests
└── frontend/                      # React + Vite application
    ├── src/
    │   ├── components/            # React components
    │   ├── api.ts                 # API client
    │   ├── App.tsx                # Main app
    │   └── index.css              # Tailwind styles
    └── index.html                 # HTML template
```

> **Note on Dockerfile**: The `Dockerfile` builds the backend API for cloud deployment only (used by the CI/CD workflow to push an image to GitHub Container Registry). You do **not** need Docker to run the app locally — use `start.sh` / `start.bat` or the manual steps below instead.

## Quick Start

### Option 1: Run Everything at Once (Recommended)

#### macOS/Linux:
```bash
chmod +x start.sh
./start.sh
```

#### Windows:
```cmd
start.bat
```

### Option 2: Run Separately

**Terminal 1 - Backend:**
```bash
dotnet run --project LibraryDiscovery
```

**Terminal 2 - Frontend:**
```bash
cd frontend
npm install
npm run dev
```

Then open: **http://localhost:5173**

## Usage

1. Enter a book query (e.g., "Harry Potter by Rowling", "Hobbit 1937", "Lord Rings Tolkien")
2. The system parses your query and searches Open Library
3. Results are ranked by relevance with explanations
4. Click "View on OpenLibrary" to see the full book page

## Testing

Run all tests:
```bash
dotnet test
```

Current: **130 unit tests** 

## Technology Stack

### Backend (.NET 8)
- **Language**: C# 12
- **Framework**: ASP.NET Core 8
- **Testing**: xUnit
- **API**: RESTful with Swagger/OpenAPI

### Frontend
- **UI**: React 18 + TypeScript
- **Build Tool**: Vite 5
- **Styling**: Tailwind CSS 3
- **HTTP Client**: Axios
- **Package Manager**: npm

## Features

**Smart Query Parsing**: Gemini AI-powered parsing with regex fallback (works without an API key)
**Open Library Integration**: Real-time book search
**Deterministic Scoring**: Clear, auditable match scoring
**Deduplication**: Automatic duplicate removal
**Result Enrichment**: Normalized titles and extracted authors
**Explanations**: Human-readable match explanations
**Responsive UI**: Mobile-friendly design
**No Authentication**: Public, open access

## API Endpoints

### POST /api/books/match
Match a book query to Open Library books.

**Request:**
```json
{
  "query": "Lord of the Rings by Tolkien"
}
```

**Response:**
```json
{
  "query": "Lord of the Rings by Tolkien",
  "parsedQuery": { ... },
  "matches": [
    {
      "title": "The Lord of the Rings",
      "primaryAuthors": ["J.R.R. Tolkien"],
      "score": 95,
      "matchTier": "ExactTitleAndPrimaryAuthor",
      "explanation": "...",
      "coverUrl": "...",
      "openLibraryWorkId": "..."
    }
  ]
}
```

## Learning Resources

- See [SETUP.md](SETUP.md) for detailed backend setup
- See [RUN_LOCALLY.txt](RUN_LOCALLY.txt) for full local setup instructions including frontend
- See [frontend/README.md](frontend/README.md) for detailed frontend setup
- API documentation available at: http://localhost:5248/swagger

## Notes

- Gemini API key optional — app falls back to regex-based parsing if not configured
- No authentication needed
- All data comes from Open Library (public domain API)
- CORS enabled for local development
- Frontend configured for localhost:5173
- Backend configured for localhost:5248

## Known Issues

- Text rendering issue in some docs (display workaround included)

## Future Improvements

- Authentication: No auth is currently in place — add user accounts to support personalisation and access control
- Rate limiting: The API has no rate limiting; add throttling to prevent abuse and avoid hammering the Open Library upstream API
- Pagination: Results are currently capped at 5 with no way to request more — expose page/offset parameters
- Implement caching for frequently searched books
- Add user preferences and saved searches
- Support for multiple languages
- Advanced filtering and sorting options
- User ratings and reviews

---

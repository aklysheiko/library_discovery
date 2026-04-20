# Library Discovery Backend Setup

## Running the Backend Locally

### Prerequisites
- .NET 8 SDK
- Visual Studio Code or Visual Studio

### Configuration

The backend runs on `http://localhost:5248` by default (HTTP profile). The frontend Vite proxy is configured to forward `/api` requests to this URL.

### Running the Server

From the project root:

```bash
dotnet run --project LibraryDiscovery
```

Or build first:

```bash
dotnet build
dotnet run --project LibraryDiscovery
```

The API will be available at:
- `http://localhost:5248/api/books/match` - Book matching endpoint
- `http://localhost:5248/swagger` - Swagger UI documentation

### Testing

Run all tests:

```bash
dotnet test
```

Run specific test project:

```bash
dotnet test tests/LibraryDiscovery.UnitTests/
```

## API Endpoints

### POST /api/books/match

Matches a book query to Open Library books.

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
  "parsedQuery": {
    "titleCandidates": ["Lord of the Rings"],
    "authorCandidates": ["Tolkien"],
    "keywords": [],
    "yearHint": null
  },
  "matches": [
    {
      "title": "The Lord of the Rings",
      "primaryAuthors": ["J.R.R. Tolkien"],
      "firstPublishYear": 1954,
      "openLibraryWorkId": "OL123456W",
      "coverUrl": "https://covers.openlibrary.org/b/id/123-M.jpg",
      "score": 95,
      "matchTier": "ExactTitleAndPrimaryAuthor",
      "explanation": "Exact title match: 'The Lord of the Rings' Primary author surname match: J.R.R. Tolkien"
    }
  ],
  "message": null
}
```

## Architecture

### Main Components

1. **BooksController** - HTTP API endpoint
2. **BookMatchService** - Main orchestrator
   - Query Parsing
   - Book Search (Open Library)
   - Result Enrichment & Deduplication
   - Candidate Ranking
   - Explanation Generation
3. **Domain Models** - Entity and value objects
4. **Infrastructure** - External service implementations

### Service Flow

```
User Query → Parse → Search → Enrich → Rank → Explain → Response
```

## Dependencies

Key NuGet packages:
- Microsoft.AspNetCore.OpenApi
- Swashbuckle.AspNetCore (Swagger)

Supports optional Gemini AI query parsing (set `GEMINI_API_KEY` via dotnet user-secrets or environment variable). Falls back to regex-based parsing if no key is configured.

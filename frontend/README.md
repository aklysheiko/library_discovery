# Library Discovery - Frontend

A React + Vite + Tailwind CSS application for searching and discovering books.

## Development

### Prerequisites
- Node.js 18+ 
- npm

### Installation

```bash
npm install
```

### Running Locally

Start the development server:

```bash
npm run dev
```

The app will be available at `http://localhost:5173`

**Important:** Make sure the backend API is running on `http://localhost:5248` (the Vite proxy forwards `/api` calls there automatically).

### Building

To create a production build:

```bash
npm run build
```

## Project Structure

```
frontend/
├── src/
│   ├── components/
│   │   └── BookCard.tsx     # Individual book result card
│   ├── api.ts              # API client and types
│   ├── App.tsx             # Main app component
│   ├── main.tsx            # Entry point
│   └── index.css           # Tailwind CSS imports
├── index.html              # HTML template
├── vite.config.ts          # Vite configuration
├── tsconfig.json           # TypeScript configuration
├── tailwind.config.ts      # Tailwind CSS configuration
└── postcss.config.js       # PostCSS configuration
```

## Features

- Smart book search with query parsing
- Results ranked by match tier (colour-coded badge per result)
- Clean, responsive UI with Tailwind CSS
- Direct links to Open Library
- Mobile-friendly design

## API Integration

The frontend communicates with the backend API:

- **Endpoint:** `POST /api/books/match`
- **Request:** `{ query: string }`
- **Response:** `BookMatchResponse` with matches and parsed query info

The backend should be running locally for development.

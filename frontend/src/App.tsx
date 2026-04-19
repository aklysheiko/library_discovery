import { useState } from 'react'
import { matchBooks, BookMatchResponse } from './api'
import { BookCard } from './components/BookCard'

function App() {
  const [query, setQuery] = useState('')
  const [response, setResponse] = useState<BookMatchResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!query.trim()) {
      setError('Please enter a search query')
      return
    }

    setLoading(true)
    setError('')
    setResponse(null)

    try {
      const result = await matchBooks(query)
      setResponse(result)

      if (result.message) {
        setError(result.message)
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
      {/* Header */}
      <header className="bg-white shadow">
        <div className="max-w-6xl mx-auto px-4 py-8">
          <h1 className="text-4xl font-bold text-gray-900 mb-2">
            🔍 Library Discovery
          </h1>
          <p className="text-gray-600">
            Find books in Open Library using smart matching
          </p>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-6xl mx-auto px-4 py-8">
        {/* Search Box */}
        <div className="bg-white rounded-lg shadow-lg p-8 mb-8">
          <form onSubmit={handleSearch} className="space-y-4">
            <div>
              <label htmlFor="query" className="block text-sm font-medium text-gray-700 mb-2">
                Search for a book
              </label>
              <div className="flex gap-2">
                <input
                  id="query"
                  type="text"
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                  placeholder="e.g., 'Tolkien Hobbit 1937' or 'Harry Potter first book'"
                  className="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  disabled={loading}
                />
                <button
                  type="submit"
                  disabled={loading}
                  className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:bg-gray-400 transition-colors font-medium"
                >
                  {loading ? 'Searching...' : 'Search'}
                </button>
              </div>
            </div>

            {error && (
              <div className="p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
                {error}
              </div>
            )}
          </form>
        </div>

        {/* Search Info */}
        {response && (
          <div className="mb-8">
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
              <p className="text-gray-700">
                <span className="font-semibold">Query:</span> {response.query}
              </p>
              {response.parsedQuery && (
                <div className="mt-3 text-sm text-gray-600">
                  <p>
                    <span className="font-semibold">Parsed as:</span>{' '}
                    {response.parsedQuery.titleCandidates.length > 0 &&
                      `Title: ${response.parsedQuery.titleCandidates.join(', ')}`}
                    {response.parsedQuery.authorCandidates.length > 0 &&
                      ` | Author: ${response.parsedQuery.authorCandidates.join(', ')}`}
                    {response.parsedQuery.yearHint &&
                      ` | Year: ${response.parsedQuery.yearHint}`}
                  </p>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Results */}
        {response && response.matches.length > 0 && (
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="text-2xl font-bold text-gray-900">
                Results ({response.matches.length})
              </h2>
            </div>
            <div className="space-y-4">
              {response.matches.map((match, idx) => (
                <BookCard key={idx} match={match} />
              ))}
            </div>
          </div>
        )}

        {/* No Results */}
        {response && response.matches.length === 0 && !error && (
          <div className="text-center py-12">
            <p className="text-xl text-gray-600 mb-2">No books found</p>
            <p className="text-gray-500">Try searching with different keywords</p>
          </div>
        )}

        {/* Instructions */}
        {!response && (
          <div className="bg-white rounded-lg shadow p-8 text-center">
            <h2 className="text-2xl font-bold text-gray-900 mb-4">How it works</h2>
            <div className="grid md:grid-cols-3 gap-6 text-left">
              <div>
                <div className="text-3xl font-bold text-blue-600 mb-2">1</div>
                <h3 className="font-semibold text-gray-900 mb-2">Enter your query</h3>
                <p className="text-gray-600 text-sm">
                  Type a messy book query like "Lord of the Rings by Tolkien"
                </p>
              </div>
              <div>
                <div className="text-3xl font-bold text-blue-600 mb-2">2</div>
                <h3 className="font-semibold text-gray-900 mb-2">Smart parsing</h3>
                <p className="text-gray-600 text-sm">
                  Our system extracts title, author, year, and keywords
                </p>
              </div>
              <div>
                <div className="text-3xl font-bold text-blue-600 mb-2">3</div>
                <h3 className="font-semibold text-gray-900 mb-2">Get results</h3>
                <p className="text-gray-600 text-sm">
                  Find matching books with match scores and explanations
                </p>
              </div>
            </div>
          </div>
        )}
      </main>

      {/* Footer */}
      <footer className="bg-gray-800 text-gray-300 mt-16 py-8">
        <div className="max-w-6xl mx-auto px-4 text-center">
          <p>
            Powered by{' '}
            <a href="https://openlibrary.org" className="text-blue-400 hover:text-blue-300">
              Open Library API
            </a>
          </p>
          <p className="text-sm mt-2">
            © 2024 Library Discovery - Find books faster
          </p>
        </div>
      </footer>
    </div>
  )
}

export default App

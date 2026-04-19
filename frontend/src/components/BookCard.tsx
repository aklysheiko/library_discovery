import { BookMatch } from '../api'

interface BookCardProps {
  match: BookMatch
}

export const BookCard: React.FC<BookCardProps> = ({ match }) => {
  return (
    <div className="bg-white rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow">
      <div className="flex gap-4">
        {match.coverUrl && (
          <img
            src={match.coverUrl}
            alt={match.title}
            className="w-24 h-32 object-cover rounded"
          />
        )}
        <div className="flex-1">
          <h3 className="text-xl font-bold text-gray-900 mb-2">{match.title}</h3>

          {match.primaryAuthors.length > 0 && (
            <p className="text-gray-600 mb-2">
              by {match.primaryAuthors.join(', ')}
            </p>
          )}

          {match.firstPublishYear > 0 && (
            <p className="text-gray-500 text-sm mb-3">
              First published: {match.firstPublishYear}
            </p>
          )}

          <p className="text-gray-700 text-sm mb-3">{match.explanation}</p>

          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <div className="w-full bg-gray-200 rounded-full h-2" style={{ width: '80px' }}>
                <div
                  className="bg-blue-500 h-2 rounded-full"
                  style={{ width: `${Math.min(match.score, 100)}px` }}
                />
              </div>
              <span className="text-sm font-semibold text-gray-700">
                {match.score}/100
              </span>
            </div>
            {match.openLibraryWorkId && (
              <a
                href={`https://openlibrary.org/works/${match.openLibraryWorkId}`}
                target="_blank"
                rel="noopener noreferrer"
                className="text-blue-600 hover:text-blue-800 text-sm font-medium"
              >
                View on OpenLibrary
              </a>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}

import axios from 'axios'

// Prefer env-configured base URL; default to '/api' so Vite proxy can handle it
const API_BASE_URL = (import.meta as any).env?.VITE_API_BASE_URL || '/api'

export interface BookMatch {
  title: string
  primaryAuthors: string[]
  firstPublishYear: number
  openLibraryWorkId?: string
  coverUrl?: string
  score: number
  explanation: string
}

export interface BookMatchResponse {
  query: string
  parsedQuery?: {
    titleCandidates: string[]
    authorCandidates: string[]
    keywords: string[]
    yearHint?: number
  }
  matches: BookMatch[]
  message?: string
}

export const matchBooks = async (query: string): Promise<BookMatchResponse> => {
  try {
    const response = await axios.post<BookMatchResponse>(
      `${API_BASE_URL}/books/match`,
      { query },
      {
        validateStatus: () => true // Don't throw on any status
      }
    )
    return response.data
  } catch (error) {
    throw new Error(`Failed to match books: ${error instanceof Error ? error.message : 'Unknown error'}`)
  }
}

export type IndexedDocument = {
  id: number
  name: string
  sourcePath: string
  chunkCount: number
}

export type Citation = {
  documentName: string
  heading?: string | null
  chunkIndex: number
}

export type ChatResponse = {
  answer: string
  citations: Citation[]
}

export type IngestionResult = {
  documentCount: number
  chunkCount: number
  failedFiles: string[]
}

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  })

  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    throw new Error(problem?.detail ?? problem?.title ?? `Request failed: ${response.status}`)
  }

  return response.json() as Promise<T>
}

export function getDocuments() {
  return request<IndexedDocument[]>('/api/documents')
}

export function runIngestion() {
  return request<IngestionResult>('/api/ingestion/run', { method: 'POST' })
}

export function sendChat(question: string) {
  return request<ChatResponse>('/api/chat', {
    method: 'POST',
    body: JSON.stringify({ question }),
  })
}

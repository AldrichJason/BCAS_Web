/** Mirrors the DTOs returned by the BCAS Web API. */

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface CurrentUser {
  id: number
  email: string
  fullName: string
  role: 'Administrator' | 'Editor' | string
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
  user: CurrentUser
}

export interface Announcement {
  id: number
  title: string
  slug: string
  summary: string
  content: string
  imageUrl: string | null
  category: string
  isPublished: boolean
  publishedAt: string | null
  authorName: string
  createdAt: string
  updatedAt: string
}

export interface AnnouncementInput {
  title: string
  summary: string
  content: string
  imageUrl: string | null
  category: string
  isPublished: boolean
}

export interface Program {
  id: number
  code: string
  name: string
  slug: string
  description: string
  degreeLevel: string
  durationYears: number
  isActive: boolean
  displayOrder: number
}

export interface ContentPage {
  id: number
  slug: string
  title: string
  content: string
  isPublished: boolean
  updatedAt: string
}

export interface Faq {
  id: number
  question: string
  answer: string
  keywords: string
  category: string
  isActive: boolean
}

export interface FaqInput {
  question: string
  answer: string
  keywords: string
  category: string
  isActive: boolean
}

export interface ChatReply {
  sessionId: string
  reply: string
  matchedFaqId: number | null
  suggestions: string[]
}

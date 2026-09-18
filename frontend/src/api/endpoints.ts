import { request, toQueryString } from './client'
import type {
  Announcement,
  AnnouncementInput,
  AuthResponse,
  ChatReply,
  ContentPage,
  CurrentUser,
  Faq,
  FaqInput,
  PagedResult,
  Program,
} from './types'

export const authApi = {
  login: (email: string, password: string) =>
    request<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: { email, password },
      skipAuthRetry: true,
    }),

  refresh: (refreshToken: string) =>
    request<AuthResponse>('/api/auth/refresh', {
      method: 'POST',
      body: { refreshToken },
      skipAuthRetry: true,
    }),

  logout: (refreshToken: string) =>
    request<void>('/api/auth/logout', { method: 'POST', body: { refreshToken } }),

  me: () => request<CurrentUser>('/api/auth/me'),
}

export const announcementsApi = {
  list: (
    params: { page?: number; pageSize?: number; search?: string; category?: string; includeDrafts?: boolean } = {},
    signal?: AbortSignal,
  ) => request<PagedResult<Announcement>>(`/api/announcements${toQueryString(params)}`, { signal }),

  categories: (signal?: AbortSignal) => request<string[]>('/api/announcements/categories', { signal }),

  bySlug: (slug: string, signal?: AbortSignal) =>
    request<Announcement>(`/api/announcements/${encodeURIComponent(slug)}`, { signal }),

  create: (input: AnnouncementInput) =>
    request<Announcement>('/api/announcements', { method: 'POST', body: input }),

  update: (id: number, input: AnnouncementInput) =>
    request<Announcement>(`/api/announcements/${id}`, { method: 'PUT', body: input }),

  remove: (id: number) => request<void>(`/api/announcements/${id}`, { method: 'DELETE' }),
}

export const programsApi = {
  list: (includeInactive = false, signal?: AbortSignal) =>
    request<Program[]>(`/api/programs${toQueryString({ includeInactive })}`, { signal }),

  bySlug: (slug: string, signal?: AbortSignal) =>
    request<Program>(`/api/programs/${encodeURIComponent(slug)}`, { signal }),
}

export const pagesApi = {
  list: (includeDrafts = false, signal?: AbortSignal) =>
    request<ContentPage[]>(`/api/pages${toQueryString({ includeDrafts })}`, { signal }),

  bySlug: (slug: string, signal?: AbortSignal) =>
    request<ContentPage>(`/api/pages/${encodeURIComponent(slug)}`, { signal }),
}

export const chatbotApi = {
  ask: (message: string, sessionId: string | null) =>
    request<ChatReply>('/api/chatbot/ask', {
      method: 'POST',
      body: { message, sessionId },
    }),

  faqs: (includeInactive = false, signal?: AbortSignal) =>
    request<Faq[]>(`/api/chatbot/faqs${toQueryString({ includeInactive })}`, { signal }),

  createFaq: (input: FaqInput) => request<Faq>('/api/chatbot/faqs', { method: 'POST', body: input }),

  updateFaq: (id: number, input: FaqInput) =>
    request<Faq>(`/api/chatbot/faqs/${id}`, { method: 'PUT', body: input }),

  removeFaq: (id: number) => request<void>(`/api/chatbot/faqs/${id}`, { method: 'DELETE' }),
}

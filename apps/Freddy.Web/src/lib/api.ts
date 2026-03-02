import ky from 'ky';
import type { ConversationDto, MessageDto, TokenResponse } from '../types/chat';

const TOKEN_KEY = 'freddy_token';

function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

function setToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token);
}

const api = ky.create({
  prefixUrl: '/api/v1',
  timeout: 300_000,
  hooks: {
    beforeRequest: [
      (request) => {
        const token = getToken();
        if (token) {
          request.headers.set('Authorization', `Bearer ${token}`);
        }
        console.log(`[Freddy API] ${request.method} ${request.url}`);
      },
    ],
    afterResponse: [
      (_request, _options, response) => {
        console.log(`[Freddy API] Response: ${response.status} ${response.url}`);
      },
    ],
    beforeError: [
      (error) => {
        console.error(`[Freddy API] Error: ${error.response?.status} ${error.response?.url}`, error.message);
        return error;
      },
    ],
  },
});

export async function ensureAuthToken(): Promise<void> {
  if (getToken()) return;

  const response = await api.post('auth/dev-token').json<TokenResponse>();
  setToken(response.token);
}

export async function getConversations(): Promise<ConversationDto[]> {
  await ensureAuthToken();
  return api.get('chat/conversations').json<ConversationDto[]>();
}

export async function createConversation(title?: string): Promise<ConversationDto> {
  await ensureAuthToken();
  return api.post('chat/conversations', { json: { title } }).json<ConversationDto>();
}

export async function getMessages(conversationId: string): Promise<MessageDto[]> {
  await ensureAuthToken();
  return api.get(`chat/conversations/${conversationId}/messages`).json<MessageDto[]>();
}

export async function sendMessage(conversationId: string, content: string): Promise<MessageDto> {
  await ensureAuthToken();
  return api.post(`chat/conversations/${conversationId}/messages`, { json: { content } }).json<MessageDto>();
}

export async function deleteConversation(conversationId: string): Promise<void> {
  await ensureAuthToken();
  await api.delete(`chat/conversations/${conversationId}`);
}

export interface MessageDto {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  createdAt: string;
}

export interface ConversationDto {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string;
}

export interface TokenResponse {
  token: string;
}

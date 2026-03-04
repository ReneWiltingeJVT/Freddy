export interface AttachmentDto {
  name: string;
  url: string;
  description?: string;
}

export interface MessageDto {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  createdAt: string;
  attachments?: AttachmentDto[];
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

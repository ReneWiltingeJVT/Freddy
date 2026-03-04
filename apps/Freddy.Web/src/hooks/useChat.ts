import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import type { MessageDto } from '../types/chat';
import {
  getConversations,
  createConversation,
  getMessages,
  sendMessage,
  deleteConversation,
} from '../lib/api';

export function useConversations() {
  return useQuery({
    queryKey: ['conversations'],
    queryFn: getConversations,
  });
}

export function useMessages(conversationId: string | undefined) {
  return useQuery({
    queryKey: ['messages', conversationId],
    queryFn: () => getMessages(conversationId!),
    enabled: !!conversationId,
  });
}

export function useCreateConversation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (title?: string) => createConversation(title),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['conversations'] });
    },
  });
}

export function useSendMessage(conversationId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (content: string) => sendMessage(conversationId, content),
    onMutate: async (content: string) => {
      // Cancel in-flight queries so they don't overwrite our optimistic update
      await queryClient.cancelQueries({ queryKey: ['messages', conversationId] });

      const previous = queryClient.getQueryData<MessageDto[]>(['messages', conversationId]);

      // Optimistically add the user message
      const optimisticMessage: MessageDto = {
        id: `optimistic-${Date.now()}`,
        role: 'user',
        content,
        createdAt: new Date().toISOString(),
      };

      queryClient.setQueryData<MessageDto[]>(
        ['messages', conversationId],
        (old = []) => [...old, optimisticMessage],
      );

      return { previous };
    },
    onError: (_error, _content, context) => {
      // Roll back on error
      if (context?.previous) {
        queryClient.setQueryData(['messages', conversationId], context.previous);
      }
    },
    onSuccess: () => {
      // Refetch to get the real user message + assistant response
      queryClient.invalidateQueries({ queryKey: ['messages', conversationId] });
      queryClient.invalidateQueries({ queryKey: ['conversations'] });
    },
  });
}

export function useDeleteConversation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (conversationId: string) => deleteConversation(conversationId),
    onSuccess: (_data, conversationId) => {
      queryClient.invalidateQueries({ queryKey: ['conversations'] });
      queryClient.removeQueries({ queryKey: ['messages', conversationId] });
    },
  });
}

import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { ConversationList } from './ConversationList';
import { ChatMessageList } from './ChatMessageList';
import { ChatInput } from './ChatInput';
import { useMessages, useSendMessage, useCreateConversation, useDeleteConversation } from '../../hooks/useChat';
import type { MessageDto } from '../../types/chat';
import { sendMessage as apiSendMessage } from '../../lib/api';

export function ChatPage() {
  const { conversationId } = useParams<{ conversationId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { data: messages = [], isLoading: messagesLoading } = useMessages(conversationId);
  const sendMessageMutation = useSendMessage(conversationId ?? '');
  const createConversationMutation = useCreateConversation();
  const deleteConversationMutation = useDeleteConversation();
  const [isSending, setIsSending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isBusy = sendMessageMutation.isPending || isSending;

  async function handleSendMessage(content: string) {
    setError(null);

    if (!conversationId) {
      // === New conversation flow ===
      setIsSending(true);
      try {
        console.log('[Freddy] Creating new conversation...');
        const conversation = await createConversationMutation.mutateAsync(undefined);
        console.log('[Freddy] Conversation created:', conversation.id);

        // Pre-seed the messages cache with the optimistic user message
        // BEFORE navigating, so useMessages finds it immediately.
        const optimisticMessage: MessageDto = {
          id: `optimistic-${Date.now()}`,
          role: 'user',
          content,
          createdAt: new Date().toISOString(),
        };
        queryClient.setQueryData<MessageDto[]>(
          ['messages', conversation.id],
          [optimisticMessage],
        );

        // Navigate (component stays mounted because of optional route param)
        navigate(`/chat/${conversation.id}`, { replace: true });

        // Cancel any background refetch that useMessages might trigger,
        // so it doesn't overwrite our optimistic data with [].
        await queryClient.cancelQueries({ queryKey: ['messages', conversation.id] });

        // Send the actual message to the backend (slow — Ollama call)
        console.log('[Freddy] Sending message to backend...');
        const response = await apiSendMessage(conversation.id, content);
        console.log('[Freddy] Backend response received:', response);

        // Refetch to get all real messages (user + assistant)
        await queryClient.invalidateQueries({ queryKey: ['messages', conversation.id] });
        await queryClient.invalidateQueries({ queryKey: ['conversations'] });
      } catch (err) {
        console.error('[Freddy] Error in new conversation flow:', err);
        setError('Er ging iets mis bij het versturen van je bericht. Probeer het opnieuw.');
      } finally {
        setIsSending(false);
      }
      return;
    }

    // === Existing conversation flow ===
    try {
      console.log('[Freddy] Sending message to conversation:', conversationId);
      await sendMessageMutation.mutateAsync(content);
      console.log('[Freddy] Message sent successfully');
    } catch (err) {
      console.error('[Freddy] Error sending message:', err);
      setError('Er ging iets mis bij het versturen van je bericht. Probeer het opnieuw.');
    }
  }

  function handleNewConversation() {
    navigate('/chat');
  }

  function handleSelectConversation(id: string) {
    navigate(`/chat/${id}`);
  }

  async function handleDeleteConversation(id: string) {
    await deleteConversationMutation.mutateAsync(id);
    if (conversationId === id) {
      navigate('/chat');
    }
  }

  function dismissError() {
    setError(null);
  }

  return (
    <div className="flex h-screen bg-gray-50">
      {/* Sidebar */}
      <aside className="w-72 border-r border-gray-200 bg-white flex flex-col">
        <div className="p-4 border-b border-gray-200">
          <h1 className="text-xl font-bold text-blue-600">Freddy</h1>
          <p className="text-sm text-gray-500">Zorgassistent</p>
        </div>

        <div className="p-3">
          <button
            onClick={handleNewConversation}
            className="w-full rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 transition-colors"
          >
            + Nieuw gesprek
          </button>
        </div>

        <ConversationList
          selectedId={conversationId}
          onSelect={handleSelectConversation}
          onDelete={handleDeleteConversation}
        />
      </aside>

      {/* Main chat area */}
      <main className="flex-1 flex flex-col">
        {error && (
          <div className="bg-red-50 border-b border-red-200 px-4 py-3 flex items-center justify-between">
            <p className="text-sm text-red-700">{error}</p>
            <button onClick={dismissError} className="text-red-500 hover:text-red-700 text-sm font-medium">
              Sluiten
            </button>
          </div>
        )}

        {conversationId ? (
          <>
            <ChatMessageList
              messages={messages}
              isLoading={messagesLoading}
              isPending={isBusy}
            />
            <ChatInput
              onSend={handleSendMessage}
              disabled={isBusy}
            />
          </>
        ) : (
          <div className="flex-1 flex items-center justify-center">
            <div className="text-center">
              <h2 className="text-2xl font-semibold text-gray-700 mb-2">
                Welkom bij Freddy
              </h2>
              <p className="text-gray-500 mb-6">
                Stel een vraag over protocollen, procedures of werkwijzen.
              </p>
              <ChatInput
                onSend={handleSendMessage}
                disabled={isBusy}
              />
            </div>
          </div>
        )}
      </main>
    </div>
  );
}

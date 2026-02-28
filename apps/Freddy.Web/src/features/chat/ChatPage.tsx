import { useParams, useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { ConversationList } from './ConversationList';
import { ChatMessageList } from './ChatMessageList';
import { ChatInput } from './ChatInput';
import { useMessages, useSendMessage, useCreateConversation } from '../../hooks/useChat';
import { sendMessage as apiSendMessage } from '../../lib/api';

export function ChatPage() {
  const { conversationId } = useParams<{ conversationId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { data: messages = [], isLoading: messagesLoading } = useMessages(conversationId);
  const sendMessageMutation = useSendMessage(conversationId ?? '');
  const createConversationMutation = useCreateConversation();

  async function handleSendMessage(content: string) {
    if (!conversationId) {
      const conversation = await createConversationMutation.mutateAsync(undefined);
      navigate(`/chat/${conversation.id}`);
      await apiSendMessage(conversation.id, content);
      await queryClient.invalidateQueries({ queryKey: ['messages', conversation.id] });
      return;
    }
    await sendMessageMutation.mutateAsync(content);
  }

  function handleNewConversation() {
    navigate('/chat');
  }

  function handleSelectConversation(id: string) {
    navigate(`/chat/${id}`);
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
        />
      </aside>

      {/* Main chat area */}
      <main className="flex-1 flex flex-col">
        {conversationId ? (
          <>
            <ChatMessageList
              messages={messages}
              isLoading={messagesLoading}
              isPending={sendMessageMutation.isPending}
            />
            <ChatInput
              onSend={handleSendMessage}
              disabled={sendMessageMutation.isPending}
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
                disabled={createConversationMutation.isPending}
              />
            </div>
          </div>
        )}
      </main>
    </div>
  );
}

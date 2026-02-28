import { useConversations } from '../../hooks/useChat';

interface ConversationListProps {
  selectedId: string | undefined;
  onSelect: (id: string) => void;
}

export function ConversationList({ selectedId, onSelect }: ConversationListProps) {
  const { data: conversations = [], isLoading } = useConversations();

  if (isLoading) {
    return (
      <div className="p-4 text-sm text-gray-400">Laden...</div>
    );
  }

  if (conversations.length === 0) {
    return (
      <div className="p-4 text-sm text-gray-400">Geen gesprekken</div>
    );
  }

  return (
    <nav className="flex-1 overflow-y-auto p-2 space-y-1">
      {conversations.map((conversation) => (
        <button
          key={conversation.id}
          onClick={() => onSelect(conversation.id)}
          className={`w-full text-left rounded-lg px-3 py-2 text-sm transition-colors ${
            selectedId === conversation.id
              ? 'bg-blue-50 text-blue-700 font-medium'
              : 'text-gray-700 hover:bg-gray-100'
          }`}
        >
          <p className="truncate">{conversation.title}</p>
          <p className="text-xs text-gray-400 mt-0.5">
            {new Date(conversation.updatedAt).toLocaleDateString('nl-NL')}
          </p>
        </button>
      ))}
    </nav>
  );
}

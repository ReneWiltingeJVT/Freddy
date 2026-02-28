import { useConversations } from '../../hooks/useChat';

interface ConversationListProps {
  selectedId: string | undefined;
  onSelect: (id: string) => void;
  onDelete: (id: string) => void;
}

export function ConversationList({ selectedId, onSelect, onDelete }: ConversationListProps) {
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
        <div
          key={conversation.id}
          className={`group flex items-center rounded-lg transition-colors ${
            selectedId === conversation.id
              ? 'bg-blue-50 text-blue-700'
              : 'text-gray-700 hover:bg-gray-100'
          }`}
        >
          <button
            onClick={() => onSelect(conversation.id)}
            className="flex-1 text-left px-3 py-2 min-w-0"
          >
            <p className={`truncate text-sm ${selectedId === conversation.id ? 'font-medium' : ''}`}>
              {conversation.title}
            </p>
            <p className="text-xs text-gray-400 mt-0.5">
              {new Date(conversation.updatedAt).toLocaleDateString('nl-NL')}
            </p>
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              onDelete(conversation.id);
            }}
            title="Gesprek verwijderen"
            className="opacity-0 group-hover:opacity-100 mr-2 p-1 rounded text-gray-400 hover:text-red-500 hover:bg-red-50 transition-all"
          >
            <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
              <path fillRule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clipRule="evenodd" />
            </svg>
          </button>
        </div>
      ))}
    </nav>
  );
}

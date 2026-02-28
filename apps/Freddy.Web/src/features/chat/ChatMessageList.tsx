import { useEffect, useRef } from 'react';
import type { MessageDto } from '../../types/chat';

interface ChatMessageListProps {
  messages: MessageDto[];
  isLoading: boolean;
  isPending: boolean;
}

export function ChatMessageList({ messages, isLoading, isPending }: ChatMessageListProps) {
  const endRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    endRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  if (isLoading) {
    return (
      <div className="flex-1 flex items-center justify-center">
        <p className="text-gray-400">Berichten laden...</p>
      </div>
    );
  }

  return (
    <div className="flex-1 overflow-y-auto p-6 space-y-4">
      {messages.map((message) => (
        <div
          key={message.id}
          className={`flex ${message.role === 'user' ? 'justify-end' : 'justify-start'}`}
        >
          <div
            className={`max-w-[70%] rounded-2xl px-4 py-3 text-sm leading-relaxed ${
              message.role === 'user'
                ? 'bg-blue-600 text-white'
                : 'bg-white text-gray-800 border border-gray-200 shadow-sm'
            }`}
          >
            {message.content}
          </div>
        </div>
      ))}

      {isPending && (
        <div className="flex justify-start">
          <div className="bg-white border border-gray-200 shadow-sm rounded-2xl px-4 py-3 text-sm text-gray-400">
            <span className="animate-pulse">Freddy denkt na...</span>
          </div>
        </div>
      )}

      <div ref={endRef} />
    </div>
  );
}

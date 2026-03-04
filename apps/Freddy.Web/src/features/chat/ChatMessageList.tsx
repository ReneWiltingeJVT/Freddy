import { useEffect, useRef } from 'react';
import ReactMarkdown from 'react-markdown';
import type { MessageDto, AttachmentDto } from '../../types/chat';

function DownloadButton({ attachment }: { attachment: AttachmentDto }) {
  return (
    <a
      href={attachment.url}
      target="_blank"
      rel="noopener noreferrer"
      download
      className="flex items-center gap-3 rounded-xl border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-800 hover:bg-blue-100 active:bg-blue-200 transition-colors cursor-pointer no-underline"
    >
      <span className="flex-shrink-0 text-blue-500">
        <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
          <path fillRule="evenodd" d="M3 17a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm3.293-7.707a1 1 0 011.414 0L9 10.586V3a1 1 0 112 0v7.586l1.293-1.293a1 1 0 111.414 1.414l-3 3a1 1 0 01-1.414 0l-3-3a1 1 0 010-1.414z" clipRule="evenodd" />
        </svg>
      </span>
      <div className="min-w-0">
        <p className="font-semibold truncate">{attachment.name}</p>
        {attachment.description && (
          <p className="text-xs text-blue-500 truncate">{attachment.description}</p>
        )}
      </div>
    </a>
  );
}

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
            className={`max-w-[75%] rounded-2xl px-4 py-3 text-sm leading-relaxed ${
              message.role === 'user'
                ? 'bg-blue-600 text-white'
                : 'bg-white text-gray-800 border border-gray-200 shadow-sm'
            }`}
          >
            {message.role === 'user' ? (
              message.content
            ) : (
              <>
                <div className="prose prose-sm max-w-none prose-headings:font-semibold prose-headings:text-gray-800 prose-h2:text-base prose-h3:text-sm prose-p:text-gray-700 prose-strong:text-gray-900 prose-ul:text-gray-700 prose-ol:text-gray-700 prose-li:my-0.5 prose-a:text-blue-600 prose-a:font-medium hover:prose-a:underline prose-em:text-gray-500">
                  <ReactMarkdown
                    components={{
                      a: ({ href, children }) => (
                        <a href={href} target="_blank" rel="noopener noreferrer">
                          {children}
                        </a>
                      ),
                    }}
                  >
                    {message.content}
                  </ReactMarkdown>
                </div>
                {message.attachments && message.attachments.length > 0 && (
                  <div className="mt-3 space-y-2">
                    {message.attachments.map((att, i) => (
                      <DownloadButton key={i} attachment={att} />
                    ))}
                  </div>
                )}
              </>
            )}
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

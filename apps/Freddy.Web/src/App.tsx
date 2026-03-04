import { Routes, Route, Navigate } from 'react-router-dom';
import { ChatPage } from './features/chat/ChatPage';

export function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/chat" replace />} />
      <Route path="/chat/:conversationId?" element={<ChatPage />} />
    </Routes>
  );
}

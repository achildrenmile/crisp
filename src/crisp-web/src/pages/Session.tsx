import { useParams, Link, useNavigate } from 'react-router-dom';
import { useRef, useEffect, useState } from 'react';
import { useSession } from '../hooks/useSession';
import {
  ChatMessage,
  ChatInput,
  PlanView,
  ActionIndicator,
} from '../components';
import { ChatSidebar } from '../components/ChatSidebar';

export function Session() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const navigate = useNavigate();
  const {
    session,
    messages,
    isLoading,
    error,
    currentPlan,
    deliveryResult,
    currentAction,
    sendMessage,
    approvePlan,
    createSession,
  } = useSession(sessionId);

  const messagesEndRef = useRef<HTMLDivElement>(null);
  const [sidebarRefresh, setSidebarRefresh] = useState(0);

  // Auto-scroll to bottom on new messages or action updates
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isLoading, currentAction]);

  // Refresh sidebar when delivery result changes
  useEffect(() => {
    if (deliveryResult) {
      setSidebarRefresh((prev) => prev + 1);
    }
  }, [deliveryResult]);

  const handleApprove = () => {
    approvePlan(true);
  };

  const handleReject = () => {
    approvePlan(false, 'Please revise the plan');
  };

  const handleNewSession = async () => {
    try {
      const newSessionId = await createSession();
      navigate(`/session/${newSessionId}`);
    } catch {
      // Error handled by hook
    }
  };

  const isInputDisabled =
    isLoading ||
    session?.status === 'executing' ||
    session?.status === 'awaiting_approval';

  if (!sessionId) {
    return (
      <div className="error-container">
        <h2>Session Not Found</h2>
        <p>The session you're looking for doesn't exist or has expired.</p>
        <Link to="/" className="btn-primary">
          Start New Session
        </Link>
      </div>
    );
  }

  return (
    <div className="chat-layout">
      <ChatSidebar currentSessionId={sessionId} onNewSession={handleNewSession} refreshTrigger={sidebarRefresh} />

      <div className="chat-container">
        <div className="chat-header">
          <div className="session-info">
            {session && (
              <span
                className={`session-status status-${session.status.toLowerCase().replace('_', '')}`}
              >
                {session.status.replace('_', ' ')}
              </span>
            )}
          </div>
        </div>

        <div className="chat-messages">
          {messages.length === 0 && (
            <div className="welcome-message">
              <p>Hi! I'm CRISP, your AI assistant for creating code repositories.</p>
              <p>Tell me about the project you'd like to create. For example:</p>
              <ul>
                <li>"I need a .NET 8 Web API project called customer-api"</li>
                <li>"Create a FastAPI project for a todo application"</li>
                <li>"Set up a new microservice with Docker support"</li>
              </ul>
            </div>
          )}

          {messages.map((message) => (
            <ChatMessage key={message.id} message={message} />
          ))}

          {currentPlan && (
            <PlanView
              plan={currentPlan}
              isAwaitingApproval={session?.status === 'awaiting_approval'}
              onApprove={handleApprove}
              onReject={handleReject}
            />
          )}

          {(isLoading || currentAction) && (
            <ActionIndicator
              actionKey={currentAction?.actionKey}
              description={currentAction?.description}
            />
          )}

          {error && (
            <div className="message message-assistant error">
              <div className="message-content">
                <p>Error: {error}</p>
              </div>
            </div>
          )}

          <div ref={messagesEndRef} />
        </div>

        <ChatInput onSend={sendMessage} disabled={isInputDisabled} />
      </div>
    </div>
  );
}

import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { History, Plus, CheckCircle, XCircle, Loader2, Clock, MessageSquare } from 'lucide-react';
import { getSessionHistory, SessionHistoryItem } from '../services/api';
import './ChatSidebar.css';

interface ChatSidebarProps {
  currentSessionId?: string;
  onNewSession: () => void;
}

export function ChatSidebar({ currentSessionId, onNewSession }: ChatSidebarProps) {
  const navigate = useNavigate();
  const [sessions, setSessions] = useState<SessionHistoryItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    loadHistory();
  }, []);

  const loadHistory = async () => {
    try {
      setIsLoading(true);
      const history = await getSessionHistory();
      setSessions(history);
    } catch {
      // Silently fail - sidebar is not critical
    } finally {
      setIsLoading(false);
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status.toLowerCase()) {
      case 'completed':
        return <CheckCircle size={12} className="status-icon success" />;
      case 'failed':
        return <XCircle size={12} className="status-icon error" />;
      case 'executing':
      case 'planning':
        return <Loader2 size={12} className="status-icon spin" />;
      default:
        return <Clock size={12} className="status-icon" />;
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m`;
    if (diffHours < 24) return `${diffHours}h`;
    if (diffDays < 7) return `${diffDays}d`;
    return date.toLocaleDateString();
  };

  const getDisplayName = (session: SessionHistoryItem) => {
    // If we have a project name, show it in full
    if (session.projectName) {
      return session.projectName;
    }
    // Otherwise show first message truncated
    if (session.firstMessage) {
      return session.firstMessage.length > 30
        ? session.firstMessage.substring(0, 30) + '...'
        : session.firstMessage;
    }
    return 'New Project';
  };

  return (
    <div className="chat-sidebar">
      <div className="sidebar-header">
        <History size={16} />
        <span>History</span>
      </div>

      <button className="new-session-btn" onClick={onNewSession}>
        <Plus size={16} />
        New Project
      </button>

      <div className="sidebar-sessions">
        {isLoading ? (
          <div className="sidebar-loading">
            <Loader2 size={16} className="spin" />
          </div>
        ) : sessions.length === 0 ? (
          <div className="sidebar-empty">
            <MessageSquare size={20} />
            <span>No projects yet</span>
          </div>
        ) : (
          sessions.map((session) => (
            <div
              key={session.sessionId}
              className={`sidebar-session ${session.sessionId === currentSessionId ? 'active' : ''}`}
              onClick={() => navigate(`/session/${session.sessionId}`)}
            >
              <div className="session-name">
                {getDisplayName(session)}
              </div>
              <div className="session-meta">
                {getStatusIcon(session.status)}
                <span className="session-time">{formatDate(session.lastActivityAt)}</span>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
}

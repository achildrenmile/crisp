import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { History, Clock, CheckCircle, XCircle, Loader2, MessageSquare, ExternalLink, Code } from 'lucide-react';
import { getSessionHistory, SessionHistoryItem } from '../services/api';
import './ProjectHistory.css';

export function ProjectHistory() {
  const navigate = useNavigate();
  const [sessions, setSessions] = useState<SessionHistoryItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadHistory();
  }, []);

  const loadHistory = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const history = await getSessionHistory();
      setSessions(history);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load history');
    } finally {
      setIsLoading(false);
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status.toLowerCase()) {
      case 'completed':
        return <CheckCircle size={16} className="status-icon success" />;
      case 'failed':
        return <XCircle size={16} className="status-icon error" />;
      case 'executing':
      case 'planning':
        return <Loader2 size={16} className="status-icon spin" />;
      default:
        return <Clock size={16} className="status-icon" />;
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
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return date.toLocaleDateString();
  };

  const truncateMessage = (message: string | null, maxLength: number = 80) => {
    if (!message) return 'No description';
    if (message.length <= maxLength) return message;
    return message.substring(0, maxLength) + '...';
  };

  if (isLoading) {
    return (
      <div className="project-history">
        <div className="history-header">
          <History size={20} />
          <h3>Recent Projects</h3>
        </div>
        <div className="history-loading">
          <Loader2 size={24} className="spin" />
          <span>Loading history...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="project-history">
        <div className="history-header">
          <History size={20} />
          <h3>Recent Projects</h3>
        </div>
        <div className="history-error">
          <XCircle size={20} />
          <span>{error}</span>
        </div>
      </div>
    );
  }

  if (sessions.length === 0) {
    return (
      <div className="project-history">
        <div className="history-header">
          <History size={20} />
          <h3>Recent Projects</h3>
        </div>
        <div className="history-empty">
          <MessageSquare size={32} />
          <p>No projects yet</p>
          <span>Start a new project to see it here</span>
        </div>
      </div>
    );
  }

  return (
    <div className="project-history">
      <div className="history-header">
        <History size={20} />
        <h3>Recent Projects</h3>
      </div>
      <div className="history-list">
        {sessions.map((session) => (
          <div
            key={session.sessionId}
            className="history-item"
            onClick={() => navigate(`/session/${session.sessionId}`)}
          >
            <div className="history-item-header">
              <span className="project-name">
                {session.projectName || 'Untitled Project'}
              </span>
              <span className="session-time">{formatDate(session.lastActivityAt)}</span>
            </div>
            <div className="history-item-body">
              <span className="first-message">
                {truncateMessage(session.firstMessage)}
              </span>
            </div>
            <div className="history-item-footer">
              <span className={`status-badge status-${session.status.toLowerCase()}`}>
                {getStatusIcon(session.status)}
                {session.status}
              </span>
              <div className="history-item-actions">
                {session.vsCodeWebUrl && (
                  <a
                    href={session.vsCodeWebUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="vscode-link"
                    onClick={(e) => e.stopPropagation()}
                    title="Open in browser (vscode.dev)"
                  >
                    <Code size={14} />
                    Browser
                  </a>
                )}
                {session.vsCodeCloneUrl && (
                  <a
                    href={session.vsCodeCloneUrl}
                    className="clone-link"
                    onClick={(e) => e.stopPropagation()}
                    title="Clone to VS Code desktop"
                  >
                    <Code size={14} />
                    Clone
                  </a>
                )}
                {session.repositoryUrl && (
                  <a
                    href={session.repositoryUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="repo-link"
                    onClick={(e) => e.stopPropagation()}
                  >
                    <ExternalLink size={14} />
                    Repo
                  </a>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

interface ActionIndicatorProps {
  actionKey?: string;
  description?: string;
}

// Map action keys to icons
const actionIcons: Record<string, string> = {
  thinking: '🤔',
  preparing: '🔧',
  creating_plan: '📋',
  scaffolding: '📁',
  enterprise: '🏢',
  creating_repo: '🚀',
  git_init: '📦',
  pushing: '⬆️',
  pipeline: '⚙️',
  build_wait: '⏳',
  finalizing: '✨',
};

export function ActionIndicator({ actionKey, description }: ActionIndicatorProps) {
  const icon = actionKey ? actionIcons[actionKey] || '⏳' : '⏳';
  const text = description || 'Processing...';

  return (
    <div className="message message-assistant loading">
      <div className="action-indicator">
        <span className="action-icon">{icon}</span>
        <span className="action-text">{text}</span>
        <span className="action-spinner"></span>
      </div>
    </div>
  );
}

// Keep TypingIndicator for backwards compatibility
export function TypingIndicator() {
  return <ActionIndicator />;
}

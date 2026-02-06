import type { DeliveryCard as DeliveryCardType } from '../types';

interface DeliveryCardProps {
  card: DeliveryCardType;
}

export function DeliveryCard({ card }: DeliveryCardProps) {
  const getBuildStatusClass = () => {
    const status = card.buildStatus.toLowerCase();
    if (['success', 'succeeded', 'passing'].includes(status)) {
      return 'status-success';
    }
    if (['failed', 'failure'].includes(status)) {
      return 'status-failed';
    }
    return 'status-pending';
  };

  const getBuildStatusIcon = () => {
    const status = card.buildStatus.toLowerCase();
    if (['success', 'succeeded', 'passing'].includes(status)) {
      return '✅';
    }
    if (['failed', 'failure'].includes(status)) {
      return '❌';
    }
    return '⏳';
  };

  return (
    <div className="delivery-card">
      <div className="delivery-header">
        <span className="delivery-icon">🎉</span>
        <h3>Repository Ready!</h3>
      </div>

      <div className="delivery-content">
        <div className="delivery-item">
          <span className="label">Platform</span>
          <span className="value">{card.platform}</span>
        </div>

        <div className="delivery-item">
          <span className="label">Repository</span>
          <a
            href={card.repositoryUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="value link"
          >
            {card.repositoryUrl}
          </a>
        </div>

        <div className="delivery-item">
          <span className="label">Branch</span>
          <span className="value">{card.branch}</span>
        </div>

        {card.pipelineUrl && (
          <div className="delivery-item">
            <span className="label">CI/CD Pipeline</span>
            <a
              href={card.pipelineUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="value link"
            >
              View Pipeline
            </a>
          </div>
        )}

        <div className="delivery-item">
          <span className="label">Build Status</span>
          <span className={`value build-status ${getBuildStatusClass()}`}>
            {getBuildStatusIcon()} {card.buildStatus}
          </span>
        </div>
      </div>

      <div className="delivery-actions">
        <a
          href={card.vsCodeWebUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="btn-vscode"
          title="Open instantly in browser"
        >
          <span className="vscode-icon">🌐</span>
          Open in Browser
        </a>

        <a
          href={card.vsCodeCloneUrl}
          className="btn-vscode-clone"
          title="Clone and open in VS Code desktop"
        >
          <span className="vscode-icon">💻</span>
          Clone to Desktop
        </a>

        <a
          href={card.repositoryUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="btn-secondary"
        >
          View on {card.platform}
        </a>
      </div>
    </div>
  );
}

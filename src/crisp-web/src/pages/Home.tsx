import { useNavigate } from 'react-router-dom';
import { useState } from 'react';
import { MessageSquare, Rocket, ShieldCheck, FolderPlus, Loader2 } from 'lucide-react';
import { useSession } from '../hooks/useSession';

export function Home() {
  const navigate = useNavigate();
  const { createSession, isLoading, error } = useSession();
  const [isCreating, setIsCreating] = useState(false);

  const handleStartSession = async () => {
    setIsCreating(true);
    try {
      const sessionId = await createSession();
      navigate(`/session/${sessionId}`);
    } catch {
      // Error is handled by the hook
    } finally {
      setIsCreating(false);
    }
  };

  return (
    <div className="home-container">
      <div className="hero">
        <h1>Welcome to CRISP</h1>
        <p className="hero-subtitle">
          Code Repo Initialization & Scaffolding Platform
        </p>

        <div className="features">
          <div className="feature">
            <span className="feature-icon">
              <MessageSquare size={32} />
            </span>
            <h3>Natural Language</h3>
            <p>Describe your project in plain English and let AI do the rest</p>
          </div>

          <div className="feature">
            <span className="feature-icon">
              <Rocket size={32} />
            </span>
            <h3>Full Stack Setup</h3>
            <p>Get a complete repo with CI/CD, tests, and documentation</p>
          </div>

          <div className="feature">
            <span className="feature-icon">
              <ShieldCheck size={32} />
            </span>
            <h3>Policy Compliant</h3>
            <p>All scaffolds follow your organization's coding standards</p>
          </div>
        </div>

        {error && <div className="error-message">{error}</div>}

        <button
          className="btn-primary btn-large"
          onClick={handleStartSession}
          disabled={isLoading || isCreating}
        >
          {isCreating ? (
            <>
              <Loader2 size={20} className="spin" />
              Creating Session...
            </>
          ) : (
            <>
              <FolderPlus size={20} />
              Start New Project
            </>
          )}
        </button>
      </div>
    </div>
  );
}

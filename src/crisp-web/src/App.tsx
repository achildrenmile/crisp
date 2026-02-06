import { BrowserRouter, Routes, Route, Link, useNavigate } from 'react-router-dom';
import { User, LogOut, Github } from 'lucide-react';
import { Home, Session } from './pages';
import { LoginPage } from './pages/LoginPage';
import { ProtectedRoute } from './components/ProtectedRoute';
import { isAuthenticated, logout, getStoredUser } from './services/auth';
import { VERSION } from './version';

function Header() {
  const navigate = useNavigate();
  const user = getStoredUser();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <header className="app-header">
      <div className="header-content">
        <Link to="/" className="logo">
          <span className="logo-text">CRISP</span>
        </Link>
        <span className="tagline">Code Repo Initialization & Scaffolding Platform</span>
        {isAuthenticated() && (
          <div className="user-menu">
            <User size={18} />
            <span className="user-name">{user?.name || 'User'}</span>
            <button onClick={handleLogout} className="logout-btn">
              <LogOut size={16} />
              Logout
            </button>
          </div>
        )}
      </div>
    </header>
  );
}

function Layout({ children }: { children: React.ReactNode }) {
  return (
    <div className="app-container">
      <Header />
      <main className="app-main">{children}</main>
      <footer className="app-footer">
        <p>
          CRISP - Code Repo Initialization & Scaffolding Platform
          <span className="footer-separator">|</span>
          <span className="footer-version">v{VERSION}</span>
          <span className="footer-separator">|</span>
          <a href="https://github.com/achildrenmile/crisp" target="_blank" rel="noopener noreferrer" className="footer-link">
            <Github size={14} />
            GitHub
          </a>
        </p>
      </footer>
    </div>
  );
}

function ProtectedLayout({ children }: { children: React.ReactNode }) {
  return (
    <ProtectedRoute>
      <Layout>{children}</Layout>
    </ProtectedRoute>
  );
}

export function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route
          path="/"
          element={
            <ProtectedLayout>
              <Home />
            </ProtectedLayout>
          }
        />
        <Route
          path="/session/:sessionId"
          element={
            <ProtectedLayout>
              <Session />
            </ProtectedLayout>
          }
        />
      </Routes>
    </BrowserRouter>
  );
}

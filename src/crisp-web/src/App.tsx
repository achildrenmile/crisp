import { BrowserRouter, Routes, Route, Link, useNavigate } from 'react-router-dom';
import { Home, Session } from './pages';
import { LoginPage } from './pages/LoginPage';
import { ProtectedRoute } from './components/ProtectedRoute';
import { isAuthenticated, logout, getStoredUser } from './services/auth';

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
            <span className="user-name">{user?.name || 'User'}</span>
            <button onClick={handleLogout} className="logout-btn">
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
        <p>CRISP - Powered by Claude AI</p>
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

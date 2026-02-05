import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import { Home, Session } from './pages';

function Layout({ children }: { children: React.ReactNode }) {
  return (
    <div className="app-container">
      <header className="app-header">
        <div className="header-content">
          <Link to="/" className="logo">
            <span className="logo-text">CRISP</span>
          </Link>
          <span className="tagline">Code Repo Initialization & Scaffolding Platform</span>
        </div>
      </header>

      <main className="app-main">{children}</main>

      <footer className="app-footer">
        <p>CRISP - Powered by Claude AI</p>
      </footer>
    </div>
  );
}

export function App() {
  return (
    <BrowserRouter>
      <Layout>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/session/:sessionId" element={<Session />} />
        </Routes>
      </Layout>
    </BrowserRouter>
  );
}

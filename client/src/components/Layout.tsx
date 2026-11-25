import { Link, Outlet } from 'react-router-dom';

export function Layout() {
  return (
    <>
      <a className="skip-link" href="#main-content">
        Skip to content
      </a>
      <header className="site-header">
        <nav className="site-nav" aria-label="Main navigation">
          <Link className="brand" to="/">
            Campus Lost &amp; Found
          </Link>
        </nav>
      </header>
      <main id="main-content" className="main-content">
        <Outlet />
      </main>
      <footer className="site-footer">
        <p>Privacy-first campus lost and found platform.</p>
      </footer>
    </>
  );
}

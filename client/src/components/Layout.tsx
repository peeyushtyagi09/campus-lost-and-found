import { Link, NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

export function Layout() {
  const { isAuthenticated, user, logout } = useAuth();

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
          <ul className="nav-links">
            <li>
              <NavLink className="nav-link" to="/" end>
                Public Board
              </NavLink>
            </li>
            {isAuthenticated ? (
              <>
                <li>
                  <NavLink className="nav-link" to="/report-lost">
                    Report Lost
                  </NavLink>
                </li>
                <li>
                  <NavLink className="nav-link" to="/report-found">
                    Report Found
                  </NavLink>
                </li>
                <li>
                  <NavLink className="nav-link" to="/my-reports">
                    My Reports
                  </NavLink>
                </li>
                <li>
                  <NavLink className="nav-link" to="/matches">
                    Possible Matches
                  </NavLink>
                </li>
                <li>
                  <NavLink className="nav-link" to="/my-claims">
                    My Claims
                  </NavLink>
                </li>
                {user?.role === 'Staff' ? (
                  <li>
                    <NavLink className="nav-link" to="/staff">
                      Staff Queue
                    </NavLink>
                  </li>
                ) : null}
                <li>
                  <span className="field-hint">Signed in as {user?.displayName}</span>
                </li>
                <li>
                  <button type="button" className="btn btn-secondary" onClick={logout}>
                    Sign Out
                  </button>
                </li>
              </>
            ) : (
              <>
                <li>
                  <NavLink className="nav-link" to="/login">
                    Sign In
                  </NavLink>
                </li>
                <li>
                  <NavLink className="nav-link" to="/register">
                    Register
                  </NavLink>
                </li>
              </>
            )}
          </ul>
        </nav>
      </header>
      <main id="main-content" className="main-content">
        <Outlet />
      </main>
      <footer className="site-footer">
        <p>
          Privacy-first campus lost and found platform. Contact details are never posted publicly.
        </p>
      </footer>
    </>
  );
}

import { Link } from 'react-router-dom';

export function NotFoundPage() {
  return (
    <section aria-labelledby="not-found-heading">
      <h1 id="not-found-heading">Page not found</h1>
      <p>
        Return to the <Link to="/">home page</Link>.
      </p>
    </section>
  );
}

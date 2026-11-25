import { screen } from '@testing-library/react';
import { renderAppAt } from './testUtils';

describe('App routing and shell', () => {
  it('renders public board on root with skip link', async () => {
    renderAppAt(['/']);

    expect(screen.getByRole('link', { name: /skip to content/i })).toBeInTheDocument();
    expect(
      await screen.findByRole('heading', { name: /public lost & found board/i })
    ).toBeInTheDocument();
  });

  it('redirects protected routes to login when unauthenticated', async () => {
    renderAppAt(['/report-lost']);

    expect(await screen.findByRole('heading', { name: /sign in/i })).toBeInTheDocument();
  });

  it('renders not found page for unknown routes', () => {
    renderAppAt(['/unknown-route']);

    expect(screen.getByRole('heading', { name: /page not found/i })).toBeInTheDocument();
  });
});

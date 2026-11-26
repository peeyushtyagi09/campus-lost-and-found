import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { server } from './mocks/server';
import { renderAppAt } from './testUtils';

function seedSession() {
  sessionStorage.setItem(
    'clf_session',
    JSON.stringify({
      accessToken: 'fake-jwt-token',
      expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      user: {
        id: 'user-1',
        displayName: 'Alice Student',
        email: 'alice@campus.edu',
        role: 'User',
      },
    })
  );
}

describe('MatchesPage', () => {
  it('renders public found item details without private fields and supports confirm', async () => {
    seedSession();
    let confirmed = false;

    server.use(
      http.get('/api/v1/matches/mine', () => {
        return HttpResponse.json([
          {
            id: 'match-1',
            lostReportId: 'lost-1',
            foundReportId: 'found-1',
            score: 85,
            explanation: 'Same category (+10); Same campus area (+30)',
            status: confirmed ? 'Confirmed' : 'Suggested',
            createdAt: '2026-11-23T10:00:00Z',
            foundItem: {
              id: 'found-1',
              title: 'Found Silver Watch',
              category: 'Other',
              publicDescription: 'Analog watch near desk',
              campusArea: 'Library',
              dateLost: '2026-11-23',
              photoUrl: null,
              status: 'Open',
              postedAt: '2026-11-23T10:00:00Z',
              heldAt: 'Main Security Desk',
              storageNotes: 'HIDDEN_LOCKER_99',
              finderContact: 'hidden@finder.edu',
              verificationDetails: 'HIDDEN_ENGRAVING',
            },
          },
        ]);
      }),
      http.post('/api/v1/matches/match-1/confirm', () => {
        confirmed = true;
        return HttpResponse.json({
          id: 'match-1',
          lostReportId: 'lost-1',
          foundReportId: 'found-1',
          score: 85,
          explanation: 'Same category (+10); Same campus area (+30)',
          status: 'Confirmed',
          createdAt: '2026-11-23T10:00:00Z',
          foundItem: {
            id: 'found-1',
            title: 'Found Silver Watch',
            category: 'Other',
            publicDescription: 'Analog watch near desk',
            campusArea: 'Library',
            dateLost: '2026-11-23',
            photoUrl: null,
            status: 'Open',
            postedAt: '2026-11-23T10:00:00Z',
            heldAt: 'Main Security Desk',
          },
        });
      })
    );

    const user = userEvent.setup();
    renderAppAt(['/matches']);

    expect(await screen.findByText(/Score: 85\/100/i)).toBeInTheDocument();
    expect(screen.getByText(/Found Silver Watch/i)).toBeInTheDocument();
    expect(screen.getByText(/Main Security Desk/i)).toBeInTheDocument();
    expect(screen.queryByText(/HIDDEN_LOCKER_99/)).not.toBeInTheDocument();
    expect(screen.queryByText(/hidden@finder.edu/)).not.toBeInTheDocument();
    expect(screen.queryByText(/HIDDEN_ENGRAVING/)).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /found silver watch/i }));
    expect(await screen.findByRole('dialog')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Close' }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /confirm match/i }));
    await waitFor(() => expect(confirmed).toBe(true));
  });
});

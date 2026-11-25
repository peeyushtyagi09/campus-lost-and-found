import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { server } from './mocks/server';
import { renderAppAt } from './testUtils';

function seedUserSession(role: 'User' | 'Staff') {
  sessionStorage.setItem(
    'clf_session',
    JSON.stringify({
      accessToken: 'fake-jwt-token',
      expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      user: {
        id: role === 'Staff' ? 'staff-1' : 'user-1',
        displayName: role === 'Staff' ? 'Desk Staff' : 'Alice Student',
        email: role === 'Staff' ? 'staff@campus.edu' : 'alice@campus.edu',
        role,
      },
    })
  );
}

describe('Claims, staff handover, and custody timeline', () => {
  it('submits a claim and renders custody timeline', async () => {
    seedUserSession('User');
    let submitted = false;

    server.use(
      http.post('/api/v1/claims', () => {
        submitted = true;
        return HttpResponse.json(
          {
            id: 'claim-1',
            foundReportId: 'found-1',
            lostReportId: null,
            status: 'Pending',
            verificationAnswers: 'Engraved KMP',
            reviewNotes: null,
            createdAt: '2026-11-24T10:00:00Z',
            foundItem: null,
          },
          { status: 201 }
        );
      }),
      http.get('/api/v1/claims/mine', () => {
        return HttpResponse.json([
          {
            id: 'claim-1',
            foundReportId: 'found-1',
            lostReportId: null,
            status: 'Pending',
            verificationAnswers: 'Engraved KMP',
            reviewNotes: null,
            createdAt: '2026-11-24T10:00:00Z',
            foundItem: {
              id: 'found-1',
              title: 'Found Silver Watch',
              category: 'Other',
              publicDescription: 'Analog watch',
              campusArea: 'Library',
              dateLost: '2026-11-24',
              photoUrl: null,
              status: 'Open',
              postedAt: '2026-11-24T09:00:00Z',
              heldAt: 'Main Security Desk',
            },
          },
        ]);
      })
    );

    const user = userEvent.setup();
    renderAppAt(['/claim/found-1']);

    await user.type(await screen.findByLabelText(/proof of ownership/i), 'Engraved initials KMP');
    await user.click(screen.getByRole('button', { name: /submit claim/i }));

    await waitFor(() => expect(submitted).toBe(true));
    expect(await screen.findByRole('heading', { name: /my claims/i })).toBeInTheDocument();
  });

  it('allows staff to approve a claim and complete supervised handover', async () => {
    seedUserSession('Staff');
    let claimStatus: 'Pending' | 'Approved' = 'Pending';
    let handoverDone = false;

    server.use(
      http.get('/api/v1/auth/me', () =>
        HttpResponse.json({
          id: 'staff-1',
          displayName: 'Desk Staff',
          email: 'staff@campus.edu',
          role: 'Staff',
        })
      ),
      http.get('/api/v1/claims/review', () => {
        return HttpResponse.json([
          {
            id: 'claim-9',
            foundReportId: 'found-9',
            lostReportId: null,
            status: claimStatus,
            verificationAnswers: 'Sticker PHY-101',
            foundVerificationDetails: 'Sticker PHY-101 on back',
            foundStorageNotes: 'Locker 4',
            reviewNotes: null,
            createdAt: '2026-11-24T10:00:00Z',
            foundItem: {
              id: 'found-9',
              title: 'Graphing Calculator',
              category: 'Other',
              publicDescription: 'Black calculator',
              campusArea: 'Science Building',
              dateLost: '2026-11-24',
              photoUrl: null,
              status: handoverDone ? 'Returned' : 'Open',
              postedAt: '2026-11-24T09:00:00Z',
              heldAt: 'Main Security Desk',
            },
          },
        ]);
      }),
      http.post('/api/v1/claims/claim-9/approve', () => {
        claimStatus = 'Approved';
        return HttpResponse.json({
          id: 'claim-9',
          foundReportId: 'found-9',
          lostReportId: null,
          status: 'Approved',
          verificationAnswers: 'Sticker PHY-101',
          foundVerificationDetails: 'Sticker PHY-101 on back',
          foundStorageNotes: 'Locker 4',
          reviewNotes: 'Approved',
          createdAt: '2026-11-24T10:00:00Z',
          foundItem: null,
        });
      }),
      http.post('/api/v1/handovers/claim-9/complete', () => {
        handoverDone = true;
        return HttpResponse.json(
          {
            id: 'handover-1',
            claimId: 'claim-9',
            foundReportId: 'found-9',
            witnessConfirmed: true,
            idChecked: true,
            completedAt: '2026-11-24T11:00:00Z',
          },
          { status: 201 }
        );
      })
    );

    const user = userEvent.setup();
    renderAppAt(['/staff']);

    expect(await screen.findByText(/Sticker PHY-101 on back/i)).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /approve claim/i }));

    const idCheckbox = await screen.findByLabelText(/id verified/i);
    const witnessCheckbox = screen.getByLabelText(/witness confirmed/i);
    await user.click(idCheckbox);
    await user.click(witnessCheckbox);
    await user.click(screen.getByRole('button', { name: /complete handover/i }));

    await waitFor(() => expect(handoverDone).toBe(true));
  });

  it('displays verified custody hash chain timeline', async () => {
    seedUserSession('User');
    server.use(
      http.get('/api/v1/custody/found-9', () => {
        return HttpResponse.json({
          reportId: 'found-9',
          isChainValid: true,
          events: [
            {
              id: 'evt-1',
              reportId: 'found-9',
              sequenceNumber: 1,
              eventType: 'Received',
              actorRole: 'User',
              timestamp: '2026-11-24T09:00:00Z',
              notes: 'Item received at Main Security Desk.',
              previousEventHash: 'GENESIS',
              eventHash: 'abcdef1234567890abcdef1234567890',
            },
          ],
        });
      })
    );

    renderAppAt(['/custody/found-9']);
    expect(await screen.findByText(/verified intact/i)).toBeInTheDocument();
    expect(screen.getByText(/#1 Received/i)).toBeInTheDocument();
  });

  it('allows claimant to withdraw a pending claim', async () => {
    seedUserSession('User');
    let withdrawn = false;
    server.use(
      http.get('/api/v1/claims/mine', () => {
        return HttpResponse.json([
          {
            id: 'claim-w',
            foundReportId: 'found-w',
            lostReportId: null,
            status: withdrawn ? 'Withdrawn' : 'Pending',
            verificationAnswers: 'Red strap',
            reviewNotes: null,
            createdAt: '2026-11-24T10:00:00Z',
            foundItem: null,
          },
        ]);
      }),
      http.post('/api/v1/claims/claim-w/withdraw', () => {
        withdrawn = true;
        return HttpResponse.json({
          id: 'claim-w',
          foundReportId: 'found-w',
          lostReportId: null,
          status: 'Withdrawn',
          verificationAnswers: 'Red strap',
          reviewNotes: null,
          createdAt: '2026-11-24T10:00:00Z',
          foundItem: null,
        });
      })
    );

    const user = userEvent.setup();
    renderAppAt(['/my-claims']);

    await user.click(await screen.findByRole('button', { name: /withdraw/i }));
    await waitFor(() => expect(withdrawn).toBe(true));
  });
});

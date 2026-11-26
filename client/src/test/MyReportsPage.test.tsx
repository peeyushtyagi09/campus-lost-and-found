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

describe('MyReportsPage', () => {
  it('displays owner reports with private details and allows closing a report', async () => {
    seedSession();
    let closedCalled = false;

    server.use(
      http.get('/api/v1/lost-reports/mine', () => {
        return HttpResponse.json({
          items: [
            {
              id: 'owner-report-1',
              title: 'Red Headphones',
              category: 'Other',
              publicDescription: 'Left in music practice room',
              campusArea: 'Student Union',
              dateLost: '2026-11-22',
              photoFileName: null,
              photoUrl: null,
              status: closedCalled ? 'Closed' : 'Open',
              privateDetails: 'Serial SN-44221',
              contactPhone: '555-333-4444',
              contactEmail: 'alice@campus.edu',
              createdAt: '2026-11-22T10:00:00Z',
              updatedAt: '2026-11-22T10:00:00Z',
            },
          ],
          page: 1,
          pageSize: 20,
          totalCount: 1,
        });
      }),
      http.delete('/api/v1/lost-reports/owner-report-1', () => {
        closedCalled = true;
        return new HttpResponse(null, { status: 204 });
      })
    );

    const user = userEvent.setup();
    renderAppAt(['/my-reports']);

    expect(await screen.findByText('Red Headphones')).toBeInTheDocument();
    expect(screen.getByText(/Serial SN-44221/)).toBeInTheDocument();
    expect(screen.getByText(/555-333-4444/)).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /open card/i }));
    expect(await screen.findByRole('dialog')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /close item details/i }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /close report/i }));
    await waitFor(() => expect(closedCalled).toBe(true));
  });

  it('allows permanent deletion when the owner found the item themselves', async () => {
    seedSession();
    let deletedPermanent = false;

    server.use(
      http.get('/api/v1/lost-reports/mine', () => {
        return HttpResponse.json({
          items: deletedPermanent
            ? []
            : [
                {
                  id: 'owner-report-2',
                  title: 'Pink Phone',
                  category: 'Phone',
                  publicDescription: 'Left in hostel',
                  campusArea: 'Hostel Block A',
                  dateLost: '2026-11-26',
                  photoFileName: null,
                  photoUrl: null,
                  status: 'Open',
                  privateDetails: 'Pink case',
                  contactPhone: '555-333-4444',
                  contactEmail: 'alice@campus.edu',
                  createdAt: '2026-11-26T10:00:00Z',
                  updatedAt: '2026-11-26T10:00:00Z',
                },
              ],
          page: 1,
          pageSize: 20,
          totalCount: deletedPermanent ? 0 : 1,
        });
      }),
      http.delete('/api/v1/lost-reports/owner-report-2', ({ request }) => {
        const url = new URL(request.url);
        if (url.searchParams.get('permanent') === 'true') {
          deletedPermanent = true;
        }
        return new HttpResponse(null, { status: 204 });
      })
    );

    const user = userEvent.setup();
    renderAppAt(['/my-reports']);

    expect(await screen.findByText('Pink Phone')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /found it myself \(delete\)/i }));
    await user.click(screen.getByRole('button', { name: /confirm delete/i }));

    await waitFor(() => expect(deletedPermanent).toBe(true));
    expect(await screen.findByText(/you have not filed any lost reports yet/i)).toBeInTheDocument();
  });

  it('displays user submitted found reports on My Reports', async () => {
    seedSession();

    server.use(
      http.get('/api/v1/found-reports/mine', () => {
        return HttpResponse.json({
          items: [
            {
              id: 'found-mine-1',
              title: 'Found Blue Bottle',
              category: 'Other',
              publicDescription: 'Insulated bottle found near gym',
              campusArea: 'Sports Complex',
              dateFound: '2026-11-26',
              heldAt: 'Main Security Desk',
              photoFileName: null,
              photoUrl: null,
              status: 'Open',
              storageNotes: 'Shelf A2',
              finderContact: '555-222-1111',
              verificationDetails: 'Sticker on bottom',
              createdAt: '2026-11-26T10:00:00Z',
              updatedAt: '2026-11-26T10:00:00Z',
            },
          ],
          page: 1,
          pageSize: 20,
          totalCount: 1,
        });
      })
    );

    renderAppAt(['/my-reports']);

    expect(await screen.findByText('Found Blue Bottle')).toBeInTheDocument();
    expect(screen.getByText(/Shelf A2/)).toBeInTheDocument();
  });
});

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

describe('ReportFoundPage', () => {
  it('submits a found item report cleanly', async () => {
    seedSession();
    let submitted = false;

    server.use(
      http.post('/api/v1/found-reports', async ({ request }) => {
        const body = (await request.json()) as Record<string, unknown>;
        submitted = true;
        return HttpResponse.json(
          {
            id: 'found-created-1',
            ...body,
            photoUrl: null,
            status: 'Open',
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString(),
          },
          { status: 201 }
        );
      })
    );

    const user = userEvent.setup();
    renderAppAt(['/report-found']);

    await user.type(await screen.findByLabelText(/item title/i), 'Found Silver Keys');
    await user.type(screen.getByLabelText(/public description/i), 'Set of 3 keys on blue ring');
    await user.type(screen.getByLabelText(/locker \/ shelf storage notes/i), 'Bin C3');
    await user.type(screen.getByLabelText(/finder contact/i), '555-888-7777');
    await user.type(screen.getByLabelText(/verification details/i), 'Engraved room 204');

    await user.click(screen.getByRole('button', { name: /submit found report/i }));
    await waitFor(() => expect(submitted).toBe(true));
    expect(
      await screen.findByRole('heading', { name: /public lost & found board/i })
    ).toBeInTheDocument();
  });
});

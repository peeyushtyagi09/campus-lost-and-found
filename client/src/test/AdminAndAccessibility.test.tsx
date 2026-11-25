import axe from 'axe-core';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { server } from './mocks/server';
import { renderAppAt } from './testUtils';

function seedSession(role: 'User' | 'Admin') {
  sessionStorage.setItem(
    'clf_session',
    JSON.stringify({
      accessToken: 'fake-jwt-token',
      expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      user: {
        id: role === 'Admin' ? 'admin-1' : 'user-1',
        displayName: role === 'Admin' ? 'Campus Admin' : 'Alice Student',
        email: role === 'Admin' ? 'admin@campus.edu' : 'alice@campus.edu',
        role,
      },
    })
  );
}

describe('Notifications, Admin, and Accessibility', () => {
  it('renders notifications and marks unread notification as read', async () => {
    seedSession('User');
    let markedRead = false;

    server.use(
      http.get('/api/v1/notifications', () => {
        return HttpResponse.json([
          {
            id: 'notif-1',
            type: 'MatchFound',
            message: 'A possible match was found for your item.',
            relatedEntityId: 'match-1',
            isRead: markedRead,
            createdAt: '2026-11-25T10:00:00Z',
          },
        ]);
      }),
      http.post('/api/v1/notifications/notif-1/read', () => {
        markedRead = true;
        return HttpResponse.json({
          id: 'notif-1',
          type: 'MatchFound',
          message: 'A possible match was found for your item.',
          relatedEntityId: 'match-1',
          isRead: true,
          createdAt: '2026-11-25T10:00:00Z',
        });
      })
    );

    const user = userEvent.setup();
    renderAppAt(['/notifications']);

    expect(
      await screen.findByText(/A possible match was found for your item/i)
    ).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /mark as read/i }));
    await waitFor(() => expect(markedRead).toBe(true));
  });

  it('allows admin to create staff account and view audit logs', async () => {
    seedSession('Admin');
    let created = false;

    server.use(
      http.get('/api/v1/auth/me', () =>
        HttpResponse.json({
          id: 'admin-1',
          displayName: 'Campus Admin',
          email: 'admin@campus.edu',
          role: 'Admin',
        })
      ),
      http.get('/api/v1/admin/users', () =>
        HttpResponse.json([
          {
            id: 'admin-1',
            displayName: 'Campus Admin',
            role: 'Admin',
            createdAt: '2026-11-25T10:00:00Z',
          },
        ])
      ),
      http.get('/api/v1/admin/audit-logs', () =>
        HttpResponse.json({
          items: [
            {
              id: 'aud-1',
              action: 'StaffAccountCreated',
              actorRole: 'Admin',
              targetId: 'staff-2',
              summary: 'Created staff account.',
              timestamp: '2026-11-25T10:00:00Z',
            },
          ],
          page: 1,
          pageSize: 25,
          totalCount: 1,
        })
      ),
      http.post('/api/v1/admin/staff', () => {
        created = true;
        return HttpResponse.json(
          {
            id: 'staff-2',
            displayName: 'New Staff',
            role: 'Staff',
            createdAt: '2026-11-25T10:01:00Z',
          },
          { status: 201 }
        );
      })
    );

    const user = userEvent.setup();
    renderAppAt(['/admin']);

    expect(await screen.findByText(/StaffAccountCreated/i)).toBeInTheDocument();
    await user.type(screen.getByLabelText(/display name/i), 'New Staff');
    await user.type(screen.getByLabelText(/staff email/i), 'staff2@campus.edu');
    await user.type(screen.getByLabelText(/temporary password/i), 'StaffPass123');
    await user.click(screen.getByRole('button', { name: /create staff account/i }));

    await waitFor(() => expect(created).toBe(true));
  });

  it('passes axe accessibility audit on public board page', async () => {
    const { container } = renderAppAt(['/']);
    await screen.findByRole('heading', { name: /public lost & found board/i });
    const results = await axe.run(container, { rules: { 'color-contrast': { enabled: false } } });
    expect(results.violations).toEqual([]);
  });

  it('passes axe accessibility audit on login page', async () => {
    const { container } = renderAppAt(['/login']);
    await screen.findByRole('heading', { name: /sign in/i });
    const results = await axe.run(container, { rules: { 'color-contrast': { enabled: false } } });
    expect(results.violations).toEqual([]);
  });
});

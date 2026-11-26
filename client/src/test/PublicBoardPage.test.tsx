import axe from 'axe-core';
import { fireEvent, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { server } from './mocks/server';
import { renderAppAt } from './testUtils';

describe('PublicBoardPage privacy', () => {
  it('renders only public DTO fields even if payload includes extra private properties', async () => {
    server.use(
      http.get('/api/v1/public/board', () => {
        return HttpResponse.json({
          items: [
            {
              id: 'leak-check-1',
              title: 'Blue Umbrella',
              category: 'Other',
              publicDescription: 'Foldable umbrella near science hall',
              campusArea: 'Science Building',
              dateLost: '2026-11-21',
              photoUrl: null,
              status: 'Open',
              postedAt: '2026-11-21T09:00:00Z',
              contactPhone: 'SECRET_PHONE_999000',
              contactEmail: 'secret.leak@campus.edu',
              privateDetails: 'SECRET_SERIAL_TAG_123',
              reporterId: 'SECRET_REPORTER_ID',
            },
          ],
          page: 1,
          pageSize: 12,
          totalCount: 1,
        });
      })
    );

    renderAppAt(['/']);

    expect(await screen.findByRole('heading', { name: /blue umbrella/i })).toBeInTheDocument();
    expect(screen.getByText(/foldable umbrella near science hall/i)).toBeInTheDocument();

    expect(screen.queryByText(/SECRET_PHONE_999000/)).not.toBeInTheDocument();
    expect(screen.queryByText(/secret\.leak@campus\.edu/)).not.toBeInTheDocument();
    expect(screen.queryByText(/SECRET_SERIAL_TAG_123/)).not.toBeInTheDocument();
    expect(screen.queryByText(/SECRET_REPORTER_ID/)).not.toBeInTheDocument();
  });

  it('switches between lost and found board types', async () => {
    server.use(
      http.get('/api/v1/public/board', ({ request }) => {
        const url = new URL(request.url);
        const type = url.searchParams.get('type');
        return HttpResponse.json({
          items: [
            {
              id: type === 'found' ? 'found-item-1' : 'lost-item-1',
              title: type === 'found' ? 'Found Laptop Charger' : 'Lost Phone',
              category: 'Other',
              publicDescription: 'Item summary',
              campusArea: 'Library',
              dateLost: '2026-11-21',
              photoUrl: null,
              status: 'Open',
              postedAt: '2026-11-21T09:00:00Z',
              heldAt: type === 'found' ? 'Main Security Desk' : null,
            },
          ],
          page: 1,
          pageSize: 12,
          totalCount: 1,
        });
      })
    );

    const user = userEvent.setup();
    renderAppAt(['/']);

    expect(await screen.findByText('Lost Phone')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /found items/i }));
    expect(await screen.findByText('Found Laptop Charger')).toBeInTheDocument();
    expect(screen.getByText(/Main Security Desk/i)).toBeInTheDocument();
  });

  it('renders a category photo on lost item cards and opens card details on click', async () => {
    const user = userEvent.setup();
    const { container } = renderAppAt(['/']);

    const img = await screen.findByRole('img', {
      name: /phone preview image for black pixel phone/i,
    });
    expect(img).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /black pixel phone/i }));

    const dialog = await screen.findByRole('dialog');
    expect(dialog).toBeInTheDocument();
    expect(dialog).toHaveTextContent('Matte black case near main library');
    expect(dialog).toHaveTextContent('Library');

    const results = await axe.run(container, { rules: { 'color-contrast': { enabled: false } } });
    expect(results.violations).toEqual([]);

    await user.click(screen.getByRole('button', { name: 'Close' }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /black pixel phone/i }));
    expect(await screen.findByRole('dialog')).toBeInTheDocument();
    await user.keyboard('{Escape}');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('falls back to category preview when an uploaded photo fails to load and closes on backdrop click', async () => {
    server.use(
      http.get('/api/v1/public/board', () => {
        return HttpResponse.json({
          items: [
            {
              id: 'broken-photo-1',
              title: 'Canvas Backpack',
              category: 'Bag',
              publicDescription: 'Navy backpack',
              campusArea: 'Student Union',
              dateLost: '2026-11-24',
              photoUrl: '/api/v1/media/missing.jpg',
              status: 'Open',
              postedAt: '2026-11-24T10:00:00Z',
            },
          ],
          page: 1,
          pageSize: 12,
          totalCount: 1,
        });
      })
    );

    const user = userEvent.setup();
    renderAppAt(['/']);

    const img = await screen.findByRole('img', { name: /sanitized photo of canvas backpack/i });
    fireEvent.error(img);
    expect(
      await screen.findByRole('img', { name: /bag preview image for canvas backpack/i })
    ).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /canvas backpack/i }));
    const dialog = await screen.findByRole('dialog');
    const modalImg = dialog.querySelector('img');
    expect(modalImg).not.toBeNull();
    if (modalImg) {
      fireEvent.error(modalImg);
    }

    const backdrop = dialog.parentElement;
    expect(backdrop).not.toBeNull();
    if (backdrop) {
      fireEvent.click(backdrop);
    }
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('allows signed-in owner to delete their lost report from the opened board card', async () => {
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

    let deleted = false;
    server.use(
      http.get('/api/v1/public/board', () => {
        return HttpResponse.json({
          items: deleted
            ? []
            : [
                {
                  id: 'own-lost-1',
                  title: 'My Found Pink Phone',
                  category: 'Phone',
                  publicDescription: 'Pink case',
                  campusArea: 'Hostel Block A',
                  dateLost: '2026-11-26',
                  photoUrl: null,
                  status: 'Open',
                  postedAt: '2026-11-26T08:00:00Z',
                },
              ],
          page: 1,
          pageSize: 12,
          totalCount: deleted ? 0 : 1,
        });
      }),
      http.get('/api/v1/lost-reports/mine', () => {
        return HttpResponse.json({
          items: deleted
            ? []
            : [
                {
                  id: 'own-lost-1',
                  title: 'My Found Pink Phone',
                  category: 'Phone',
                  publicDescription: 'Pink case',
                  campusArea: 'Hostel Block A',
                  dateLost: '2026-11-26',
                  photoFileName: null,
                  photoUrl: null,
                  status: 'Open',
                  privateDetails: 'Pin',
                  contactPhone: '555-123-4567',
                  contactEmail: 'alice@campus.edu',
                  createdAt: '2026-11-26T08:00:00Z',
                  updatedAt: '2026-11-26T08:00:00Z',
                },
              ],
          page: 1,
          pageSize: 50,
          totalCount: deleted ? 0 : 1,
        });
      }),
      http.delete('/api/v1/lost-reports/own-lost-1', ({ request }) => {
        if (new URL(request.url).searchParams.get('permanent') === 'true') {
          deleted = true;
        }
        return new HttpResponse(null, { status: 204 });
      })
    );

    const user = userEvent.setup();
    renderAppAt(['/']);

    await user.click(await screen.findByRole('button', { name: /my found pink phone/i }));
    await user.click(await screen.findByRole('button', { name: /found it myself \(delete\)/i }));
    await user.click(screen.getByRole('button', { name: /confirm delete/i }));

    expect(await screen.findByText(/no lost items match the current filters/i)).toBeInTheDocument();
  });
});

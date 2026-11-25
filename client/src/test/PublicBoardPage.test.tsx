import { screen } from '@testing-library/react';
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
});

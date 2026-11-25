import { http, HttpResponse } from 'msw';
import type { LoginRequest, LoginResponse, RegisterRequest } from '../../types/auth';
import type { PagedResponse } from '../../types/common';
import type { PublicReport } from '../../types/reports';

export const samplePublicReports: PublicReport[] = [
  {
    id: 'report-1',
    title: 'Black Pixel Phone',
    category: 'Phone',
    publicDescription: 'Matte black case near main library',
    campusArea: 'Library',
    dateLost: '2026-11-20',
    photoUrl: null,
    status: 'Open',
    postedAt: '2026-11-20T10:00:00Z',
  },
];

export const handlers = [
  http.get('/api/v1/public/board', () => {
    const response: PagedResponse<PublicReport> = {
      items: samplePublicReports,
      page: 1,
      pageSize: 12,
      totalCount: samplePublicReports.length,
    };
    return HttpResponse.json(response);
  }),

  http.post('/api/v1/auth/register', async ({ request }) => {
    const body = (await request.json()) as RegisterRequest;
    return HttpResponse.json(
      {
        user: {
          id: 'user-1',
          displayName: body.displayName,
          email: body.email,
          role: 'User',
        },
      },
      { status: 201 }
    );
  }),

  http.post('/api/v1/auth/login', async ({ request }) => {
    const body = (await request.json()) as LoginRequest;
    if (body.password !== 'StrongPass123') {
      return HttpResponse.json(
        {
          status: 401,
          title: 'Invalid credentials.',
        },
        { status: 401 }
      );
    }

    const payload: LoginResponse = {
      accessToken: 'fake-jwt-token',
      expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      user: {
        id: 'user-1',
        displayName: 'Alice Student',
        email: body.email,
        role: 'User',
      },
    };
    return HttpResponse.json(payload);
  }),

  http.get('/api/v1/auth/me', () => {
    return HttpResponse.json({
      id: 'user-1',
      displayName: 'Alice Student',
      email: 'alice@campus.edu',
      role: 'User',
    });
  }),

  http.post('/api/v1/uploads/photos', () => {
    return HttpResponse.json(
      {
        fileName: '0123456789abcdef0123456789abcdef.jpg',
        url: '/api/v1/media/0123456789abcdef0123456789abcdef.jpg',
      },
      { status: 201 }
    );
  }),

  http.post('/api/v1/lost-reports', async ({ request }) => {
    const body = (await request.json()) as Record<string, unknown>;
    return HttpResponse.json(
      {
        id: 'created-report-1',
        ...body,
        photoUrl: null,
        status: 'Open',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      },
      { status: 201 }
    );
  }),

  http.get('/api/v1/lost-reports/mine', () => {
    return HttpResponse.json({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    });
  }),
];

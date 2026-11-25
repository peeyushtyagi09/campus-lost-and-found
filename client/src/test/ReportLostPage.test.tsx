import { fireEvent, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderAppAt } from './testUtils';

function seedAuthenticatedSession() {
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

describe('ReportLostPage', () => {
  it('validates fields inline and shows EXIF removal notice', async () => {
    seedAuthenticatedSession();
    const user = userEvent.setup();
    renderAppAt(['/report-lost']);

    expect(
      await screen.findByText(/photo location data is removed automatically/i)
    ).toBeInTheDocument();

    await user.type(screen.getByLabelText(/item title/i), 'My');
    await user.type(screen.getByLabelText(/public description/i), 'Email me at leak@campus.edu');
    await user.click(screen.getByRole('button', { name: /submit lost report/i }));

    expect(await screen.findByText(/title must be at least 3 characters/i)).toBeInTheDocument();
    expect(
      screen.getByText(/public description must not contain email addresses/i)
    ).toBeInTheDocument();
    expect(screen.getByText(/contact phone is required/i)).toBeInTheDocument();
  });

  it('rejects unsupported and oversized photos client-side', async () => {
    seedAuthenticatedSession();
    renderAppAt(['/report-lost']);

    const input = (await screen.findByLabelText(/optional photo/i)) as HTMLInputElement;

    const badTypeFile = new File(['hello'], 'notes.txt', { type: 'text/plain' });
    fireEvent.change(input, { target: { files: [badTypeFile] } });
    expect(
      await screen.findByText(/photo must be a jpeg, png, or webp image/i)
    ).toBeInTheDocument();

    const oversized = new File([new Uint8Array(5 * 1024 * 1024 + 10)], 'huge.jpg', {
      type: 'image/jpeg',
    });
    fireEvent.change(input, { target: { files: [oversized] } });
    expect(await screen.findByText(/photo must be 5 mb or smaller/i)).toBeInTheDocument();
  });

  it('uploads valid photo, submits report, and redirects to my reports', async () => {
    seedAuthenticatedSession();
    const user = userEvent.setup();
    renderAppAt(['/report-lost']);

    await user.type(await screen.findByLabelText(/item title/i), 'Silver Keys');
    await user.type(screen.getByLabelText(/public description/i), 'Keychain with blue tag');
    await user.type(screen.getByLabelText(/private identifying details/i), 'Brass key #42');
    await user.type(screen.getByLabelText(/contact phone/i), '5551234567');
    await user.type(screen.getByLabelText(/contact email/i), 'alice@campus.edu');

    const input = screen.getByLabelText(/optional photo/i) as HTMLInputElement;
    const validFile = new File([new Uint8Array([1, 2, 3])], 'keys.jpg', { type: 'image/jpeg' });
    fireEvent.change(input, { target: { files: [validFile] } });

    await user.click(screen.getByRole('button', { name: /submit lost report/i }));
    expect(await screen.findByRole('heading', { name: /my lost reports/i })).toBeInTheDocument();
  });
});

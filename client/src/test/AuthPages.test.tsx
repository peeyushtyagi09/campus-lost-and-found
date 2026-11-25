import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderAppAt } from './testUtils';

describe('Authentication pages', () => {
  it('logs in successfully and redirects to board', async () => {
    const user = userEvent.setup();
    renderAppAt(['/login']);

    await user.type(screen.getByLabelText(/^email$/i), 'alice@campus.edu');
    await user.type(screen.getByLabelText(/^password$/i), 'StrongPass123');
    await user.click(screen.getByRole('button', { name: /^sign in$/i }));

    expect(
      await screen.findByRole('heading', { name: /public lost & found board/i })
    ).toBeInTheDocument();
    expect(screen.getByText(/signed in as alice student/i)).toBeInTheDocument();
  });

  it('shows server error on invalid credentials', async () => {
    const user = userEvent.setup();
    renderAppAt(['/login']);

    await user.type(screen.getByLabelText(/^email$/i), 'alice@campus.edu');
    await user.type(screen.getByLabelText(/^password$/i), 'WrongPassword9');
    await user.click(screen.getByRole('button', { name: /^sign in$/i }));

    expect(await screen.findByText(/invalid credentials/i)).toBeInTheDocument();
  });

  it('shows inline validation errors on weak registration input', async () => {
    const user = userEvent.setup();
    renderAppAt(['/register']);

    await user.type(screen.getByLabelText(/display name/i), 'A');
    await user.type(screen.getByLabelText(/campus email/i), 'not-an-email');
    await user.type(screen.getByLabelText(/^password$/i), 'weak');
    await user.click(screen.getByRole('button', { name: /create account/i }));

    expect(await screen.findByText(/at least 2 characters/i)).toBeInTheDocument();
    expect(screen.getByText(/enter a valid email address/i)).toBeInTheDocument();
    expect(screen.getByText('Password must be at least 10 characters.')).toBeInTheDocument();
  });
});

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { getNotifications, markNotificationRead } from '../api/notifications';
import styles from './BoardPage.module.css';

export function NotificationsPage() {
  const queryClient = useQueryClient();

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['notifications'],
    queryFn: getNotifications,
  });

  const readMutation = useMutation({
    mutationFn: (id: string) => markNotificationRead(id),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['notifications'] }),
  });

  return (
    <section aria-labelledby="notifications-heading">
      <h1 id="notifications-heading">Notifications</h1>
      {isLoading ? (
        <p role="status">Loading notifications...</p>
      ) : isError ? (
        <p className="alert-error" role="alert">
          {error instanceof Error ? error.message : 'Unable to load notifications.'}
        </p>
      ) : !data || data.length === 0 ? (
        <p className="alert-info">You have no notifications.</p>
      ) : (
        <div className={styles.list}>
          {data.map((notif) => (
            <article key={notif.id} className={styles.reportCard}>
              <div className={styles.header}>
                <div>
                  <h2>{notif.type}</h2>
                  <p>{notif.message}</p>
                  <p className="field-hint">
                    {new Date(notif.createdAt).toLocaleString()} &middot;{' '}
                    {notif.isRead ? 'Read' : 'Unread'}
                  </p>
                </div>
                {!notif.isRead ? (
                  <button
                    type="button"
                    className="btn btn-secondary"
                    disabled={readMutation.isPending}
                    onClick={() => readMutation.mutate(notif.id)}
                  >
                    Mark as read
                  </button>
                ) : null}
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}

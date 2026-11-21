import { api } from './client';

/** Which kinds of email the caller gets; everything is on until switched off. */
export interface NotificationSettingsDto {
  emailInvitations: boolean;
  emailRequests: boolean;
  emailMessages: boolean;
  emailComments: boolean;
}

const BASE = '/api/collaboration/notification-settings';

export const notificationsApi = {
  get: () => api.get<NotificationSettingsDto>(BASE),
  save: (settings: NotificationSettingsDto) => api.put<NotificationSettingsDto>(BASE, settings),
};

import { inject, Injectable, signal } from '@angular/core';
import { MailboxMessageDto } from '../../generated/client/models/mailbox-message-dto';
import { MailboxService as GeneratedMailboxService } from '../../generated/client/services/mailbox.service';

@Injectable({ providedIn: 'root' })
export class MailboxService {
  private readonly mailboxApi = inject(GeneratedMailboxService);

  readonly messages = signal<MailboxMessageDto[]>([]);
  readonly unreadCount = signal(0);
  readonly isLoading = signal(false);

  async refreshUnreadCount(): Promise<number> {
    const response = await this.mailboxApi.apiMailboxUnreadCountGet$Json();
    const unreadCount = Number(response.unreadCount ?? 0);
    this.unreadCount.set(unreadCount);
    return unreadCount;
  }

  async loadMessages(): Promise<void> {
    this.isLoading.set(true);

    try {
      const messages = await this.mailboxApi.apiMailboxGet$Json();
      this.messages.set(messages);
      this.unreadCount.set(messages.filter(message => !message.isRead).length);
    } finally {
      this.isLoading.set(false);
    }
  }

  async markAsRead(messageId: string): Promise<void> {
    await this.mailboxApi.apiMailboxMessageIdReadPost({ messageId });
    const wasUnread = this.messages().some(message => message.id === messageId && !message.isRead);

    this.messages.update(messages => messages.map(message =>
      message.id === messageId && !message.isRead
        ? { ...message, isRead: true, readAtUtc: new Date().toISOString() }
        : message));

    if (wasUnread) {
      this.unreadCount.set(Math.max(0, this.unreadCount() - 1));
    }
  }

  async markAllAsRead(): Promise<void> {
    await this.mailboxApi.apiMailboxReadAllPost();
    this.messages.update(messages => messages.map(message =>
      message.isRead
        ? message
        : { ...message, isRead: true, readAtUtc: new Date().toISOString() }));
    this.unreadCount.set(0);
  }
}

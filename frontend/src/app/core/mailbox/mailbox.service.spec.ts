/// <reference types="vitest/globals" />
import { TestBed } from '@angular/core/testing';
import { MailboxService as GeneratedMailboxService } from '../../generated/client/services/mailbox.service';
import { MailboxService } from './mailbox.service';

describe('MailboxService', () => {
  let service: MailboxService;
  let mailboxApi: {
    apiMailboxUnreadCountGet$Json: ReturnType<typeof vi.fn>;
    apiMailboxGet$Json: ReturnType<typeof vi.fn>;
    apiMailboxMessageIdReadPost: ReturnType<typeof vi.fn>;
    apiMailboxReadAllPost: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-04-19T20:10:00Z'));

    mailboxApi = {
      apiMailboxUnreadCountGet$Json: vi.fn(),
      apiMailboxGet$Json: vi.fn(),
      apiMailboxMessageIdReadPost: vi.fn(),
      apiMailboxReadAllPost: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        MailboxService,
        {
          provide: GeneratedMailboxService,
          useValue: mailboxApi,
        },
      ],
    });

    service = TestBed.inject(MailboxService);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('refreshUnreadCount updates the signal and returns the unread count', async () => {
    mailboxApi.apiMailboxUnreadCountGet$Json.mockResolvedValue({ unreadCount: 3 });

    const result = await service.refreshUnreadCount();

    expect(result).toBe(3);
    expect(service.unreadCount()).toBe(3);
  });

  it('loadMessages fills messages, recalculates unread count and clears loading state', async () => {
    mailboxApi.apiMailboxGet$Json.mockResolvedValue([
      {
        id: '1',
        isRead: false,
        category: 'a',
        titleKey: 'a',
        bodyKey: 'a',
        parameters: {},
        createdAt: '',
      },
      {
        id: '2',
        isRead: true,
        category: 'b',
        titleKey: 'b',
        bodyKey: 'b',
        parameters: {},
        createdAt: '',
      },
    ]);

    const loadPromise = service.loadMessages();

    expect(service.isLoading()).toBe(true);

    await loadPromise;

    expect(service.messages()).toHaveLength(2);
    expect(service.unreadCount()).toBe(1);
    expect(service.isLoading()).toBe(false);
  });

  it('markAsRead updates local message state and decreases unread count once', async () => {
    service.messages.set([
      {
        id: 'message-1',
        isRead: false,
        readAtUtc: undefined,
        category: 'mailbox',
        titleKey: 'title',
        bodyKey: 'body',
        parameters: {},
        createdAt: '',
      },
    ]);
    service.unreadCount.set(1);
    mailboxApi.apiMailboxMessageIdReadPost.mockResolvedValue(undefined);

    await service.markAsRead('message-1');

    expect(mailboxApi.apiMailboxMessageIdReadPost).toHaveBeenCalledWith({
      messageId: 'message-1',
    });
    expect(service.messages()[0]?.isRead).toBe(true);
    expect(service.messages()[0]?.readAtUtc).toBe('2026-04-19T20:10:00.000Z');
    expect(service.unreadCount()).toBe(0);
  });

  it('markAllAsRead updates every message and resets unread count', async () => {
    service.messages.set([
      {
        id: 'message-1',
        isRead: false,
        readAtUtc: undefined,
        category: 'mailbox',
        titleKey: 'title',
        bodyKey: 'body',
        parameters: {},
        createdAt: '',
      },
      {
        id: 'message-2',
        isRead: true,
        readAtUtc: '2026-04-19T18:00:00.000Z',
        category: 'mailbox',
        titleKey: 'title',
        bodyKey: 'body',
        parameters: {},
        createdAt: '',
      },
    ]);
    service.unreadCount.set(1);
    mailboxApi.apiMailboxReadAllPost.mockResolvedValue(undefined);

    await service.markAllAsRead();

    expect(mailboxApi.apiMailboxReadAllPost).toHaveBeenCalledOnce();
    expect(service.messages().every((message) => message.isRead)).toBe(true);
    expect(service.unreadCount()).toBe(0);
  });
});

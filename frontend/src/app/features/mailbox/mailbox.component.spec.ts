import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { MailboxService } from '../../core/mailbox/mailbox.service';
import { MailboxPageComponent } from './mailbox.component';

describe('MailboxPageComponent', () => {
  let mailbox: {
    messages: ReturnType<typeof signal>;
    isLoading: ReturnType<typeof signal>;
    unreadCount: ReturnType<typeof signal>;
    loadMessages: ReturnType<typeof vi.fn>;
    markAsRead: ReturnType<typeof vi.fn>;
    markAllAsRead: ReturnType<typeof vi.fn>;
  };
  let router: {
    navigateByUrl: ReturnType<typeof vi.fn>;
  };
  let snackBar: {
    open: ReturnType<typeof vi.fn>;
  };
  let translate: {
    instant: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    mailbox = {
      messages: signal([]),
      isLoading: signal(false),
      unreadCount: signal(0),
      loadMessages: vi.fn().mockResolvedValue(undefined),
      markAsRead: vi.fn().mockResolvedValue(undefined),
      markAllAsRead: vi.fn().mockResolvedValue(undefined),
    };
    router = {
      navigateByUrl: vi.fn().mockResolvedValue(true),
    };
    snackBar = {
      open: vi.fn(),
    };
    translate = {
      instant: vi.fn((key: string) => key),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: MailboxService, useValue: mailbox },
        { provide: Router, useValue: router },
        { provide: MatSnackBar, useValue: snackBar },
        { provide: TranslateService, useValue: translate },
      ],
    });
  });

  it('loads messages on init', async () => {
    const component = TestBed.runInInjectionContext(() => new MailboxPageComponent());

    await component.ngOnInit();

    expect(mailbox.loadMessages).toHaveBeenCalledOnce();
  });

  it('shows a snackbar when initial loading fails', async () => {
    mailbox.loadMessages.mockRejectedValue(new Error('network'));
    const component = TestBed.runInInjectionContext(() => new MailboxPageComponent());

    await component.ngOnInit();

    expect(snackBar.open).toHaveBeenCalledWith('mailbox.failedToLoad', 'common.close', {
      duration: 4000,
    });
  });

  it('does not call markAsRead for already read messages', async () => {
    const component = TestBed.runInInjectionContext(() => new MailboxPageComponent());

    await component.markAsRead({
      id: 'message-1',
      isRead: true,
      category: 'mailbox',
      titleKey: 'title',
      bodyKey: 'body',
      parameters: {},
      createdAt: '',
    });

    expect(mailbox.markAsRead).not.toHaveBeenCalled();
  });

  it('opens a message by marking it as read and navigating to its link', async () => {
    const component = TestBed.runInInjectionContext(() => new MailboxPageComponent());
    const message = {
      id: 'message-1',
      isRead: false,
      category: 'mailbox',
      titleKey: 'title',
      bodyKey: 'body',
      parameters: {},
      createdAt: '',
      link: '/products',
    };

    await component.openMessage(message);

    expect(mailbox.markAsRead).toHaveBeenCalledWith('message-1');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/products');
  });

  it('shows a snackbar when markAllAsRead fails', async () => {
    mailbox.markAllAsRead.mockRejectedValue(new Error('network'));
    const component = TestBed.runInInjectionContext(() => new MailboxPageComponent());

    await component.markAllAsRead();

    expect(snackBar.open).toHaveBeenCalledWith('mailbox.failedToUpdate', 'common.close', {
      duration: 4000,
    });
  });
});

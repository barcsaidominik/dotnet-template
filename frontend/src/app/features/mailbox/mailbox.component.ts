import type { OnInit } from '@angular/core';
import { Component, DestroyRef, computed, inject } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { filter, pairwise } from 'rxjs';
import type { MailboxMessageDto } from '../../generated/client/models/mailbox-message-dto';
import { MailboxService } from '../../core/mailbox/mailbox.service';
import { LanguageService } from '../../core/i18n/language.service';

@Component({
  selector: 'app-mailbox',
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    TranslatePipe,
  ],
  templateUrl: './mailbox.component.html',
  styleUrls: ['./mailbox.component.scss'],
})
export class MailboxPageComponent implements OnInit {
  private readonly mailbox = inject(MailboxService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);
  private readonly destroyRef = inject(DestroyRef);
  readonly lang = inject(LanguageService);

  readonly messages = this.mailbox.messages;
  readonly isLoading = this.mailbox.isLoading;
  readonly unreadCount = this.mailbox.unreadCount;
  readonly hasMessages = computed(() => this.messages().length > 0);

  constructor() {
    toObservable(this.mailbox.unreadCount)
      .pipe(
        pairwise(),
        filter(([prev, curr]) => curr > prev),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => void this.mailbox.loadMessages());
  }

  async ngOnInit(): Promise<void> {
    try {
      await this.mailbox.loadMessages();
    } catch {
      this.snackBar.open(
        this.translate.instant('mailbox.failedToLoad'),
        this.translate.instant('common.close'),
        { duration: 4000 }
      );
    }
  }

  async markAsRead(message: MailboxMessageDto): Promise<void> {
    if (message.isRead) {
      return;
    }

    try {
      await this.mailbox.markAsRead(message.id);
    } catch {
      this.snackBar.open(
        this.translate.instant('mailbox.failedToUpdate'),
        this.translate.instant('common.close'),
        { duration: 4000 }
      );
    }
  }

  async markAllAsRead(): Promise<void> {
    try {
      await this.mailbox.markAllAsRead();
    } catch {
      this.snackBar.open(
        this.translate.instant('mailbox.failedToUpdate'),
        this.translate.instant('common.close'),
        { duration: 4000 }
      );
    }
  }

  async openMessage(message: MailboxMessageDto): Promise<void> {
    await this.markAsRead(message);

    if (message.link && this.isLocalUrl(message.link)) {
      await this.router.navigateByUrl(message.link);
    }
  }

  private isLocalUrl(url: string): boolean {
    return url.startsWith('/') && !url.startsWith('//');
  }
}

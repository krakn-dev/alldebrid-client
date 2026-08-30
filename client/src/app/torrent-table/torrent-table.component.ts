import { Component, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Torrent } from '../models/torrent.model';
import { TorrentService } from '../torrent.service';
import { forkJoin, Observable } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { NgClass, DecimalPipe, DatePipe } from '@angular/common';
import { TorrentStatusPipe } from '../torrent-status.pipe';
import { SortPipe } from '../sort.pipe';
import { FileSizePipe } from '../filesize.pipe';
import { FilterPipe } from '../filter.pipe';

@Component({
  selector: 'app-torrent-table',
  templateUrl: './torrent-table.component.html',
  styleUrls: ['./torrent-table.component.scss'],
  imports: [FormsModule, NgClass, DecimalPipe, DatePipe, TorrentStatusPipe, SortPipe, FileSizePipe, FilterPipe],
  standalone: true,
})
export class TorrentTableComponent implements OnInit {
  private router = inject(Router);
  private torrentService = inject(TorrentService);

  public torrents: Torrent[] = [];
  public selectedTorrents: string[] = [];
  public error: string;
  public sortProperty = 'rdName';
  public sortDirection: 'asc' | 'desc' = 'asc';
  public filterText = '';

  public isDeleteModalActive: boolean;
  public deleteError: string;
  public deleting: boolean;
  public deleteSelectAll: boolean;
  public deleteData: boolean;
  public deleteRdTorrent: boolean;
  public deleteLocalFiles: boolean;

  public isRetryModalActive: boolean;
  public retryError: string;
  public retrying: boolean;

  public isChangeSettingsModalActive: boolean;
  public changeSettingsError: string;
  public changingSettings: boolean;

  public updateSettingsDownloadClient: number;
  public updateSettingsHostDownloadAction: number;
  public updateSettingsCategory: string;
  public updateSettingsPriority: number;
  public updateSettingsDownloadRetryAttempts: number;
  public updateSettingsTorrentRetryAttempts: number;
  public updateSettingsDeleteOnError: number;
  public updateSettingsTorrentLifetime: number;

  constructor() {
    const torrentService = this.torrentService;

    torrentService.update$.pipe(takeUntilDestroyed()).subscribe((result) => {
      this.torrents = result;
    });
  }

  ngOnInit(): void {
    this.torrentService.getList().subscribe({
      next: (result) => {
        this.torrents = result;
      },
      error: (err) => {
        this.error = err.error;
      },
    });
  }

  public sort(property: string): void {
    this.sortDirection = this.sortProperty === property ? (this.sortDirection === 'asc' ? 'desc' : 'asc') : 'asc';
    this.sortProperty = property;
  }

  public sortIcon(property: string): Record<string, boolean> {
    const active = this.sortProperty === property;
    return {
      'fa-sort': !active,
      'fa-sort-up': active && this.sortDirection === 'asc',
      'fa-sort-down': active && this.sortDirection === 'desc',
      'sort-active': active,
    };
  }

  public openTorrent(torrentId: string): void {
    this.router.navigate([`/torrent/${torrentId}`]);
  }

  public toggleDeleteSelectAll(event: Event) {
    this.selectedTorrents = [];

    if ((event.target as HTMLInputElement).checked) {
      this.torrents.forEach((torrent) => {
        this.selectedTorrents.push(torrent.torrentId);
      });
    }
  }

  public toggleSelect(torrentId: string) {
    const index = this.selectedTorrents.indexOf(torrentId);

    if (index > -1) {
      this.selectedTorrents.splice(index, 1);
    } else {
      this.selectedTorrents.push(torrentId);
    }
  }

  public showDeleteModal(): void {
    this.deleteData = false;
    this.deleteRdTorrent = false;
    this.deleteLocalFiles = false;
    this.deleteError = null;

    this.isDeleteModalActive = true;
  }

  public deleteCancel(): void {
    this.isDeleteModalActive = false;
  }

  public deleteOk(): void {
    this.deleting = true;

    const calls: Observable<void>[] = [];

    this.selectedTorrents.forEach((torrentId) => {
      calls.push(this.torrentService.delete(torrentId, this.deleteData, this.deleteRdTorrent, this.deleteLocalFiles));
    });

    forkJoin(calls).subscribe({
      complete: () => {
        this.isDeleteModalActive = false;
        this.deleting = false;

        this.selectedTorrents = [];
      },
      error: (err) => {
        this.deleteError = err.error;
        this.deleting = false;
      },
    });
  }

  public showRetryModal(): void {
    this.retryError = null;

    this.isRetryModalActive = true;
  }

  public retryCancel(): void {
    this.isRetryModalActive = false;
  }

  public retryOk(): void {
    this.retrying = true;

    const calls: Observable<void>[] = [];

    this.selectedTorrents.forEach((torrentId) => {
      calls.push(this.torrentService.retry(torrentId));
    });

    forkJoin(calls).subscribe({
      complete: () => {
        this.isRetryModalActive = false;
        this.retrying = false;

        this.selectedTorrents = [];
      },
      error: (err) => {
        this.retryError = err.error;
        this.retrying = false;
      },
    });
  }

  public changeSettingsModal(): void {
    this.changeSettingsError = null;

    const selected = this.torrents.filter((m) => this.selectedTorrents.includes(m.torrentId));
    const cv = <V>(getter: (t: Torrent) => V) => this.consensus(selected, getter);

    this.updateSettingsDownloadClient = cv((m) => m.downloadClient);
    this.updateSettingsHostDownloadAction = cv((m) => m.hostDownloadAction);
    this.updateSettingsCategory = cv((m) => m.category);
    this.updateSettingsPriority = cv((m) => m.priority);
    this.updateSettingsDownloadRetryAttempts = cv((m) => m.downloadRetryAttempts);
    this.updateSettingsTorrentRetryAttempts = cv((m) => m.torrentRetryAttempts);
    this.updateSettingsDeleteOnError = cv((m) => m.deleteOnError);
    this.updateSettingsTorrentLifetime = cv((m) => m.lifetime);

    this.isChangeSettingsModalActive = true;
  }

  private consensus<V>(items: Torrent[], getter: (item: Torrent) => V): V | null {
    const first = getter(items[0]);
    return items.every((item) => getter(item) === first) ? first : null;
  }

  public changeSettingsCancel(): void {
    this.isChangeSettingsModalActive = false;
  }

  public changeSettingsOk(): void {
    this.changingSettings = true;

    const calls: Observable<void>[] = [];

    const selectedTorrents = this.torrents.filter((m) => this.selectedTorrents.indexOf(m.torrentId) > -1);

    selectedTorrents.forEach((torrent) => {
      if (this.updateSettingsDownloadClient != null) {
        torrent.downloadClient = this.updateSettingsDownloadClient;
      }
      if (this.updateSettingsHostDownloadAction != null) {
        torrent.hostDownloadAction = this.updateSettingsHostDownloadAction;
      }
      if (this.updateSettingsCategory != null) {
        torrent.category = this.updateSettingsCategory;
      }
      if (this.updateSettingsPriority != null) {
        torrent.priority = this.updateSettingsPriority;
      }
      if (this.updateSettingsDownloadRetryAttempts != null) {
        torrent.downloadRetryAttempts = this.updateSettingsDownloadRetryAttempts;
      }
      if (this.updateSettingsTorrentRetryAttempts != null) {
        torrent.torrentRetryAttempts = this.updateSettingsTorrentRetryAttempts;
      }
      if (this.updateSettingsDeleteOnError != null) {
        torrent.deleteOnError = this.updateSettingsDeleteOnError;
      }
      if (this.updateSettingsTorrentLifetime != null) {
        torrent.lifetime = this.updateSettingsTorrentLifetime;
      }

      calls.push(this.torrentService.update(torrent));
    });

    forkJoin(calls).subscribe({
      complete: () => {
        this.isChangeSettingsModalActive = false;
        this.changingSettings = false;

        this.selectedTorrents = [];
      },
      error: (err) => {
        this.changeSettingsError = err.error;
        this.changingSettings = false;
      },
    });
  }
  toggleDeleteSelectAllOptions() {
    this.deleteData = this.deleteSelectAll;
    this.deleteRdTorrent = this.deleteSelectAll;
    this.deleteLocalFiles = this.deleteSelectAll;
  }

  updateDeleteSelectAll() {
    this.deleteSelectAll = this.deleteData && this.deleteRdTorrent && this.deleteLocalFiles;
  }
}

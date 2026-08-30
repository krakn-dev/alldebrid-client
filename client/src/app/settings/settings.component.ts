import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { SettingsService } from 'src/app/settings.service';
import { Setting } from '../models/setting.model';
import { NgClass, KeyValuePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Nl2BrPipe } from '../nl2br.pipe';
import { FileSizePipe } from '../filesize.pipe';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss'],
  imports: [NgClass, FormsModule, KeyValuePipe, Nl2BrPipe, FileSizePipe],
  standalone: true,
})
export class SettingsComponent implements OnInit {
  private settingsService = inject(SettingsService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  public activeTab = 0;

  public profileUsername: string;
  public profilePassword: string;
  public profileSaving = false;
  public profileSuccess = false;
  public profileError: string = null;

  public tabs: Setting[] = [];
  private settingMap = new Map<string, Setting>();

  public saving = false;
  public error: string;

  public testPathError: string;
  public testPathSuccess: boolean;

  public testDownloadSpeedError: string;
  public testDownloadSpeedSuccess: number;

  public testWriteSpeedError: string;
  public testWriteSpeedSuccess: number;

  public canRegisterMagnetHandler = false;

  ngOnInit(): void {
    this.reset();
    this.canRegisterMagnetHandler = !!(window.isSecureContext && 'registerProtocolHandler' in navigator);
  }

  public reset(): void {
    this.settingsService.get().subscribe((settings) => {
      this.tabs = settings.filter((m) => m.key.indexOf(':') === -1);

      for (let tab of this.tabs) {
        tab.settings = settings.filter((m) => m.key.indexOf(`${tab.key}:`) > -1);
      }

      this.settingMap = new Map(settings.map((s) => [s.key, s]));
      this.cdr.detectChanges();
    });
  }

  private getSetting(key: string): string {
    return (this.settingMap.get(key)?.value as string) || '';
  }

  public ok(): void {
    this.saving = true;

    const settingsToSave = this.tabs.flatMap((m) => m.settings).filter((m) => m.type !== 'Object');

    this.settingsService.update(settingsToSave).subscribe({
      next: () =>
        setTimeout(() => {
          this.saving = false;
        }, 1000),
      error: (err) => {
        this.saving = false;
        this.error = err;
      },
    });
  }

  public testDownloadPath(): void {
    const settingDownloadPath = this.getSetting('Paths:DownloadPath');

    this.saving = true;
    this.testPathError = null;
    this.testPathSuccess = false;

    this.settingsService.testPath(settingDownloadPath).subscribe({
      next: () => {
        this.saving = false;
        this.testPathSuccess = true;
      },
      error: (err) => {
        this.testPathError = err.error;
        this.saving = false;
      },
    });
  }

  public testDownloadSpeed(): void {
    this.saving = true;
    this.testDownloadSpeedError = null;
    this.testDownloadSpeedSuccess = 0;

    this.settingsService.testDownloadSpeed().subscribe({
      next: (result) => {
        this.saving = false;
        this.testDownloadSpeedSuccess = result;
      },
      error: (err) => {
        this.testDownloadSpeedError = err.error;
        this.saving = false;
      },
    });
  }
  public testWriteSpeed(): void {
    this.saving = true;
    this.testWriteSpeedError = null;
    this.testWriteSpeedSuccess = 0;

    this.settingsService.testWriteSpeed().subscribe({
      next: (result) => {
        this.saving = false;
        this.testWriteSpeedSuccess = result;
      },
      error: (err) => {
        this.testWriteSpeedError = err.error;
        this.saving = false;
      },
    });
  }

  public getPlaceholder(setting: Setting): string {
    switch (setting.key) {
      case 'Paths:MappedPath':
        return this.getSetting('Paths:DownloadPath') || 'same as download path';
      case 'Paths:WatchErrorPath':
      case 'Paths:WatchProcessedPath': {
        const wp = this.getSetting('Paths:WatchPath');
        const sub = setting.key === 'Paths:WatchErrorPath' ? 'error' : 'processed';
        return wp ? `${wp}\\${sub}` : '';
      }
      default:
        return '';
    }
  }

  public saveProfile(): void {
    this.profileSuccess = false;
    this.profileError = null;
    this.profileSaving = true;

    this.authService.update(this.profileUsername, this.profilePassword).subscribe({
      next: () => {
        this.profileSuccess = true;
        this.profileSaving = false;
      },
      error: (err) => {
        this.profileError = err.error;
        this.profileSuccess = false;
        this.profileSaving = false;
      },
    });
  }

  public registerMagnetHandler(): void {
    try {
      navigator.registerProtocolHandler('magnet', `${window.location.origin}/add?magnet=%s`);
      alert(
        'Success! Your browser will now prompt you to confirm and add the client as the default handler for magnet links.'
      );
    } catch (error) {
      alert('Magnet link registration failed.');
    }
  }
}

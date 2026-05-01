import { Component, OnInit } from '@angular/core';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';
import { Profile } from '../models/profile.model';
import { SettingsService } from '../settings.service';
import { NgClass } from '@angular/common';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss'],
  imports: [RouterLink, NgClass],
  standalone: true,
})
export class NavbarComponent implements OnInit {
  public showMobileMenu = false;

  public profile: Profile;
  public providerLink: string;
  public version: string;

  constructor(
    private settingsService: SettingsService,
    private authService: AuthService,
    private router: Router,
  ) {
    this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        this.showMobileMenu = false;
      }
    });
  }

  ngOnInit(): void {
    this.settingsService.getProfile().subscribe((result) => {
      this.profile = result;

      switch (result.provider) {
        case 'RealDebrid':
          this.providerLink = 'https://real-debrid.com/?id=1348683';
          break;
        case 'AllDebrid':
          this.providerLink = 'https://alldebrid.com/account/';
          break;
        case 'Premiumize':
          this.providerLink = 'https://www.premiumize.me/';
          break;
        case 'TorBox':
          this.providerLink = 'https://torbox.app/';
          break;
        case 'DebridLink':
          this.providerLink = 'https://debrid-link.com/';
          break;
      }
    });

    this.settingsService.getVersion().subscribe((result) => {
      this.version = result.version;
    });
  }

  get premiumDays(): number {
    if (!this.profile?.expiration) return 0;
    const diff = new Date(this.profile.expiration).getTime() - Date.now();
    return Math.max(0, Math.ceil(diff / (1000 * 60 * 60 * 24)));
  }

  public logout(): void {
    this.authService.logout().subscribe({ next: () => this.router.navigate(['/login']), error: console.error });
  }
}

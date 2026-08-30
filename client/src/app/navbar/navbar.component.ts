import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
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
  private settingsService = inject(SettingsService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  public showMobileMenu = false;

  public profile: Profile;
  public readonly providerLink = 'https://alldebrid.com/account/';
  public version: string;

  constructor() {
    this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        this.showMobileMenu = false;
      }
    });
  }

  ngOnInit(): void {
    this.settingsService.getProfile().subscribe((result) => {
      this.profile = result;

      this.cdr.detectChanges();
    });

    this.settingsService.getVersion().subscribe((result) => {
      this.version = result.version;
      this.cdr.detectChanges();
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

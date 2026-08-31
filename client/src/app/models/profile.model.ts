export class Profile {
  public provider: string;
  public userName: string;
  public expiration: Date;
  public currentVersion: string;
  public latestVersion: string;
  public updateAvailable: boolean;
  public disableUpdateNotification: boolean;
}

import { Pipe, PipeTransform } from '@angular/core';
import { Torrent } from './models/torrent.model';

@Pipe({ name: 'filter', standalone: true })
export class FilterPipe implements PipeTransform {
  transform(torrents: Torrent[], search: string): Torrent[] {
    if (!search?.trim()) return torrents;
    const lower = search.toLowerCase();
    return torrents.filter(t => t.rdName?.toLowerCase().includes(lower));
  }
}

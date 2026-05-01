import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'decodeURI', })
export class DecodeURIPipe implements PipeTransform {
  transform(input: string) {
    return decodeURI(input);
  }
}

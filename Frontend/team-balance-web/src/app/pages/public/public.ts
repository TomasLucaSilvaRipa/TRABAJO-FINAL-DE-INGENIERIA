import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-public',
  imports: [],
  templateUrl: './public.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Public {}

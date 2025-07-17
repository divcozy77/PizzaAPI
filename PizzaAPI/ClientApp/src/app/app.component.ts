import { Component, signal } from '@angular/core';

@Component({
  selector: 'app-root',
  standalone: false,
  //templateUrl: './app.html',
  template: `
    <div class="container-fluid p-3">
      <router-outlet></router-outlet>
    </div>
  `,
  styleUrl: './app.scss'
})
export class AppComponent {
  protected readonly title = signal('pizza-dashboard');
}

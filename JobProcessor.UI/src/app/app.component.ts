import { Component } from '@angular/core';
import { FooterComponent } from './footer/footer.component';
import { HeaderComponent } from './header/header.component';
import { MaincontentComponent } from './maincontent/maincontent.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [HeaderComponent, MaincontentComponent, FooterComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {}

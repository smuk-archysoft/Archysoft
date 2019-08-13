import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import {
  AuthModule as AuthenticationModule,
  AUTH_SERVICE,
  PUBLIC_FALLBACK_PAGE_URI,
  PROTECTED_FALLBACK_PAGE_URI
} from 'ngx-auth';

import { AuthRoutingModule } from './auth-routing.module';
import { SharedModule } from '../shared/shared.module';
import { AuthService } from './services/auth.service';
import { TokenService } from './services/token.service';
import { AuthComponent } from './auth.component';
import { LoginComponent } from './components/login/login.component';
import { SignupComponent } from './components/signup/signup.component';
import { TranslateModule } from '@ngx-translate/core';
import { AuthNotificationComponent } from './components/auth-notification/auth-notification.component';
import { ConfirmEmailComponent } from './components/confirm-email/confirm-email.component';
import { ForgotPasswordComponent } from './components/forgot-password/forgot-password.component';
import { RecoverPasswordComponent } from './components/recover-password/recover-password.component';

export function factory(authService: AuthService) {
  return authService;
}

@NgModule({
  declarations: [
    AuthComponent, 
    LoginComponent, 
    SignupComponent, 
    AuthNotificationComponent, 
    ConfirmEmailComponent, 
    ForgotPasswordComponent, 
    RecoverPasswordComponent
  ],
  imports: [
    CommonModule,
    AuthRoutingModule,
    SharedModule,
    TranslateModule
  ],
  exports: [
    AuthenticationModule
  ],
  providers: [
    AuthService,
    TokenService,
    { provide: PROTECTED_FALLBACK_PAGE_URI, useValue: '/' },
    { provide: PUBLIC_FALLBACK_PAGE_URI, useValue: '/auth/login' },
    {
      provide: AUTH_SERVICE,
      deps: [AuthService],
      useFactory: factory
    }
  ]
})
export class AuthModule { }

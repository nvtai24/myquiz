import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../environments/environment';

export const baseUrlInterceptor: HttpInterceptorFn = (req, next) => {
  // Only intercept relative /api routes
  if (req.url.startsWith('/api')) {
    const apiUrl: string = (environment as any).apiUrl || '';
    const normalizedApiUrl = apiUrl.endsWith('/') ? apiUrl.slice(0, -1) : apiUrl;
      
    const apiReq = req.clone({ url: `${normalizedApiUrl}${req.url}` });
    return next(apiReq);
  }
  return next(req);
};

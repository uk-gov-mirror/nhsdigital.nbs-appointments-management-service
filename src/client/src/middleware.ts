import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

export function middleware(request: NextRequest) {
  // Encode the search params (the ?... part) so that ampersands (&)
  // are treated as text, not as separators for the login page parameters.
  const pathAndQuery = `${request.nextUrl.pathname}${encodeURIComponent(request.nextUrl.search)}`;

  const isApi = request.nextUrl.pathname.includes('/api/');

  if (!isApi) {
    const nonce = Buffer.from(crypto.randomUUID()).toString('base64');

    const csp =
      "default-src 'self'; " +
      "connect-src 'self' https://js.monitor.azure.com https://dc.services.visualstudio.com; " +
      `style-src 'self' 'nonce-${nonce}' https://assets.nhs.uk; ` +
      `font-src 'self' 'nonce-${nonce}' https://assets.nhs.uk; ` +
      `script-src 'self' 'nonce-${nonce}' 'strict-dynamic'; `
        //cleanup for performance
        .replace(/\s{2,}/g, ' ')
        .trim();

    const requestHeaders = new Headers(request.headers);
    requestHeaders.set('x-nonce', nonce);
    requestHeaders.set('Content-Security-Policy', csp);

    if (!request.nextUrl.pathname.endsWith('login')) {
      requestHeaders.set('mya-last-requested-path', pathAndQuery);
    }

    const response = NextResponse.next({
      headers: requestHeaders,
    });

    response.headers.set(
      'x-forwarded-host',
      request.headers.get('origin')?.replace(/(http|https):\/\//, '') || '*',
    );

    const isPage = request.headers.get('accept')?.includes('text/html');
    //only remove cache for page requests, leaving scripts and other content cached.
    if (isPage) {
      response.headers.set(
        'Cache-Control',
        'no-store, max-age=0, must-revalidate',
      );
    }

    return response;
  }
}

export const config = {
  matcher: [
    // All routes except /login, /api, /_next, and /favicon.ico
    '/((?!api|_next/static|_next/image|favicon.ico).*)',
  ],
};

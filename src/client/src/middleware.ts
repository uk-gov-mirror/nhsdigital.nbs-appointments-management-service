/* eslint-disable @typescript-eslint/no-non-null-assertion */
import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

export function middleware(request: NextRequest) {
  // Encode the search params (the ?... part) so that ampersands (&)
  // are treated as text, not as separators for the login page parameters.
  const pathAndQuery = `${request.nextUrl.pathname}${encodeURIComponent(request.nextUrl.search)}`;

  const nonce = Buffer.from(crypto.randomUUID()).toString('base64');

  const permittedConnectUrls = [
    process.env.NBS_API_BASE_URL!,
    process.env.AUTH_HOST!,
    process.env.MOCK_OIDC_SERVER_BASE_URL!,
    process.env.MOCK_AUTHENTICATION_ISSUER_URL!,
    'https://js.monitor.azure.com',
    'https://dc.services.visualstudio.com',
  ];

  const permittedAssetUrls = ['https://assets.nhs.uk'];

  const csp =
    "default-src 'self'; " +
    `connect-src 'self' ${permittedConnectUrls.join(' ')}; ` +
    `style-src 'self' 'nonce-${nonce}' ${permittedAssetUrls.join(' ')}; ` +
    `font-src 'self' 'nonce-${nonce}' ${permittedAssetUrls.join(' ')}; ` +
    `script-src 'self' 'nonce-${nonce}' 'strict-dynamic'; `
      //cleanup for performance
      .replace(/\s{2,}/g, ' ')
      .trim();

  const headers: HeadersInit = {
    'x-nonce': nonce,
    'Content-Security-Policy': csp,
  };

  if (!request.nextUrl.pathname.endsWith('login')) {
    headers['mya-last-requested-path'] = pathAndQuery;
  }

  const response = NextResponse.next({ headers });

  const isApi = request.nextUrl.pathname.includes('/api/');

  //TODO revert this back to what it was before?
  if (!isApi) {
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
  }

  return response;
}

export const config = {
  matcher: [
    // All routes except /login, /api, /_next, and /favicon.ico
    '/((?!api|_next/static|_next/image|favicon.ico).*)',
  ],
};

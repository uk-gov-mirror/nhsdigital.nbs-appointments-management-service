/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  env: {
    BUILD_NUMBER: process.env.BUILD_NUMBER ?? '',
  },
  output: 'standalone',
  basePath: process.env.CLIENT_BASE_PATH,
  headers: async () => {
    return [
      {
        source: '/:path*',
        headers: [
          {
            key: 'Strict-Transport-Security',
            value: 'max-age=31536000; includeSubDomains; preload',
          },
        ],
      },
      {
        key: 'X-Content-Type-Options',
        value: 'nosniff',
      },
    ];
  },
  redirects: async () => {
    return [
      {
        source: '/',
        basePath: false,
        destination: `${process.env.CLIENT_BASE_PATH}/sites`,
        permanent: true,
      },
      {
        source: '/manage-your-appointments/api/:path*',
        destination: `${process.env.AUTH_HOST}/api/:path*`, // Ensure API calls go to backend
        permanent: true,
      },
    ];
  },
};

export default nextConfig;


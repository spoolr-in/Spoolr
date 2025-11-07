
/** @type {import('next').NextConfig} */

const API_URL = process.env.NEXT_PUBLIC_API_URL;
console.log("--- Building with API_URL: ", API_URL, "---");

if (!API_URL) {
  throw new Error("Missing environment variable NEXT_PUBLIC_API_URL");
}

const nextConfig = {
  async rewrites() {
    return [
      {
        source: '/api/:path*',
        destination: `${API_URL}/api/:path*`,
      },
    ];
  },
  images: {
    domains: ['images.unsplash.com'],
  },
};

export default nextConfig;

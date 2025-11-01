
"use client";

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';

const RegisterVendorRedirect = () => {
  const router = useRouter();

  useEffect(() => {
    router.replace('/vendor/register');
  }, [router]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-900 text-white">
      <p>Redirecting to vendor registration...</p>
    </div>
  );
};

export default RegisterVendorRedirect;

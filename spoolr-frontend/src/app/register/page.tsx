'use client';

import { useRouter } from 'next/navigation';

const RegisterPage = () => {
  const router = useRouter();

  return (
    <div className="min-h-screen flex items-center justify-center bg-white text-gray-900 pt-16">
      <div className="max-w-md w-full bg-white p-8 rounded-lg shadow-lg border border-gray-200">
        <h2 className="text-3xl font-bold text-center text-[#6C2EFF] mb-8">Join PrintWave</h2>
        <div className="space-y-4">
          <button
            onClick={() => router.push('/register-user')}
            className="w-full flex justify-center py-3 px-4 border border-transparent rounded-none shadow-sm text-sm font-medium text-white bg-[#6C2EFF] hover:bg-[#5A25CC] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[#6C2EFF] transition-transform transform hover:scale-105"
          >
            Register as a User
          </button>
          <button
            onClick={() => router.push('/register-vendor')}
            className="w-full flex justify-center py-3 px-4 border border-[#6C2EFF] rounded-none shadow-sm text-sm font-medium text-[#6C2EFF] bg-transparent hover:bg-[#6C2EFF]/10 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[#6C2EFF] transition-transform transform hover:scale-105"
          >
            Register as a Vendor
          </button>
        </div>
      </div>
    </div>
  );
};

export default RegisterPage;

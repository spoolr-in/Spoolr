'use client';

import { useState, useEffect, useRef } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { verifyEmail } from '@/lib/api';
import axios from 'axios';
import { CheckCircle, XCircle, Loader2 } from 'lucide-react';

const VerifyEmailPage = () => {
  const [loading, setLoading] = useState(true);
  const [success, setSuccess] = useState(false);
  const [message, setMessage] = useState('');
  const router = useRouter();
  const searchParams = useSearchParams();
  const verificationAttempted = useRef(false); // The guard

  useEffect(() => {
    const token = searchParams.get('token');

    if (token) {
      // Prevent React.StrictMode double-invocation in development
      if (verificationAttempted.current) {
        return;
      }
      verificationAttempted.current = true;

      verifyEmail(token)
        .then(response => {
          setSuccess(true);
          setMessage(response.data.message || 'Your email has been successfully verified!');
        })
        .catch(err => {
          setSuccess(false);
          let errorMessage = 'Failed to verify email. The link might be invalid or expired.';
          if (axios.isAxiosError(err) && err.response) {
            if (err.response.data && typeof err.response.data === 'object' && err.response.data.error) {
              errorMessage = err.response.data.error;
            } else if (typeof err.response.data === 'string') {
              errorMessage = err.response.data;
            }
          }
          setMessage(errorMessage);
        })
        .finally(() => {
          setLoading(false);
        });
    } else {
      setSuccess(false);
      setMessage('No verification token found in the URL.');
      setLoading(false);
    }
  }, [searchParams]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-white text-gray-900 pt-16">
      <div className="max-w-md w-full bg-white p-8 rounded-lg shadow-lg border border-gray-200 text-center">
        {loading ? (
          <div className="flex flex-col items-center">
            <Loader2 className="h-12 w-12 animate-spin text-[#6C2EFF] mb-4" />
            <p className="text-lg">Verifying your email...</p>
          </div>
        ) : (
          <div className="flex flex-col items-center">
            {success ? (
              <CheckCircle className="h-12 w-12 text-green-500 mb-4" />
            ) : (
              <XCircle className="h-12 w-12 text-red-500 mb-4" />
            )}
            <h2 className="text-2xl font-bold text-[#6C2EFF] mb-4">{message}</h2>
            <button
              onClick={() => router.push(success ? '/login' : '/register')}
              className="mt-6 w-full flex justify-center py-3 px-4 border border-transparent rounded-none shadow-sm text-sm font-medium text-white bg-[#6C2EFF] hover:bg-[#5A25CC] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[#6C2EFF] transition-transform transform hover:scale-105"
            >
              {success ? 'Go to Login' : 'Go to Registration'}
            </button>
          </div>
        )}
      </div>
    </div>
  );
};

export default VerifyEmailPage;

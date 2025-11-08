"use client";

import { useEffect, useState, Suspense, useRef } from 'react';
import { useSearchParams } from 'next/navigation';
import axios from 'axios';
import { CheckCircle, XCircle, Download } from 'lucide-react';

const VerifyEmailContent = () => {
  const searchParams = useSearchParams();
  const token = searchParams.get('token');
  const [status, setStatus] = useState<'verifying' | 'success' | 'error'>('verifying');
  const [message, setMessage] = useState('Verifying your email address...');
  const verificationAttempted = useRef(false); // The 1000-year guard

  useEffect(() => {
    if (!token) {
      setStatus('error');
      setMessage('Verification token is missing. Please check the link in your email.');
      return;
    }

    // Prevent React.StrictMode double-invocation in development
    if (verificationAttempted.current) {
      return;
    }
    verificationAttempted.current = true;

    const verifyToken = async () => {
      try {
        const response = await axios.get(`/api/vendors/verify-email?token=${token}`);
        setStatus('success');
        setMessage(response.data.message || 'Email verified successfully! You can now log in.');
      } catch (error: any) {
        setStatus('error');
        setMessage(error.response?.data?.message || 'An error occurred during verification. Please try again.');
      }
    };

    verifyToken();
  }, [token]);

  const handleDownload = () => {
    // Serve the installer from the Next.js public folder
    window.location.href = '/SpoolrStationSetup.exe';
  };

  return (
    <div className="min-h-screen bg-white text-gray-900 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full bg-white p-10 rounded-xl shadow-2xl border border-gray-200 space-y-6 text-center">
        {status === 'verifying' && (
          <>
            <div className="animate-spin rounded-full h-16 w-16 border-t-4 border-b-4 border-[#6C2EFF] mx-auto"></div>
            <h2 className="text-2xl font-bold text-gray-800">{message}</h2>
          </>
        )}

        {status === 'success' && (
          <>
            <CheckCircle className="h-16 w-16 text-green-500 mx-auto" />
            <h2 className="text-3xl font-bold text-gray-800">Email Verified!</h2>
            <p className="text-gray-600 px-4">Your email has been successfully verified. Please download the Spoolr Station App to complete your setup and manage your print business.</p>
            
            <div className="pt-6">
              <h3 className="text-xl font-semibold text-[#6C2EFF] mb-2">Next Step: Download the Station App</h3>
              <p className="text-gray-600 mb-4">The Spoolr Station App is required to manage your print jobs and store efficiently.</p>
              <button 
                onClick={handleDownload}
                className="w-full flex items-center justify-center py-3 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-[#6C2EFF] hover:bg-[#5A25CC] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[#6C2EFF] transition-transform transform hover:scale-105"
              >
                <Download className="mr-2 -ml-1 h-5 w-5" />
                Download Station App
              </button>
            </div>
          </>
        )}

        {status === 'error' && (
          <>
            <XCircle className="h-16 w-16 text-red-500 mx-auto" />
            <h2 className="text-3xl font-bold text-gray-800">Verification Failed</h2>
            <p className="text-gray-600">{message}</p>
          </>
        )}
      </div>
    </div>
  );
};


const VerifyEmailPage = () => {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <VerifyEmailContent />
    </Suspense>
  )
}


export default VerifyEmailPage;

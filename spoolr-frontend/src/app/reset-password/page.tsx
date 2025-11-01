'use client';

import { useState, useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { resetPassword } from '@/lib/api';
import axios from 'axios';

const ResetPasswordPage = () => {
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [token, setToken] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const router = useRouter();
  const searchParams = useSearchParams();

  useEffect(() => {
    const tokenFromUrl = searchParams.get('token');
    if (tokenFromUrl) {
      setToken(tokenFromUrl);
    } else {
      setError('No reset token found in the URL. Please request a new password reset.');
    }
  }, [searchParams]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setMessage(null);
    setLoading(true);

    if (newPassword !== confirmPassword) {
      setError('Passwords do not match.');
      setLoading(false);
      return;
    }

    if (!token) {
      setError('Missing password reset token.');
      setLoading(false);
      return;
    }

    try {
      const response = await resetPassword(token, newPassword);
      setMessage(response.data.message || 'Your password has been successfully reset!');
    } catch (err) {
      let errorMessage = 'Failed to reset password. Please try again.';
      if (axios.isAxiosError(err) && err.response) {
        if (err.response.data && typeof err.response.data === 'object' && err.response.data.error) {
          errorMessage = err.response.data.error;
        } else if (typeof err.response.data === 'string') {
          errorMessage = err.response.data;
        }
      }
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-white text-gray-900 pt-16">
      <div className="max-w-md w-full bg-white p-8 rounded-lg shadow-lg border border-gray-200">
        <h2 className="text-3xl font-bold text-center text-[#6C2EFF] mb-8">Reset Your Password</h2>
        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label htmlFor="newPassword" className="block text-sm font-medium text-gray-700">
              New Password
            </label>
            <input
              id="newPassword"
              name="newPassword"
              type="password"
              autoComplete="new-password"
              required
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              className="mt-1 block w-full px-3 py-2 bg-gray-50 border border-gray-300 rounded-md text-gray-900 placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-[#6C2EFF] focus:border-[#6C2EFF] transition"
              disabled={loading}
            />
          </div>
          <div>
            <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-700">
              Confirm New Password
            </label>
            <input
              id="confirmPassword"
              name="confirmPassword"
              type="password"
              autoComplete="new-password"
              required
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              className="mt-1 block w-full px-3 py-2 bg-gray-50 border border-gray-300 rounded-md text-gray-900 placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-[#6C2EFF] focus:border-[#6C2EFF] transition"
              disabled={loading}
            />
          </div>

          {message && (
            <div className="bg-green-500/20 border border-green-500 text-green-300 px-4 py-3 rounded-md text-sm">
              <p>{message}</p>
              <button
                onClick={() => router.push('/login')}
                className="mt-4 w-full flex justify-center py-2 px-4 border border-transparent rounded-none shadow-sm text-sm font-medium text-white bg-[#6C2EFF] hover:bg-[#5A25CC] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[#6C2EFF] transition-transform transform hover:scale-105"
              >
                Go to Login
              </button>
            </div>
          )}

          {error && (
            <div className="bg-red-500/20 border border-red-500 text-red-300 px-4 py-3 rounded-md text-sm">
              <p>{error}</p>
            </div>
          )}

          <div>
            <button
              type="submit"
              className="w-full flex justify-center py-3 px-4 border border-transparent rounded-none shadow-sm text-sm font-medium text-white bg-[#6C2EFF] hover:bg-[#5A25CC] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[#6C2EFF] transition-transform transform hover:scale-105 disabled:bg-gray-400 disabled:scale-100"
              disabled={loading}
            >
              {loading ? 'Resetting...' : 'Reset Password'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ResetPasswordPage;
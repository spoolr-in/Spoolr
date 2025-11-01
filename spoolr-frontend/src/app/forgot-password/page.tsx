'use client';

import { useState } from 'react';
import { requestPasswordReset } from '@/lib/api';
import axios from 'axios';

const ForgotPasswordPage = () => {
  const [email, setEmail] = useState('');
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setMessage(null);
    setLoading(true);

    if (!email) {
      setError('Email address is required');
      setLoading(false);
      return;
    }

    try {
      await requestPasswordReset(email);
      setMessage('If an account with that email exists, a password reset link has been sent.');
    } catch (err) {
      let errorMessage = 'Failed to send reset link. Please try again.';
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
        <h2 className="text-3xl font-bold text-center text-[#6C2EFF] mb-8">Forgot Password</h2>
        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label htmlFor="email" className="block text-sm font-medium text-gray-700">
              Email Address
            </label>
            <input
              id="email"
              name="email"
              type="email"
              autoComplete="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="mt-1 block w-full px-3 py-2 bg-gray-50 border border-gray-300 rounded-md text-gray-900 placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-[#6C2EFF] focus:border-[#6C2EFF] transition"
              disabled={loading}
            />
          </div>

          {message && (
            <div className="bg-green-500/20 border border-green-500 text-green-300 px-4 py-3 rounded-md text-sm">
              <p>{message}</p>
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
              {loading ? 'Sending...' : 'Send Reset Link'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ForgotPasswordPage;
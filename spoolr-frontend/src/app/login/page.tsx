'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { login } from '@/lib/api';
import axios from 'axios';

const LoginPage = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const router = useRouter();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    if (!email || !password) {
      setError('Email and password are required');
      setLoading(false);
      return;
    }

    try {
      const response = await login(email, password);
      localStorage.setItem('token', response.data.token);
      router.push('/dashboard');
    } catch (err) {
      let errorMessage = 'Login failed. Please try again.';
      if (axios.isAxiosError(err) && err.response) {
        if (err.response.data && typeof err.response.data === 'object' && err.response.data.message) {
          errorMessage = err.response.data.message;
        } else if (typeof err.response.data === 'string') {
          errorMessage = err.response.data;
        }
      }
      setError(errorMessage);
      setPassword(''); // Clear password on failure
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-white text-gray-900 pt-16">
      <div className="max-w-md w-full bg-white p-8 rounded-lg shadow-lg border border-gray-200">
        <h2 className="text-3xl font-bold text-center text-[#6C2EFF] mb-8">Welcome Back</h2>
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
          <div>
            <label htmlFor="password" className="block text-sm font-medium text-gray-700">
              Password
            </label>
            <input
              id="password"
              name="password"
              type="password"
              autoComplete="current-password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="mt-1 block w-full px-3 py-2 bg-gray-50 border border-gray-300 rounded-md text-gray-900 placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-[#6C2EFF] focus:border-[#6C2EFF] transition"
              disabled={loading}
            />
          </div>

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
              {loading ? 'Logging in...' : 'Login'}
            </button>
          </div>
        </form>
        <div className="text-center mt-4">
          <Link href="/forgot-password" className="text-sm text-gray-400 hover:text-cyan-400 transition-colors">
            Forgot your password?
          </Link>
        </div>
         <div className="text-center mt-4">
          <Link href="/register" className="text-sm text-gray-400 hover:text-cyan-400 transition-colors">
            Don't have an account? Register
          </Link>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { register } from '@/lib/api';
import axios from 'axios';

const RegisterUserPage = () => {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);
  const router = useRouter();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    if (!name || !email || !password) {
      setError('All fields are required');
      setLoading(false);
      return;
    }

    try {
      await register(name, email, password);
      setSuccess(true);
    } catch (err) {
      let errorMessage = 'Registration failed. Please try again.';
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

  if (success) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-white text-gray-900 pt-16">
        <div className="max-w-md w-full bg-white p-8 rounded-lg shadow-lg border border-gray-200 text-center">
          <h2 className="text-3xl font-bold text-[#6C2EFF] mb-4">Registration Successful!</h2>
          <p className="text-gray-700 mb-6">Please check your email to verify your account.</p>
          <button
            onClick={() => router.push('/login')}
            className="w-full flex justify-center py-3 px-4 border border-transparent rounded-none shadow-sm text-sm font-medium text-white bg-[#6C2EFF] hover:bg-[#5A25CC] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[#6C2EFF] transition-transform transform hover:scale-105"
          >
            Go to Login
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-white text-gray-900 pt-16">
      <div className="max-w-md w-full bg-white p-8 rounded-lg shadow-lg border border-gray-200">
        <h2 className="text-3xl font-bold text-center text-[#6C2EFF] mb-8">Create Your Account</h2>
        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label htmlFor="name" className="block text-sm font-medium text-gray-700">
              Full Name
            </label>
            <input
              id="name"
              name="name"
              type="text"
              autoComplete="name"
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="mt-1 block w-full px-3 py-2 bg-gray-50 border border-gray-300 rounded-md text-gray-900 placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-[#6C2EFF] focus:border-[#6C2EFF] transition"
              disabled={loading}
            />
          </div>
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
              autoComplete="new-password"
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
              {loading ? 'Registering...' : 'Register'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default RegisterUserPage;
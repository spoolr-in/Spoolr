"use client";

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import axios from 'axios';
import { KeyRound, Lock, LogIn } from 'lucide-react';

const ActivateVendorPage = () => {
  const router = useRouter();
  const [activationKey, setActivationKey] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    if (newPassword.length < 8) {
        setError("Password must be at least 8 characters long.");
        setLoading(false);
        return;
    }

    try {
      const response = await axios.post('/api/vendors/first-time-login', {
        activationKey,
        newPassword,
      });

      // Assuming the backend returns a token and user data
      const { token } = response.data;
      if (token) {
        localStorage.setItem('token', token);
        // Redirect to the vendor dashboard after successful activation
        router.push('/vendor/dashboard');
      } else {
        throw new Error("Activation successful, but no token received.");
      }

    } catch (err: any) {
      setError(err.response?.data?.message || 'An error occurred during activation. Please check your key and try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-white text-gray-900 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full bg-white p-10 rounded-xl shadow-2xl border border-gray-200 space-y-8">
        <div>
          <h2 className="text-center text-4xl font-bold text-[#6C2EFF]">Activate Your Account</h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            Enter the Activation Key from your email and set your new password.
          </p>
        </div>
        
        {error && <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded-lg relative">{error}</div>}
        
        <form onSubmit={handleSubmit} className="space-y-6">
          <div className="relative">
            <KeyRound className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" />
            <input 
              type="text" 
              name="activationKey" 
              placeholder="Activation Key" 
              value={activationKey} 
              onChange={(e) => setActivationKey(e.target.value)} 
              required 
              className="input-style pl-10"
            />
          </div>

          <div className="relative">
            <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" />
            <input 
              type="password" 
              name="newPassword" 
              placeholder="New Password (min. 8 characters)" 
              value={newPassword} 
              onChange={(e) => setNewPassword(e.target.value)} 
              required 
              className="input-style pl-10"
            />
          </div>

          <div>
            <button 
              type="submit" 
              disabled={loading} 
              className="w-full flex justify-center items-center py-3 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-[#6C2EFF] hover:bg-[#5A25CC] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[#6C2EFF] transition-transform transform hover:scale-105 disabled:bg-gray-400 disabled:scale-100"
            >
              {loading ? 'Activating...' : <><LogIn className="mr-2 h-5 w-5"/>Activate & Log In</>}
            </button>
          </div>
        </form>
      </div>
      <style jsx>{`
        .input-style {
          margin-top: 0.25rem;
          display: block;
          width: 100%;
          padding: 0.75rem 1rem;
          background-color: #f9fafb;
          border: 1px solid #d1d5db;
          border-radius: 0.375rem;
          color: #111827;
          placeholder-color: #6b7280;
        }
        .input-style:focus {
          outline: none;
          --tw-ring-color: #6C2EFF;
          border-color: #6C2EFF;
        }
      `}</style>
    </div>
  );
};

export default ActivateVendorPage;

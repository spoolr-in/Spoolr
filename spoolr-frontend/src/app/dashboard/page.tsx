'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { getProfile } from '@/lib/api';
import axios from 'axios';
import { User, ShoppingCart, CloudUpload } from 'lucide-react';

interface UserData {
  email: string;
  userId: string;
  role: string;
  name: string;
}

interface DashboardData {
  welcomeMessage: string;
  totalOrders: number;
  accountStatus: string;
  details: string;
}

const DashboardPage = () => {
  const router = useRouter();
  const [userData, setUserData] = useState<UserData>({
    email: '',
    userId: '',
    role: '',
    name: '',
  });

  const [dashboardData, setDashboardData] = useState<DashboardData>({
    welcomeMessage: '',
    totalOrders: 0,
    accountStatus: '',
    details: 'Loading dashboard data...',
  });

  useEffect(() => {
    const fetchProfile = async () => {
      const token = localStorage.getItem('token');
      if (!token) {
        router.push('/login');
        return;
      }

      try {
        const response = await getProfile(token);
        if (response.status === 200) {
          const profile = response.data;
          setUserData({
            email: profile.email,
            userId: profile.id,
            role: profile.role,
            name: profile.name,
          });
          setDashboardData({
            welcomeMessage: `Welcome, ${profile.name}!`,
            totalOrders: profile.totalOrders || 0,
            accountStatus: profile.emailVerified ? 'Verified' : 'Pending Verification',
            details: profile.message || '',
          });
        } else {
          console.error('Failed to fetch profile:', response.data);
          setDashboardData(prev => ({ ...prev, details: 'Failed to load dashboard data.' }));
        }
      } catch (error) {
        console.error('Error fetching profile:', error);
        setDashboardData(prev => ({ ...prev, details: 'Error loading dashboard data.' }));
        if (axios.isAxiosError(error) && error.response?.status === 401) {
          router.push('/login');
        }
      }
    };
    fetchProfile();
  }, [router]);

  return (
    <div className="min-h-screen bg-white text-gray-900 pt-16">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="flex justify-between items-center mb-8">
          <h1 className="text-4xl font-bold text-[#6C2EFF]">Dashboard</h1>
          <Link href="/dashboard/submit-job" legacyBehavior>
            <a className="inline-block bg-[#6C2EFF] text-white font-bold py-3 px-6 rounded-lg shadow-lg hover:bg-[#5A25CC] transition-transform transform hover:scale-105 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[#6C2EFF]">
              <div className="flex items-center">
                <CloudUpload className="h-6 w-6 mr-2" />
                <span>Upload New Job</span>
              </div>
            </a>
          </Link>
        </div>

        <div className="bg-white p-8 rounded-lg shadow-lg border border-gray-200">
          <h2 className="text-3xl font-bold text-gray-800 mb-6">{dashboardData.welcomeMessage}</h2>
          <p className="text-gray-700 mb-8">{dashboardData.details}</p>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="bg-gray-50 p-6 rounded-lg border border-gray-200 flex items-center space-x-4">
              <User className="h-10 w-10 text-[#6C2EFF]" />
              <div>
                <h3 className="text-xl font-semibold text-gray-900">Your Account</h3>
                <p className="text-gray-700">Email: {userData.email}</p>
                <p className="text-gray-700">User ID: {userData.userId}</p>
                <p className="text-gray-700">Role: {userData.role}</p>
              </div>
            </div>
            <div className="bg-gray-50 p-6 rounded-lg border border-gray-200 flex items-center space-x-4">
              <ShoppingCart className="h-10 w-10 text-[#6C2EFF]" />
              <div>
                <h3 className="text-xl font-semibold text-gray-900">Order Summary</h3>
                <p className="text-gray-700">Total Orders: {dashboardData.totalOrders}</p>
                <p className="text-gray-700">Account Status: {dashboardData.accountStatus}</p>
              </div>
            </div>
          </div>

          <p className="text-gray-500 text-center mt-8 text-sm">
            This is a temporary dashboard. Full functionality will be available after API integration.
          </p>
        </div>
      </div>
    </div>
  );
};

export default DashboardPage;

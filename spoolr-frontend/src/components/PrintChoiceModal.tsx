'use client';

import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';

const PrintChoiceModal = ({ onClose }) => {
  const router = useRouter();
  const [isLoggedIn, setIsLoggedIn] = useState(false);

  useEffect(() => {
    // In a real app, you might use a global state or context.
    // For now, we'll check localStorage as planned.
    const token = localStorage.getItem('jwt_token');
    setIsLoggedIn(!!token);
  }, []);

  const handleFastPrint = () => {
    router.push('/fast-print/scan');
  };

  const handleAuthenticatedPath = () => {
    if (isLoggedIn) {
      router.push('/dashboard');
    } else {
      router.push('/login');
    }
  };

  return (
    <div 
      className="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center z-50"
      onClick={onClose}
    >
      <div 
        className="bg-white rounded-lg shadow-2xl p-8 w-full max-w-md mx-4"
        onClick={(e) => e.stopPropagation()} // Prevent closing when clicking inside
      >
        <h2 className="text-2xl font-bold text-center text-gray-800 mb-6">Choose Your Print Method</h2>
        
        <div className="space-y-4">
          <button 
            onClick={handleFastPrint}
            className="w-full text-white bg-indigo-600 hover:bg-indigo-700 focus:ring-4 focus:ring-indigo-300 font-medium rounded-lg text-lg px-5 py-3.5 text-center transition duration-300 ease-in-out transform hover:scale-105"
          >
            🖨️ Fast Print (Anonymous)
          </button>

          <button 
            onClick={handleAuthenticatedPath}
            className="w-full text-white bg-gray-700 hover:bg-gray-800 focus:ring-4 focus:ring-gray-300 font-medium rounded-lg text-lg px-5 py-3.5 text-center transition duration-300 ease-in-out transform hover:scale-105"
          >
            {isLoggedIn ? '👤 Go to Dashboard' : '🔒 Login & Print'}
          </button>
        </div>

        <button 
          onClick={onClose}
          className="absolute top-3 right-3 text-gray-500 hover:text-gray-800"
        >
          <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12"></path></svg>
        </button>
      </div>
    </div>
  );
};

export default PrintChoiceModal;

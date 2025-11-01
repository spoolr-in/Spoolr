
"use client";

import React from 'react';

interface SuccessModalProps {
  message: string;
  onClose: () => void;
}

const SuccessModal: React.FC<SuccessModalProps> = ({ message, onClose }) => {
  return (
    <div className="fixed inset-0 bg-black bg-opacity-75 flex items-center justify-center z-50 p-4">
      <div className="bg-white p-8 rounded-lg shadow-xl text-center max-w-sm w-full border border-gray-200">
        <h2 className="text-3xl font-bold text-green-600 mb-4">Success!</h2>
        <p className="text-gray-800 mb-6">{message}</p>
        <button
          onClick={onClose}
          className="py-2 px-6 bg-[#6C2EFF] hover:bg-[#5A25CC] text-white font-semibold rounded-lg transition-colors duration-300"
        >
          Close
        </button>
      </div>
    </div>
  );
};

export default SuccessModal;

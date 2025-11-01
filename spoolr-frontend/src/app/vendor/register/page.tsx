"use client";

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import dynamic from 'next/dynamic';
import SuccessModal from '@/components/SuccessModal';

const LocationPickerMap = dynamic(() => import('@/components/LocationPickerMap'), { 
  ssr: false 
});

const VendorRegisterPage = () => {
  const router = useRouter();
  const [formData, setFormData] = useState({
    email: '',
    businessName: '',
    contactPersonName: '',
    phoneNumber: '',
    businessAddress: '',
    city: '',
    state: '',
    zipCode: '',
    latitude: 0,
    longitude: 0,
    pricePerPageBWSingleSided: '',
    pricePerPageBWDoubleSided: '',
    pricePerPageColorSingleSided: '',
    pricePerPageColorDoubleSided: '',
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [showSuccessModal, setShowSuccessModal] = useState(false); // State to control modal visibility

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData({ ...formData, [name]: value });
  };

  const handleLocationChange = (lat: number, lng: number) => {
    setFormData({ ...formData, latitude: lat, longitude: lng });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setSuccess(null);

    try {
      const response = await fetch('/api/vendors/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(formData),
      });

      const result = await response.json();

      if (!response.ok) {
        throw new Error(result.message || 'An unknown error occurred.');
      }

      setSuccess(result.message);
      setShowSuccessModal(true); // Show the modal on success
      // Reset form data after successful submission
      setFormData({
        email: '',
        businessName: '',
        contactPersonName: '',
        phoneNumber: '',
        businessAddress: '',
        city: '',
        state: '',
        zipCode: '',
        latitude: 0,
        longitude: 0,
        pricePerPageBWSingleSided: '',
        pricePerPageBWDoubleSided: '',
        pricePerPageColorSingleSided: '',
        pricePerPageColorDoubleSided: '',
      });
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-white text-gray-900 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
      {showSuccessModal && (
        <SuccessModal
          title="Registration Successful!"
          message="A verification link has been sent to your email address. Please check your inbox (and spam folder) to activate your account."
          onClose={() => setShowSuccessModal(false)}
        />
      )}
      <div className="max-w-4xl w-full bg-white p-10 rounded-xl shadow-2xl border border-gray-200 space-y-10">
        <h2 className="text-4xl font-bold text-center text-[#6C2EFF]">Register Your Business</h2>
        
        {error && <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded-lg relative">{error}</div>}
        
        <form onSubmit={handleSubmit} className="space-y-10">
          {/* Business Details */}
          <div className="p-8 border rounded-xl shadow-inner bg-gray-50/50">
            <h3 className="text-2xl font-bold mb-6 text-[#6C2EFF]">Business Details</h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <input type="email" name="email" placeholder="Business Email" value={formData.email} onChange={handleInputChange} required className="input-style" />
              <input type="text" name="businessName" placeholder="Business Name" value={formData.businessName} onChange={handleInputChange} required className="input-style" />
              <input type="text" name="contactPersonName" placeholder="Contact Person" value={formData.contactPersonName} onChange={handleInputChange} required className="input-style" />
              <input type="tel" name="phoneNumber" placeholder="Phone Number" value={formData.phoneNumber} onChange={handleInputChange} required className="input-style" />
              <input type="text" name="businessAddress" placeholder="Street Address" value={formData.businessAddress} onChange={handleInputChange} required className="input-style" />
              <input type="text" name="city" placeholder="City" value={formData.city} onChange={handleInputChange} required className="input-style" />
              <input type="text" name="state" placeholder="State / Province" value={formData.state} onChange={handleInputChange} required className="input-style" />
              <input type="text" name="zipCode" placeholder="ZIP / Postal Code" value={formData.zipCode} onChange={handleInputChange} required className="input-style" />
            </div>
          </div>

          {/* Location Picker */}
          <div className="p-8 border rounded-xl shadow-inner bg-gray-50/50">
            <h3 className="text-2xl font-bold mb-6 text-[#6C2EFF]">Store Location</h3>
            <p className="text-gray-600 mb-4">Click on the map to set your location, or drag the marker.</p>
            <LocationPickerMap onLocationChange={handleLocationChange} />
            <div className="flex justify-between mt-4 text-sm font-medium text-gray-700">
              <span>Latitude: {formData.latitude.toFixed(6)}</span>
              <span>Longitude: {formData.longitude.toFixed(6)}</span>
            </div>
          </div>

          {/* Pricing */}
          <div className="p-8 border rounded-xl shadow-inner bg-gray-50/50">
            <h3 className="text-2xl font-bold mb-6 text-[#6C2EFF]">Service Pricing (per page)</h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <input type="number" name="pricePerPageBWSingleSided" placeholder="B&W Single-Sided" value={formData.pricePerPageBWSingleSided} onChange={handleInputChange} required className="input-style" step="0.01" />
              <input type="number" name="pricePerPageBWDoubleSided" placeholder="B&W Double-Sided" value={formData.pricePerPageBWDoubleSided} onChange={handleInputChange} required className="input-style" step="0.01" />
              <input type="number" name="pricePerPageColorSingleSided" placeholder="Color Single-Sided" value={formData.pricePerPageColorSingleSided} onChange={handleInputChange} required className="input-style" step="0.01" />
              <input type="number" name="pricePerPageColorDoubleSided" placeholder="Color Double-Sided" value={formData.pricePerPageColorDoubleSided} onChange={handleInputChange} required className="input-style" step="0.01" />
            </div>
          </div>

          <button type="submit" disabled={loading} className="w-full flex justify-center py-3 px-4 border border-transparent rounded-none shadow-sm text-sm font-medium text-white bg-[#6C2EFF] hover:bg-[#5A25CC] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[#6C2EFF] transition-transform transform hover:scale-105 disabled:bg-gray-400 disabled:scale-100">
            {loading ? 'Registering...' : 'Register Business'}
          </button>
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

export default VendorRegisterPage;
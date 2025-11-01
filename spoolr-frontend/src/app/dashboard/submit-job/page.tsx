"use client";

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import axios from 'axios';
import { Upload, MapPin, Printer, DollarSign, CheckCircle, XCircle, Loader2, PackageCheck } from 'lucide-react';
import LocationPickerMap from '@/components/LocationPickerMap';

// Modal Component
const JobSubmittedModal = ({ isOpen, onClose, trackingCode, totalPrice }) => {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-60 flex justify-center items-center z-50">
      <div className="bg-white p-10 rounded-2xl shadow-2xl max-w-md w-full text-center transform transition-all scale-100">
        <PackageCheck className="h-20 w-20 text-green-500 mx-auto mb-4" />
        <h2 className="text-3xl font-bold text-gray-800 mb-2">Job Submitted!</h2>
        <p className="text-gray-600 mb-6">Your file has been successfully submitted for printing.</p>
        <div className="bg-gray-100 p-4 rounded-lg mb-6 text-left">
          <p className="text-sm text-gray-500">Tracking Code</p>
          <p className="text-lg font-mono text-gray-900">{trackingCode}</p>
          <p className="text-sm text-gray-500 mt-2">Total Price</p>
          <p className="text-lg font-bold text-gray-900">₹{totalPrice.toFixed(2)}</p>
        </div>
        <div className="flex flex-col sm:flex-row gap-4">
          <Link href={`/track/${trackingCode}`} legacyBehavior>
            <a className="w-full flex items-center justify-center py-3 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-[#6C2EFF] hover:bg-[#5A25CC] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[#6C2EFF] transition-transform transform hover:scale-105">
              Track Job
            </a>
          </Link>
          <button 
            onClick={onClose}
            className="w-full flex items-center justify-center py-3 px-4 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-400 transition-colors"
          >
            Submit Another Job
          </button>
        </div>
      </div>
    </div>
  );
};


const SubmitJobPage = () => {
  const router = useRouter();
  const [file, setFile] = useState<File | null>(null);
  const [paperSize, setPaperSize] = useState('A4');
  const [isColor, setIsColor] = useState(false);
  const [isDoubleSided, setIsDoubleSided] = useState(false);
  const [copies, setCopies] = useState(1);
  const [customerLatitude, setCustomerLatitude] = useState(0.0);
  const [customerLongitude, setCustomerLongitude] = useState(0.0);

  const handleLocationChange = (lat: number, lng: number) => {
    setCustomerLatitude(lat);
    setCustomerLongitude(lng);
  };

  const [quotes, setQuotes] = useState<any[]>([]);
  const [selectedVendorId, setSelectedVendorId] = useState<number | null>(null);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [submittedJobInfo, setSubmittedJobInfo] = useState({ trackingCode: '', totalPrice: 0 });

  // Hardcoded options for now, can be fetched from backend if an endpoint exists
  const paperSizeOptions = ['A4', 'A3', 'LETTER', 'LEGAL'];

  // Function to get current location (for initial setup)
  useEffect(() => {
    const token = localStorage.getItem('token');
    if (!token) {
      router.push('/login');
      return;
    }

    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          setCustomerLatitude(position.coords.latitude);
          setCustomerLongitude(position.coords.longitude);
        },
        (err) => {
          console.error("Geolocation error:", err);
          setError("Could not get your location. Please enter manually.");
          // Default to a known location if geolocation fails (e.g., Bangalore)
          setCustomerLatitude(12.9716);
          setCustomerLongitude(77.5946);
        }
      );
    } else {
      setError("Geolocation is not supported by your browser. Please enter manually.");
      // Default to a known location if geolocation not supported
      setCustomerLatitude(12.9716);
      setCustomerLongitude(77.5946);
    }
  }, [router]);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      setFile(e.target.files[0]);
    }
  };

  const getAuthHeaders = () => ({
    headers: {
      'Authorization': `Bearer ${localStorage.getItem('token')}`,
      'Content-Type': 'multipart/form-data',
    },
  });

  const resetForm = () => {
    setFile(null);
    setQuotes([]);
    setSelectedVendorId(null);
    setError(null);
    setCopies(1);
    setIsColor(false);
    setIsDoubleSided(false);
  };

  const handleGetQuote = async () => {
    setError(null);
    setLoading(true);
    setQuotes([]);
    setSelectedVendorId(null);

    if (!file) {
      setError("Please select a file to get a quote.");
      setLoading(false);
      return;
    }

    const formData = new FormData();
    formData.append('file', file);
    formData.append('paperSize', paperSize);
    formData.append('isColor', String(isColor));
    formData.append('isDoubleSided', String(isDoubleSided));
    formData.append('copies', String(copies));
    formData.append('customerLatitude', String(customerLatitude));
    formData.append('customerLongitude', String(customerLongitude));

    try {
      const response = await axios.post('/api/jobs/quote', formData);
      if (response.data.success) {
        setQuotes(response.data.vendors);
        if (response.data.vendors.length === 0) {
          setError("No vendors available for this job with the selected criteria.");
        }
      } else {
        setError(response.data.error || "Failed to get quotes.");
      }
    } catch (err: any) {
      setError(err.response?.data?.error || "An error occurred while getting quotes.");
    } finally {
      setLoading(false);
    }
  };

  const handleSubmitJob = async () => {
    setError(null);
    setLoading(true);

    if (!file) {
      setError("Please select a file to submit the job.");
      setLoading(false);
      return;
    }
    if (quotes.length === 0) {
      setError("Please get a quote first.");
      setLoading(false);
      return;
    }
    // If no specific vendor selected, pick the first one from quotes
    const finalVendorId = selectedVendorId || (quotes.length > 0 ? quotes[0].vendorId : null);
    if (!finalVendorId) {
      setError("No vendor selected or available for job submission.");
      setLoading(false);
      return;
    }

    const formData = new FormData();
    formData.append('file', file);
    formData.append('paperSize', paperSize);
    formData.append('isColor', String(isColor));
    formData.append('isDoubleSided', String(isDoubleSided));
    formData.append('copies', String(copies));
    formData.append('customerLatitude', String(customerLatitude));
    formData.append('customerLongitude', String(customerLongitude));
    formData.append('vendorId', String(finalVendorId)); // Always send a vendorId for submission

    try {
      const response = await axios.post('/api/jobs/upload', formData, getAuthHeaders());
      if (response.data.success) {
        setSubmittedJobInfo({
          trackingCode: response.data.trackingCode,
          totalPrice: response.data.totalPrice,
        });
        setIsModalOpen(true);
        resetForm();
      } else {
        setError(response.data.error || "Failed to submit job.");
      }
    } catch (err: any) {
      setError(err.response?.data?.error || "An error occurred while submitting the job.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <JobSubmittedModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        trackingCode={submittedJobInfo.trackingCode}
        totalPrice={submittedJobInfo.totalPrice}
      />
      <div className="min-h-screen bg-white text-gray-900 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-4xl w-full bg-white p-10 rounded-xl shadow-2xl border border-gray-200 space-y-8">
          <h2 className="text-4xl font-bold text-center text-[#6C2EFF]">Submit New Print Job</h2>

          {error && <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded-lg relative">{error}</div>}

          <div className="space-y-6">
            {/* File Upload */}
            <div className="p-6 border rounded-xl shadow-inner bg-gray-50/50">
              <h3 className="text-2xl font-bold mb-4 text-[#6C2EFF]">1. Upload Document</h3>
              <label htmlFor="file-upload" className="flex items-center justify-center w-full px-4 py-3 border-2 border-dashed border-gray-300 rounded-lg cursor-pointer bg-white hover:bg-gray-100 transition-colors">
                <Upload className="h-6 w-6 text-gray-500 mr-2" />
                <span className="text-gray-700">{file ? file.name : "Choose a file (PDF, DOCX, JPG, PNG)"}</span>
                <input id="file-upload" type="file" accept=".pdf,.docx,.jpg,.jpeg,.png" onChange={handleFileChange} className="hidden" />
              </label>
            </div>

            {/* Print Specifications */}
            <div className="p-6 border rounded-xl shadow-inner bg-gray-50/50">
              <h3 className="text-2xl font-bold mb-4 text-[#6C2EFF]">2. Print Specifications</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="paperSize" className="block text-sm font-medium text-gray-700">Paper Size</label>
                  <select id="paperSize" value={paperSize} onChange={(e) => setPaperSize(e.target.value)} className="mt-1 block w-full input-style">
                    {paperSizeOptions.map(option => (
                      <option key={option} value={option}>{option}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label htmlFor="copies" className="block text-sm font-medium text-gray-700">Copies</label>
                  <input id="copies" type="number" value={copies} onChange={(e) => setCopies(Number(e.target.value))} min="1" className="mt-1 block w-full input-style" />
                </div>
                <div className="flex items-center">
                  <input id="isColor" type="checkbox" checked={isColor} onChange={(e) => setIsColor(e.target.checked)} className="h-4 w-4 text-[#6C2EFF] border-gray-300 rounded focus:ring-[#6C2EFF]" />
                  <label htmlFor="isColor" className="ml-2 block text-sm text-gray-900">Color Print</label>
                </div>
                <div className="flex items-center">
                  <input id="isDoubleSided" type="checkbox" checked={isDoubleSided} onChange={(e) => setIsDoubleSided(e.target.checked)} className="h-4 w-4 text-[#6C2EFF] border-gray-300 rounded focus:ring-[#6C2EFF]" />
                  <label htmlFor="isDoubleSided" className="ml-2 block text-sm text-gray-900">Double-Sided</label>
                </div>
              </div>
            </div>

            {/* Location Input */}
            <div className="p-6 border rounded-xl shadow-inner bg-gray-50/50">
              <h3 className="text-2xl font-bold mb-4 text-[#6C2EFF]">3. Your Location</h3>
              <p className="text-gray-600 mb-4">Click on the map to set your location or use the search bar.</p>
              <LocationPickerMap
                onLocationChange={handleLocationChange}
                initialLat={customerLatitude}
                initialLng={customerLongitude}
              />
            </div>

            {/* Quote Button */}
            <button 
              onClick={handleGetQuote}
              disabled={loading || !file}
              className="w-full flex items-center justify-center py-3 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-[#6C2EFF] hover:bg-[#5A25CC] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[#6C2EFF] transition-transform transform hover:scale-105 disabled:bg-gray-400 disabled:scale-100"
            >
              {loading ? <Loader2 className="animate-spin mr-2" /> : <DollarSign className="mr-2" />}
              {loading ? 'Getting Quotes...' : 'Get Quotes'}
            </button>

            {/* Quotes Display */}
            {quotes.length > 0 && (
              <div className="p-6 border rounded-xl shadow-inner bg-gray-50/50">
                <h3 className="text-2xl font-bold mb-4 text-[#6C2EFF]">4. Select Vendor & Submit</h3>
                <div className="space-y-4">
                  {quotes.map((quote) => (
                    <div 
                      key={quote.vendorId} 
                      className={`flex items-center justify-between p-4 border rounded-lg cursor-pointer ${selectedVendorId === quote.vendorId ? 'border-[#6C2EFF] ring-2 ring-[#6C2EFF]' : 'border-gray-200'} hover:shadow-md transition-shadow`}
                      onClick={() => setSelectedVendorId(quote.vendorId)}
                    >
                      <div>
                        <p className="font-semibold text-lg text-gray-800">{quote.businessName}</p>
                        <p className="text-sm text-gray-600">{quote.address} ({quote.distance})</p>
                      </div>
                      <div className="text-right">
                        <p className="font-bold text-xl text-[#6C2EFF]">₹{quote.price}</p>
                        {quote.rating && <p className="text-xs text-gray-500">Rating: {quote.rating}/5</p>}
                      </div>
                    </div>
                  ))}
                </div>
                <button 
                  onClick={handleSubmitJob}
                  disabled={loading || quotes.length === 0 || selectedVendorId === null}
                  className="mt-6 w-full flex items-center justify-center py-3 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 transition-transform transform hover:scale-105 disabled:bg-gray-400 disabled:scale-100"
                >
                  {loading ? <Loader2 className="animate-spin mr-2" /> : <Printer className="mr-2" />}
                  {loading ? 'Submitting...' : 'Submit Print Job'}
                </button>
              </div>
            )}
          </div>
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
    </>
  );
};

export default SubmitJobPage;

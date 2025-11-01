'use client';

import { useState, useEffect } from 'react';
import { useParams, useRouter } from 'next/navigation';
import axios from 'axios';
import { Loader2, AlertTriangle, FileText, Printer, User, Calendar, Tag, CheckCircle, Hourglass } from 'lucide-react';

const statusConfig = {
  UPLOADED: { step: 1, text: 'Job Uploaded', icon: <FileText/>, color: 'bg-gray-500' },
  PROCESSING: { step: 2, text: 'Processing', icon: <Hourglass/>, color: 'bg-yellow-500' },
  AWAITING_ACCEPTANCE: { step: 2, text: 'Awaiting Vendor', icon: <Hourglass/>, color: 'bg-yellow-500' },
  ACCEPTED: { step: 3, text: 'Job Accepted', icon: <CheckCircle/>, color: 'bg-blue-500' },
  PRINTING: { step: 4, text: 'Printing', icon: <Printer/>, color: 'bg-indigo-500' },
  READY: { step: 5, text: 'Ready for Pickup', icon: <CheckCircle/>, color: 'bg-green-500' },
  COMPLETED: { step: 6, text: 'Completed', icon: <CheckCircle/>, color: 'bg-purple-500' },
  CANCELLED: { step: 0, text: 'Cancelled', icon: <AlertTriangle/>, color: 'bg-red-500' },
  VENDOR_REJECTED: { step: 0, text: 'Vendor Rejected', icon: <AlertTriangle/>, color: 'bg-red-500' },
  VENDOR_TIMEOUT: { step: 0, text: 'Vendor Timed Out', icon: <AlertTriangle/>, color: 'bg-red-500' },
  NO_VENDORS_AVAILABLE: { step: 0, text: 'No Vendors Found', icon: <AlertTriangle/>, color: 'bg-red-500' },
};

const JobTrackingPage = () => {
  const params = useParams();
  const trackingCode = params.trackingCode as string;
  const router = useRouter();

  const [jobDetails, setJobDetails] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchJobStatus = async () => {
      if (!trackingCode) return;
      setLoading(true);
      setError(null);
      try {
        const response = await axios.get(`/api/jobs/status/${trackingCode}`);
        if (response.data.success) {
          setJobDetails(response.data);
        } else {
          setError(response.data.error || 'Failed to fetch job status.');
        }
      } catch (err: any) {
        setError(err.response?.data?.error || 'An error occurred.');
      } finally {
        setLoading(false);
      }
    };

    fetchJobStatus();

    // Set up polling to refresh job status every 30 seconds
    const intervalId = setInterval(fetchJobStatus, 30000);

    // Clean up interval on component unmount
    return () => clearInterval(intervalId);
  }, [trackingCode]);

  const currentStatusInfo = jobDetails ? statusConfig[jobDetails.status] : null;
  const totalSteps = 6;

  const renderProgressBar = () => {
    if (!currentStatusInfo || currentStatusInfo.step === 0) return null;

    return (
      <div className="w-full">
        <div className="relative h-2 bg-gray-200 rounded-full">
          <div 
            className={`absolute top-0 left-0 h-2 rounded-full ${currentStatusInfo.color} transition-all duration-500`}
            style={{ width: `${(currentStatusInfo.step / totalSteps) * 100}%` }}
          ></div>
        </div>
        <div className="flex justify-between text-xs mt-2 text-gray-600">
          <span>Uploaded</span>
          <span>Accepted</span>
          <span>Printing</span>
          <span>Ready</span>
          <span>Completed</span>
        </div>
      </div>
    );
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-white flex justify-center items-center">
        <Loader2 className="h-16 w-16 text-[#6C2EFF] animate-spin" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-white flex justify-center items-center text-center p-4">
        <div>
          <AlertTriangle className="h-16 w-16 text-red-500 mx-auto mb-4" />
          <h2 className="text-2xl font-bold text-red-700">Error</h2>
          <p className="text-red-600">{error}</p>
        </div>
      </div>
    );
  }

  if (!jobDetails) {
    return null; // Should be handled by loading/error states
  }

  return (
    <div className="min-h-screen bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-4xl mx-auto">
        <div className="bg-white shadow-2xl rounded-2xl p-8 md:p-12">
          <div className="text-center mb-8">
            <h1 className="text-4xl font-bold text-[#6C2EFF]">Job Status</h1>
            <p className="text-lg text-gray-600 mt-2">Tracking Code: 
              <span className="font-mono bg-gray-100 text-[#6C2EFF] py-1 px-2 rounded-md">{jobDetails.trackingCode}</span>
            </p>
          </div>

          <div className="mb-10 p-6 bg-blue-50 border border-blue-200 rounded-lg text-center">
            <p className="text-xl font-semibold text-blue-800">{jobDetails.statusDescription}</p>
            {jobDetails.estimatedCompletion && <p className="text-sm text-blue-600 mt-1">{jobDetails.estimatedCompletion}</p>}
          </div>

          <div className="mb-10">
            {renderProgressBar()}
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-8 text-gray-800">
            <div className="bg-gray-50 p-6 rounded-lg border">
              <h3 className="text-xl font-bold text-[#6C2EFF] mb-4 border-b pb-2 flex items-center"><FileText className="mr-2"/>Document Details</h3>
              <p><strong>File Name:</strong> {jobDetails.fileName}</p>
              <p><strong>Print Specs:</strong> {jobDetails.printSpecs}</p>
              <p><strong>Total Pages:</strong> {jobDetails.totalPages}</p>
              <p><strong>Copies:</strong> {jobDetails.copies}</p>
            </div>
            
            <div className="bg-gray-50 p-6 rounded-lg border">
              <h3 className="text-xl font-bold text-[#6C2EFF] mb-4 border-b pb-2 flex items-center"><Printer className="mr-2"/>Vendor Information</h3>
              {jobDetails.vendor ? (
                <>
                  <p><strong>Business Name:</strong> {jobDetails.vendor.businessName}</p>
                  <p><strong>Address:</strong> {jobDetails.vendor.address}</p>
                </>
              ) : <p>Waiting for a vendor to be assigned...</p>}
            </div>

            <div className="bg-gray-50 p-6 rounded-lg border">
              <h3 className="text-xl font-bold text-[#6C2EFF] mb-4 border-b pb-2 flex items-center"><Tag className="mr-2"/>Order Information</h3>
              <p><strong>Total Price:</strong> ₹{jobDetails.totalPrice.toFixed(2)}</p>
              <p><strong>Payment:</strong> {jobDetails.paymentInfo}</p>
              <p><strong>Anonymous Order:</strong> {jobDetails.isAnonymous ? 'Yes' : 'No'}</p>
            </div>

            <div className="bg-gray-50 p-6 rounded-lg border">
              <h3 className="text-xl font-bold text-[#6C2EFF] mb-4 border-b pb-2 flex items-center"><Calendar className="mr-2"/>Timestamps</h3>
              <p><strong>Created At:</strong> {new Date(jobDetails.createdAt).toLocaleString()}</p>
            </div>
          </div>

        </div>
      </div>
    </div>
  );
};

export default JobTrackingPage;
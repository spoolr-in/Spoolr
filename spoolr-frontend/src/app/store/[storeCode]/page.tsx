'use client';

import { useState, useEffect } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { getStoreDetails, uploadAnonymousJob } from '../../../lib/api';

export default function StorePage() {
  const params = useParams();
  const router = useRouter();
  const storeCode = params.storeCode as string;

  const [vendor, setVendor] = useState(null);
  const [file, setFile] = useState(null);
  const [paperSize, setPaperSize] = useState('A4');
  const [isColor, setIsColor] = useState(false);
  const [isDoubleSided, setIsDoubleSided] = useState(false);
  const [copies, setCopies] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (storeCode) {
      getStoreDetails(storeCode)
        .then(response => {
          setVendor(response.data);
        })
        .catch(err => {
          setError('Failed to fetch vendor details.');
          console.error(err);
        });
    }
  }, [storeCode]);

  const handleFileChange = (e) => {
    setFile(e.target.files[0]);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!file) {
      setError('Please select a file to upload.');
      return;
    }

    setLoading(true);
    setError('');

    const formData = new FormData();
    formData.append('file', file);
    formData.append('storeCode', storeCode);
    formData.append('paperSize', paperSize);
    formData.append('isColor', isColor.toString());
    formData.append('isDoubleSided', isDoubleSided.toString());
    formData.append('copies', copies.toString());

    try {
      const response = await uploadAnonymousJob(formData);
      if (response.data.success) {
        router.push(`/track/${response.data.trackingCode}`);
      } else {
        setError(response.data.message || 'Failed to create print job.');
      }
    } catch (err) {
      setError('An error occurred while uploading the job.');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (error) {
    return <div className="text-red-500 text-center mt-10">{error}</div>;
  }

  if (!vendor) {
    return <div className="text-center mt-10">Loading...</div>;
  }

  return (
    <div className="container mx-auto p-4">
      <h1 className="text-3xl font-bold mb-2">Welcome to {vendor.businessName}</h1>
      <p className="text-lg mb-6">{vendor.businessAddress}</p>

      <form onSubmit={handleSubmit} className="max-w-lg mx-auto bg-white p-8 rounded-lg shadow-md">
        <h2 className="text-2xl font-bold mb-6">Upload Your Document</h2>
        
        <div className="mb-4">
          <label htmlFor="file" className="block text-gray-700 font-bold mb-2">Document</label>
          <input type="file" id="file" onChange={handleFileChange} className="w-full p-2 border rounded" required />
        </div>

        <div className="mb-4">
          <label htmlFor="paperSize" className="block text-gray-700 font-bold mb-2">Paper Size</label>
          <select id="paperSize" value={paperSize} onChange={(e) => setPaperSize(e.target.value)} className="w-full p-2 border rounded">
            <option value="A4">A4</option>
            <option value="A3">A3</option>
            <option value="LETTER">Letter</option>
            <option value="LEGAL">Legal</option>
          </select>
        </div>

        <div className="mb-4">
          <label htmlFor="copies" className="block text-gray-700 font-bold mb-2">Copies</label>
          <input type="number" id="copies" value={copies} onChange={(e) => setCopies(parseInt(e.target.value))} min="1" className="w-full p-2 border rounded" />
        </div>

        <div className="mb-6 flex items-center justify-between">
            <label className="flex items-center">
                <input type="checkbox" checked={isColor} onChange={(e) => setIsColor(e.target.checked)} className="mr-2" />
                Color
            </label>
            <label className="flex items-center">
                <input type="checkbox" checked={isDoubleSided} onChange={(e) => setIsDoubleSided(e.target.checked)} className="mr-2" />
                Double-Sided
            </label>
        </div>

        <button type="submit" className="w-full bg-blue-500 text-white p-3 rounded-lg font-bold hover:bg-blue-600 disabled:bg-gray-400" disabled={loading}>
          {loading ? 'Uploading...' : 'Upload and Print'}
        </button>
      </form>
    </div>
  );
}

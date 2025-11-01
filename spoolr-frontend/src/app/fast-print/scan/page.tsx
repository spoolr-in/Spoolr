'use client';

import { useEffect, useRef } from 'react';
import { useRouter } from 'next/navigation';
import { Html5QrcodeScanner } from 'html5-qrcode';

const ScanQrPage = () => {
  const router = useRouter();
  const scannerRef = useRef(null);

  useEffect(() => {
    // Using a global flag to prevent any re-initialization issues.
    if (window.__spoolr_scanner_active) {
      return;
    }

    const onScanSuccess = (decodedText, decodedResult) => {
      try {
        const url = new URL(decodedText);
        if (url.pathname.startsWith('/store/')) {
          router.push(url.pathname);
        } else {
          alert("Invalid QR Code: Not a valid store URL.");
        }
      } catch (error) {
        if (decodedText.startsWith('/store/')) {
          router.push(decodedText);
        } else {
          console.error("Scanned text is not a valid URL path", error);
          alert("Invalid QR Code: Not a valid store URL path.");
        }
      }
    };

    const onScanFailure = (error) => {
      // console.warn(`Code scan error = ${error}`);
    };

    const config = {
        fps: 10,
        videoConstraints: {
            facingMode: "environment",
            aspectRatio: { ideal: 1 },
            width: { min: 400, ideal: 600, max: 800 },
            height: { min: 400, ideal: 600, max: 800 }
        }
    };

    const html5QrcodeScanner = new Html5QrcodeScanner(
      "qr-reader",
      config,
      false
    );
    
    html5QrcodeScanner.render(onScanSuccess, onScanFailure);
    
    window.__spoolr_scanner_active = true;
    scannerRef.current = html5QrcodeScanner;

    return () => {
      if (scannerRef.current) {
        scannerRef.current.clear().catch(error => {
          console.error("Failed to clear scanner on unmount.", error);
        });
        scannerRef.current = null;
        window.__spoolr_scanner_active = false;
      }
    };
  }, [router]);

  return (
    <div className="container mx-auto p-4 flex flex-col items-center justify-center py-12 md:py-16">
      <div className="w-full max-w-md bg-white rounded-xl shadow-lg p-6 md:p-8 text-center">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className="w-16 h-16 mx-auto text-gray-400 mb-4">
          <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 4.5a.75.75 0 0 0-.75.75v13.5a.75.75 0 0 0 .75.75h13.5a.75.75 0 0 0 .75-.75V5.25a.75.75 0 0 0-.75-.75H3.75Z" />
          <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 9.75h.008v.008H8.25v-.008Zm.75 4.5h.008v.008h-.008v-.008Zm3.75-4.5h.008v.008h-.008v-.008Zm.75 4.5h.008v.008h-.008v-.008Zm3-4.5h.008v.008h-.008v-.008Zm-7.5 0h.008v.008H8.25v-.008Zm.75 0h.008v.008h-.008v-.008Zm3 0h.008v.008h-.008v-.008Z" />
        </svg>
        <h1 className="text-xl font-semibold text-gray-800">Scan Vendor QR Code</h1>
        <p className="text-gray-500 mt-2 mb-6">Center the QR code within the scanner view below.</p>
        
        {/* CRITICAL CHANGE: Simplified to a single container div. */}
        <div className="w-full aspect-square rounded-lg overflow-hidden shadow-inner bg-gray-100">
          <div id="qr-reader"></div>
        </div>
        
        <p className="text-xs text-gray-400 mt-4">Requesting camera access may be required.</p>
      </div>
    </div>
  );
};

export default ScanQrPage;

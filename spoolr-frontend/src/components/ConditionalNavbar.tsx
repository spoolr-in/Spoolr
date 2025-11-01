
"use client";

import { usePathname } from 'next/navigation';
import Navbar from './Navbar';

const ConditionalNavbar = () => {
  const pathname = usePathname();
  const hideNavbarPaths = ['/vendor/verify-email'];

  if (hideNavbarPaths.includes(pathname)) {
    return null; // Don't render Navbar on specified paths
  }

  return <Navbar />;
};

export default ConditionalNavbar;

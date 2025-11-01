'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { Menu, X, LogOut, Printer } from 'lucide-react';

const Navbar = () => {
  const [isOpen, setIsOpen] = useState(false);
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const pathname = usePathname();

  useEffect(() => {
    // In a real app, you'd check for a token in localStorage
    const token = localStorage.getItem('token');
    setIsLoggedIn(!!token);
  }, [pathname]); // Re-check on route change

  const handleLogout = () => {
    localStorage.removeItem('token');
    setIsLoggedIn(false);
    // In a real app, you would redirect to the login page
    window.location.href = '/login';
  };

  const navItems = isLoggedIn
    ? [
        { name: 'Dashboard', href: '/dashboard' },
      ]
    : [
        { name: 'Login', href: '/login' },
        { name: 'Register', href: '/register' },
      ];

  return (
    <nav className="bg-gray-50/90 backdrop-blur-lg text-gray-900 fixed top-0 left-0 right-0 z-50 border-b border-gray-200">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          <div className="flex items-center">
            <Link href="/" className="flex-shrink-0 flex items-center gap-2">
              <Printer className="h-8 w-8 text-[#6C2EFF]" />
              <span className="text-2xl font-bold text-[#6C2EFF]">Spoolr</span>
            </Link>
          </div>
          <div className="hidden md:block">
            <div className="ml-10 flex items-baseline space-x-4">
              {navItems.map((item) => (
                <Link
                  key={item.name}
                  href={item.href}
                  className={`px-5 py-2 text-base font-medium transition-colors ${
                    pathname === item.href
                      ? 'bg-[#6C2EFF] text-white'
                      : 'bg-[#6C2EFF] text-white hover:bg-[#5A25CC]'
                  }`}>
                  {item.name}
                </Link>
              ))}
              {isLoggedIn && (
                <button
                  onClick={handleLogout}
                  className="flex items-center gap-2 px-4 py-2 rounded-none text-sm font-medium text-gray-700 hover:bg-red-700 hover:text-white transition-colors">
                  <LogOut className="h-4 w-4" />
                  <span>Logout</span>
                </button>
              )}
            </div>
          </div>
          <div className="-mr-2 flex md:hidden">
            <button
                onClick={() => setIsOpen(!isOpen)}
                type="button"
                className="bg-gray-100 inline-flex items-center justify-center p-2 rounded-md text-gray-700 hover:text-gray-900 hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-gray-100 focus:ring-[#6C2EFF]"
                aria-controls="mobile-menu"
                aria-expanded="false"
              >
              <span className="sr-only">Open main menu</span>
              {isOpen ? <X className="block h-6 w-6" /> : <Menu className="block h-6 w-6" />}
            </button>
          </div>
        </div>
      </div>

      {isOpen && (
        <div className="md:hidden" id="mobile-menu">
          <div className="px-2 pt-2 pb-3 space-y-1 sm:px-3">
            {navItems.map((item) => (
              <Link
                key={item.name}
                href={item.href}
                className={`block px-3 py-2 rounded-md text-base font-medium transition-colors ${
                  pathname === item.href
                    ? 'bg-[#6C2EFF] text-white'
                    : 'text-gray-700 hover:bg-[#6C2EFF] hover:text-white'
                }`}>
                {item.name}
              </Link>
            ))}
            {isLoggedIn && (
              <button
                onClick={handleLogout}
                className="w-full text-left flex items-center gap-2 px-3 py-2 rounded-md text-base font-medium text-gray-700 hover:bg-red-700 hover:text-white transition-colors">
                <LogOut className="h-4 w-4" />
                <span>Logout</span>
              </button>
            )}
          </div>
        </div>
      )}
    </nav>
  );
};

export default Navbar;

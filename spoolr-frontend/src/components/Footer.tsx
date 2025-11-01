
import Link from 'next/link';

const Footer = () => {
  const currentYear = new Date().getFullYear();

  return (
    <footer className="bg-gray-100 text-gray-900">
      <div className="max-w-7xl mx-auto py-12 px-4 sm:px-6 lg:px-8">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          <div className="space-y-4">
            <h2 className="text-2xl font-bold text-[#6C2EFF]">PrintWave</h2>
            <p className="text-gray-700">
              Your trusted platform for connecting with local print shops. Quality prints, delivered with ease.
            </p>
          </div>
          <div className="space-y-4">
            <h3 className="text-lg font-semibold">Quick Links</h3>
            <ul className="space-y-2">
              <li><Link href="/" className="text-gray-700 hover:text-[#6C2EFF] transition-colors">Home</Link></li>
              <li><Link href="/login" className="text-gray-700 hover:text-[#6C2EFF] transition-colors">Login</Link></li>
              <li><Link href="/register" className="text-gray-700 hover:text-[#6C2EFF] transition-colors">Register</Link></li>
            </ul>
          </div>
          <div className="space-y-4">
            <h3 className="text-lg font-semibold">Contact Us</h3>
            <p className="text-gray-700">Email: support@printwave.com</p>
            <p className="text-gray-700">Phone: +1 (123) 456-7890</p>
            <p className="text-gray-700">Address: 123 Print St, Ink City, PC 12345</p>
          </div>
        </div>
        <div className="mt-8 border-t border-gray-300 pt-8 text-center text-gray-500">
          <p>&copy; {currentYear} PrintWave. All rights reserved.</p>
        </div>
      </div>
    </footer>
  );
};

export default Footer;

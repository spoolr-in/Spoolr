'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { CloudUpload, LifeBuoy, Star } from 'lucide-react';
import PrintChoiceModal from '../components/PrintChoiceModal';

const heroHeadlines = [
  "Spoolr: Your Vision, Printed to Perfection.",
  "Redefining Print Services for the Modern Era.",
  "Seamlessly Connect. Effortlessly Print.",
  "Innovation in Every Impression."
];

const heroTaglines = [
  "Experience unparalleled quality and convenience. Spoolr connects you with top-tier local print professionals, transforming your ideas into tangible masterpieces.",
  "From concept to creation, our platform streamlines your printing journey, ensuring precision, speed, and exceptional results.",
  "Discover a new standard of efficiency and reliability. Spoolr empowers businesses and individuals with cutting-edge print solutions.",
  "Unlock the future of printing. With Spoolr, every project is handled with meticulous care and advanced technology."
];

const HomePage = () => {
  const [headlineIndex, setHeadlineIndex] = useState(0);
  const [isModalOpen, setIsModalOpen] = useState(false);

  useEffect(() => {
    const interval = setInterval(() => {
      setHeadlineIndex((prevIndex) => (prevIndex + 1) % heroHeadlines.length);
    }, 5000); // Change headline every 5 seconds
    return () => clearInterval(interval);
  }, []);

  return (
    <>
      <div className="bg-white text-gray-900">
        {/* Hero Section */}
        <section className="relative min-h-[90vh] flex flex-col items-center justify-center text-center py-24 bg-white">
          <div className="relative z-10 max-w-4xl mx-auto px-4">
            <h1 className="text-5xl md:text-6xl font-extrabold text-gray-900 mb-6 leading-tight">
              {heroHeadlines[headlineIndex]}
            </h1>
            <p className="text-xl text-gray-700 mb-10">
              {heroTaglines[headlineIndex]}
            </p>
            <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
              <button 
                onClick={() => setIsModalOpen(true)}
                className="inline-block bg-[#6C2EFF] hover:bg-[#5A25CC] text-white font-bold py-4 px-10 rounded-lg text-xl transition duration-300 transform hover:scale-105 shadow-lg"
              >
                Print Now
              </button>
              <Link href="/register" className="inline-block bg-gray-200 hover:bg-gray-300 text-gray-800 font-bold py-4 px-10 rounded-lg text-xl transition duration-300 transform hover:scale-105 shadow-lg">
                Get Started
              </Link>
            </div>
          </div>
          <div className="mt-16 max-w-6xl mx-auto px-4">
            <img src="https://media.istockphoto.com/id/157618089/photo/using-copier.webp?s=2048x2048&w=is&k=20&c=PlUcXlDkDmTPie3c5_pWR62TFtvGp4g6tznenjWE4j4=" alt="Modern Printing" className="rounded-lg shadow-xl" />
          </div>
        </section>

        {/* Feature Grid Section */}
        <section className="py-24 bg-white">
          <div className="max-w-7xl mx-auto px-4">
            <h2 className="text-4xl font-bold text-center text-gray-900 mb-16">Unleash Your Creativity</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
              {[{
                icon: <CloudUpload className="w-16 h-16 text-[#6C2EFF] mb-4" />,
                title: 'Effortless Uploads',
                description: 'Securely upload your documents with a few clicks. Our intuitive interface makes file management a breeze.'
              },
              {
                icon: <CloudUpload className="w-16 h-16 text-[#6C2EFF] mb-4" />,
                title: 'Precision Printing',
                description: 'Customize every detail, from paper type to finishing. Our intuitive interface makes file management a breeze.'
              },
              {
                icon: <CloudUpload className="w-16 h-16 text-[#6C2EFF] mb-4" />,
                title: 'Rapid Turnaround',
                description: 'Connect with local print shops for lightning-fast service. Get your prints when you need them, without delay.'
              },
              {
                icon: <LifeBuoy className="w-16 h-16 text-[#6C2EFF] mb-4" />,
                title: 'Dedicated Support',
                description: 'Our team is here to assist you every step of the way. Experience unparalleled customer service and peace of mind.'
              }].map((item, index) => (
                <div key={index} className="bg-gray-100/50 backdrop-blur-lg p-8 rounded-2xl shadow-lg border border-gray-300 text-center transition duration-300 hover:scale-105 hover:shadow-xl">
                  {item.icon}
                  <h3 className="text-2xl font-semibold text-gray-900 mb-2">{item.title}</h3>
                  <p className="text-gray-700">{item.description}</p>
                </div>
              ))}
            </div>
          </div>
        </section>

        {/* About Section */}
        <section className="py-24 bg-white">
          <div className="max-w-7xl mx-auto px-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-12 items-center">
              <div>
                <img src="https://media.istockphoto.com/id/589129694/photo/photo-copy-machine.webp?s=2048x2048&w=is&k=20&c=O9Ryc6g3sBZ-dtdjVTCKd6Rj_T4az6PWgURe_9c-eho=" alt="Our Story" className="rounded-2xl shadow-lg border border-gray-300" />
              </div>
              <div>
                <h2 className="text-4xl font-bold text-gray-900 mb-6">Our Story: Crafting the Future of Print.</h2>
                <p className="text-gray-700 mb-4">
                  At Spoolr, we believe in the power of tangible communication. Founded on the principle of connecting innovation with local craftsmanship, we set out to revolutionize the printing industry.
                </p>
                <p className="text-gray-300 mb-4">
                  Our journey began with a simple idea: make high-quality printing accessible and efficient for everyone. We've built a platform that not only simplifies the process but also empowers local businesses.
                </p>
                <p className="text-gray-300">
                  Today, Spoolr stands as a testament to our commitment to excellence, sustainability, and community. Join us as we continue to shape the future of print, one masterpiece at a time.
                </p>
              </div>
            </div>
          </div>
        </section>

        {/* Testimonials Section */}
        <section className="py-24 bg-white">
          <div className="max-w-7xl mx-auto px-4">
            <h2 className="text-4xl font-bold text-center text-gray-900 mb-16">Voices of Our Valued Clients</h2>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
              {[{
                quote: "Spoolr has transformed our workflow. The quality is exceptional, and the speed is unmatched. A truly indispensable service!",
                author: "Sarah Chen, CEO of InnovateX",
                avatar: "https://via.placeholder.com/150?text=SC",
                rating: 5
              },
              {
                quote: "As a designer, I demand perfection. Spoolr consistently delivers, making my visions a reality with stunning clarity.",
                author: "Mark Davis, Lead Designer at CreativeFlow",
                avatar: "https://via.placeholder.com/150?text=MD",
                rating: 5
              },
              {
                quote: "The ease of use and the support from local print shops through Spoolr is phenomenal. Highly recommend to everyone!",
                author: "Emily White, Small Business Owner",
                avatar: "https://via.placeholder.com/150?text=EW",
                rating: 4
              }].map((testimonial, index) => (
                <div key={index} className="bg-gray-100/50 backdrop-blur-lg p-8 rounded-2xl shadow-lg border border-gray-300 text-center transition duration-300 hover:scale-105 hover:shadow-xl">
                  <div className="flex items-center mb-6">
                    <img src={testimonial.avatar} alt={testimonial.author} className="w-16 h-16 rounded-full object-cover border-2 border-[#6C2EFF] mr-4" />
                    <div>
                      <h3 className="text-xl font-semibold text-gray-900">{testimonial.author}</h3>
                      <div className="flex">
                        {Array.from({ length: testimonial.rating }).map((_, i) => (
                          <Star key={i} className="w-5 h-5 text-yellow-500 fill-current" />
                        ))}
                      </div>
                    </div>
                  </div>
                  <p className="text-gray-700 italic">"{testimonial.quote}"</p>
                </div>
              ))}
            </div>
          </div>
        </section>

        {/* Call to Action Footer */}
        <section className="py-24 bg-gradient-to-r from-[#6C2EFF] to-[#8A2BE2] text-center">
          <div className="max-w-4xl mx-auto px-4">
            <h2 className="text-4xl font-bold text-white mb-6">Ready to Elevate Your Prints?</h2>
            <p className="text-xl text-gray-100 mb-10">
              Join Spoolr today and transform the way you print. Innovation, quality, and convenience await.
            </p>
            <Link href="/register" className="inline-block bg-white text-[#6C2EFF] hover:bg-gray-200 font-bold py-3 px-8 rounded-full text-lg transition duration-300 transform hover:scale-105">
              Sign Up for Free
            </Link>
          </div>
        </section>
      </div>
      {isModalOpen && <PrintChoiceModal onClose={() => setIsModalOpen(false)} />}
    </>
  );
};

export default HomePage;

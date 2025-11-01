
import type { Metadata } from "next";
import { Inter, Lato, Open_Sans } from "next/font/google";
import "./globals.css";
import ConditionalNavbar from "@/components/ConditionalNavbar";
import Footer from "@/components/Footer";
import { Suspense } from 'react';

const lato = Lato({ subsets: ["latin"], weight: ["400", "700"] });
const openSans = Open_Sans({ subsets: ["latin"], weight: ["400", "700"] });

export const metadata: Metadata = {
  title: "Spoolr",
  description: "Modern Print Services",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body>
        <ConditionalNavbar />
        <Suspense fallback={<div>Loading...</div>}>
          <main>{children}</main>
        </Suspense>
          <Footer />
      </body>
    </html>
  );
}

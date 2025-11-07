
"use client";

import { MapContainer, TileLayer, Marker, useMap, useMapEvents } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';
import L from 'leaflet';
import { useState, useEffect, useCallback, useRef } from 'react';

// Fix for default icon issue with webpack
delete (L.Icon.Default.prototype as any)._getIconUrl;

L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

interface LocationPickerMapProps {
  onLocationChange: (lat: number, lng: number) => void;
  initialLat?: number;
  initialLng?: number;
}

const LocationPickerMap: React.FC<LocationPickerMapProps> = ({ onLocationChange, initialLat, initialLng }) => {
  const [position, setPosition] = useState<L.LatLng | null>(
    (initialLat && initialLng) ? L.latLng(initialLat, initialLng) : null
  );
  const [mapCenter, setMapCenter] = useState<L.LatLngExpression>(
    (initialLat && initialLng) ? [initialLat, initialLng] : [51.505, -0.09] // Default to London if no initial provided
  );
  const [locateMessage, setLocateMessage] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [searchResults, setSearchResults] = useState<any[]>([]);
  const [searchLoading, setSearchLoading] = useState(false);
  const mapRef = useRef<L.Map | null>(null); // Ref to store the map instance

  const updatePosition = useCallback(async (lat: number, lng: number) => {
    const newPos = L.latLng(lat, lng);
    setPosition(newPos);
    onLocationChange(lat, lng);
    setMapCenter(newPos);
    setLocateMessage(null); // Clear message on manual change

    // Reverse geocode to get address for display
    try {
      const response = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}`);
      const data = await response.json();
      if (data && data.display_name) {
        setLocateMessage(`Location: ${data.display_name}`);
      } else {
        setLocateMessage(`Location: ${lat.toFixed(4)}, ${lng.toFixed(4)}`);
      }
    } catch (error) {
      console.error("Reverse geocoding failed:", error);
      setLocateMessage(`Location: ${lat.toFixed(4)}, ${lng.toFixed(4)}`);
    }
  }, [onLocationChange]);

  const LocationMarker = () => {
    const map = useMapEvents({
      click(e) {
        updatePosition(e.latlng.lat, e.latlng.lng);
        map.flyTo(e.latlng, map.getZoom());
      },
      locationfound(e) {
        updatePosition(e.latlng.lat, e.latlng.lng);
        map.flyTo(e.latlng, map.getZoom());
        setLocateMessage('Location found!');
      },
      locationerror(e) {
        setLocateMessage(`Location error: ${e.message}`);
      },
    });

    // Handle manual drag of marker

    // Handle manual drag of marker
    const eventHandlers = {
      dragend: useCallback((e: any) => {
        const newPos = e.target.getLatLng();
        updatePosition(newPos.lat, newPos.lng);
      }, [updatePosition]),
    };

    return position === null ? null : (
      <Marker 
        position={position} 
        draggable={true}
        eventHandlers={eventHandlers}
      >
      </Marker>
    );
  };

  // Handle search input changes
  useEffect(() => {
    const delayDebounceFn = setTimeout(async () => {
      if (searchTerm.length > 2) {
        setSearchLoading(true);
        try {
          const response = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${searchTerm}`);
          const data = await response.json();
          setSearchResults(data);
        } catch (error) {
          console.error("Search failed:", error);
          setSearchResults([]);
        } finally {
          setSearchLoading(false);
        }
      } else {
        setSearchResults([]);
      }
    }, 500);

    return () => clearTimeout(delayDebounceFn);
  }, [searchTerm]);

  const handleSearchResultClick = (lat: string, lon: string) => {
    const newLat = parseFloat(lat);
    const newLon = parseFloat(lon);
    if (!isNaN(newLat) && !isNaN(newLon)) {
      updatePosition(newLat, newLon);
      setSearchResults([]); // Clear search results
      if (mapRef.current) {
        mapRef.current.flyTo([newLat, newLon], 16); // Fly to selected location
      }
    }
  };

  const handleLocateMe = useCallback(() => {
    setLocateMessage('Locating...');
    if (mapRef.current) {
      mapRef.current.locate({ setView: true, maxZoom: 16 });
    } else {
      setLocateMessage('Map not initialized.');
    }
  }, []);

  const MapInitializer = () => {
    const map = useMap();
    useEffect(() => {
      if (map) {
        mapRef.current = map;
        if (!position) {
          setLocateMessage('Locating...');
          map.locate();
        }
      }
    }, [map]);
    return null;
  };

  return (
    <>
      <div className="mb-4">
        <input
          type="text"
          placeholder="Search for a location..."
          className="input-style w-full"
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
        />
        {searchLoading && <p className="text-gray-600 text-sm mt-1">Searching...</p>}
        {searchResults.length > 0 && (
          <ul className="bg-white border border-gray-300 rounded-md mt-1 max-h-60 overflow-y-auto shadow-lg">
            {searchResults.map((result) => (
              <li
                key={result.place_id}
                className="p-2 hover:bg-gray-100 cursor-pointer text-gray-800 text-sm border-b border-gray-200 last:border-b-0"
                onClick={() => handleSearchResultClick(result.lat, result.lon)}
              >
                {result.display_name}
              </li>
            ))}
          </ul>
        )}
      </div>
      <MapContainer
        center={mapCenter}
        zoom={13}
        scrollWheelZoom={false}
        style={{ height: '400px', width: '100%', borderRadius: '15px', zIndex: 0 }}
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <LocationMarker />
        <MapInitializer />
      </MapContainer>
      <div className="mt-4 text-center">
        <button
          type="button"
          onClick={handleLocateMe}
          className="py-2 px-4 bg-[#6C2EFF] hover:bg-[#5A25CC] text-white font-semibold rounded-lg transition-colors duration-300"
        >
          Use My Current Location
        </button>
        {locateMessage && <p className="mt-2 text-sm text-gray-600">{locateMessage}</p>}
      </div>
    </>
  );
};

export default LocationPickerMap;

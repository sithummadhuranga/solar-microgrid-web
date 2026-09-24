// a small map, click or drag the pin to set a node's gps location
import { useCallback } from 'react'
import { GoogleMap, Marker, useJsApiLoader } from '@react-google-maps/api'

type LocationPickerProps = {
  latitude: number
  longitude: number
  onChange: (latitude: number, longitude: number) => void
}

const mapContainerStyle = { width: '100%', height: '220px' }
const colombo = { lat: 6.9271, lng: 79.8612 }

function LocationPicker({ latitude, longitude, onChange }: LocationPickerProps) {
  const { isLoaded } = useJsApiLoader({
    googleMapsApiKey: import.meta.env.VITE_GOOGLE_MAPS_API_KEY,
  })

  const position = latitude && longitude ? { lat: latitude, lng: longitude } : colombo

  // moves the pin to where the map was clicked
  const handleClick = useCallback(
    (e: google.maps.MapMouseEvent) => {
      if (e.latLng) onChange(e.latLng.lat(), e.latLng.lng())
    },
    [onChange],
  )

  // moves the pin to where it was dropped
  const handleDragEnd = useCallback(
    (e: google.maps.MapMouseEvent) => {
      if (e.latLng) onChange(e.latLng.lat(), e.latLng.lng())
    },
    [onChange],
  )

  if (!isLoaded) return <div className="text-body-secondary small">Loading map...</div>

  return (
    <GoogleMap mapContainerStyle={mapContainerStyle} center={position} zoom={12} onClick={handleClick}>
      <Marker position={position} draggable onDragEnd={handleDragEnd} />
    </GoogleMap>
  )
}

export default LocationPicker

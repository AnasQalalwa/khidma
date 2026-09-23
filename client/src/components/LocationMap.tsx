import 'leaflet/dist/leaflet.css'
import { useCallback, useEffect, useRef, useState } from 'react'
import { LocateFixed } from 'lucide-react'
import { Button } from './Button'
import {
  findCity,
  nearestCity,
  PALESTINE_CENTER,
} from '../data/cities'

type LocationMapProps = {
  city: string
  latitude: number | null
  longitude: number | null
  onChange?: (next: { city: string; latitude: number; longitude: number }) => void
  readOnly?: boolean
}

export function LocationMap({
  city,
  latitude,
  longitude,
  onChange,
  readOnly = false,
}: LocationMapProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const mapRef = useRef<import('leaflet').Map | null>(null)
  const markerRef = useRef<import('leaflet').Marker | null>(null)
  const onChangeRef = useRef(onChange)
  const cityRef = useRef(city)
  const latitudeRef = useRef(latitude)
  const longitudeRef = useRef(longitude)
  const readOnlyRef = useRef(readOnly)
  const [geoError, setGeoError] = useState<string | null>(null)
  const [locating, setLocating] = useState(false)

  useEffect(() => {
    onChangeRef.current = onChange
    cityRef.current = city
    latitudeRef.current = latitude
    longitudeRef.current = longitude
    readOnlyRef.current = readOnly
  })

  const emitChange = useCallback((lat: number, lng: number) => {
    const match = nearestCity(lat, lng)
    onChangeRef.current?.({
      city: match.name,
      latitude: Number(lat.toFixed(6)),
      longitude: Number(lng.toFixed(6)),
    })
  }, [])

  useEffect(() => {
    const element = containerRef.current
    if (!element) {
      return
    }

    let cancelled = false

    void import('leaflet').then((leaflet) => {
      if (cancelled || mapRef.current || !containerRef.current) {
        return
      }

      const L = (leaflet.default ?? leaflet) as typeof import('leaflet')
      const start = coordinatesFor(
        cityRef.current,
        latitudeRef.current,
        longitudeRef.current,
      )
      const map = L.map(containerRef.current, {
        scrollWheelZoom: false,
      }).setView([start.lat, start.lng], start.zoom)

      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap',
      }).addTo(map)

      const icon = L.divIcon({
        className: 'location-pin',
        html: '<span></span>',
        iconSize: [22, 22],
        iconAnchor: [11, 22],
      })

      const marker = L.marker([start.lat, start.lng], {
        icon,
        draggable: !readOnlyRef.current,
      }).addTo(map)

      if (!readOnlyRef.current) {
        map.on('click', (event: { latlng: { lat: number; lng: number } }) => {
          marker.setLatLng(event.latlng)
          emitChange(event.latlng.lat, event.latlng.lng)
        })

        marker.on('dragend', () => {
          const position = marker.getLatLng()
          emitChange(position.lat, position.lng)
        })
      }

      mapRef.current = map
      markerRef.current = marker
      window.setTimeout(() => map.invalidateSize(), 80)
    })

    return () => {
      cancelled = true
      mapRef.current?.remove()
      mapRef.current = null
      markerRef.current = null
    }
  }, [emitChange])

  useEffect(() => {
    const map = mapRef.current
    const marker = markerRef.current
    if (!map || !marker) {
      return
    }

    const next = coordinatesFor(city, latitude, longitude)
    marker.setLatLng([next.lat, next.lng])
    map.setView([next.lat, next.lng], next.zoom)
  }, [city, latitude, longitude])

  function useMyLocation() {
    if (!navigator.geolocation) {
      setGeoError('This browser cannot read your current location.')
      return
    }

    setLocating(true)
    setGeoError(null)
    navigator.geolocation.getCurrentPosition(
      (position) => {
        emitChange(position.coords.latitude, position.coords.longitude)
        setLocating(false)
      },
      () => {
        setGeoError('Allow location access, or click the map to drop a pin.')
        setLocating(false)
      },
      { enableHighAccuracy: true, timeout: 8000 },
    )
  }

  const hasPin = latitude != null && longitude != null

  return (
    <div className={readOnly ? 'location-picker is-readonly' : 'location-picker'}>
      {readOnly ? null : (
        <div className="location-picker-toolbar">
          <Button
            type="button"
            variant="secondary"
            icon={LocateFixed}
            loading={locating}
            onClick={useMyLocation}
          >
            {locating ? 'Finding you…' : 'Use my current location'}
          </Button>
          <p className="muted">
            {hasPin
              ? `${latitude.toFixed(5)}, ${longitude.toFixed(5)}`
              : 'Click the map or use your location so customers can see where you work.'}
          </p>
        </div>
      )}
      {geoError ? (
        <p className="field-error" role="status">
          {geoError}
        </p>
      ) : null}
      <div
        ref={containerRef}
        className="location-map"
        role="application"
        aria-label={readOnly ? 'Approved work location' : 'Choose your location on the map'}
      />
    </div>
  )
}

function coordinatesFor(
  city: string,
  latitude: number | null,
  longitude: number | null,
): { lat: number; lng: number; zoom: number } {
  if (latitude != null && longitude != null) {
    return { lat: latitude, lng: longitude, zoom: 14 }
  }

  const known = findCity(city)
  if (known) {
    return { lat: known.lat, lng: known.lng, zoom: 13 }
  }

  return { ...PALESTINE_CENTER }
}

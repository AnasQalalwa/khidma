export type CityOption = {
  name: string
  lat: number
  lng: number
}

export const PALESTINIAN_CITIES: CityOption[] = [
  { name: 'Ramallah', lat: 31.9038, lng: 35.2034 },
  { name: 'Al-Bireh', lat: 31.9074, lng: 35.216 },
  { name: 'Nablus', lat: 32.2211, lng: 35.2544 },
  { name: 'Hebron', lat: 31.5326, lng: 35.0998 },
  { name: 'Bethlehem', lat: 31.7054, lng: 35.2024 },
  { name: 'Jenin', lat: 32.4611, lng: 35.3 },
  { name: 'Tulkarm', lat: 32.3103, lng: 35.0286 },
  { name: 'Qalqilya', lat: 32.1897, lng: 34.9706 },
  { name: 'Jericho', lat: 31.8611, lng: 35.4618 },
  { name: 'Salfit', lat: 32.085, lng: 35.1806 },
  { name: 'Tubas', lat: 32.3214, lng: 35.3694 },
  { name: 'Jerusalem', lat: 31.7683, lng: 35.2137 },
  { name: 'Gaza', lat: 31.5016, lng: 34.4668 },
  { name: 'Khan Yunis', lat: 31.3462, lng: 34.3031 },
  { name: 'Rafah', lat: 31.2969, lng: 34.2455 },
  { name: 'Deir al-Balah', lat: 31.4171, lng: 34.3503 },
]

export const PALESTINE_CENTER = { lat: 31.95, lng: 35.23, zoom: 8 }

export function findCity(name: string): CityOption | undefined {
  return PALESTINIAN_CITIES.find(
    (city) => city.name.toLowerCase() === name.trim().toLowerCase(),
  )
}

export function cityChoices(current?: string): string[] {
  const names = PALESTINIAN_CITIES.map((city) => city.name)
  const trimmed = current?.trim() ?? ''
  if (trimmed && !names.some((name) => name.toLowerCase() === trimmed.toLowerCase())) {
    return [trimmed, ...names]
  }

  return names
}

function toRadians(value: number): number {
  return (value * Math.PI) / 180
}

export function nearestCity(lat: number, lng: number): CityOption {
  let best = PALESTINIAN_CITIES[0]
  let bestDistance = Number.POSITIVE_INFINITY

  for (const city of PALESTINIAN_CITIES) {
    const dLat = toRadians(city.lat - lat)
    const dLng = toRadians(city.lng - lng)
    const a =
      Math.sin(dLat / 2) ** 2 +
      Math.cos(toRadians(lat)) * Math.cos(toRadians(city.lat)) * Math.sin(dLng / 2) ** 2
    const distance = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a))
    if (distance < bestDistance) {
      bestDistance = distance
      best = city
    }
  }

  return best
}

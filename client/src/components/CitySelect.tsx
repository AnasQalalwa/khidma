import type { SelectHTMLAttributes } from 'react'
import { cityChoices } from '../data/cities'

type CitySelectProps = {
  value: string
  onChange: (city: string) => void
} & Omit<SelectHTMLAttributes<HTMLSelectElement>, 'value' | 'onChange'>

export function CitySelect({ value, onChange, ...rest }: CitySelectProps) {
  return (
    <select value={value} onChange={(event) => onChange(event.target.value)} {...rest}>
      <option value="">Select your city</option>
      {cityChoices(value).map((name) => (
        <option key={name} value={name}>
          {name}
        </option>
      ))}
    </select>
  )
}

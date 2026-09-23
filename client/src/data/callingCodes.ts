export type CallingCode = {
  name: string
  dial: string
}

const CALLING_CODES_UNSORTED: CallingCode[] = [
  { name: 'Palestine', dial: '+970' },
  { name: 'Algeria', dial: '+213' },
  { name: 'Argentina', dial: '+54' },
  { name: 'Australia', dial: '+61' },
  { name: 'Austria', dial: '+43' },
  { name: 'Bahrain', dial: '+973' },
  { name: 'Bangladesh', dial: '+880' },
  { name: 'Belgium', dial: '+32' },
  { name: 'Brazil', dial: '+55' },
  { name: 'China', dial: '+86' },
  { name: 'Egypt', dial: '+20' },
  { name: 'France', dial: '+33' },
  { name: 'Germany', dial: '+49' },
  { name: 'Greece', dial: '+30' },
  { name: 'India', dial: '+91' },
  { name: 'Indonesia', dial: '+62' },
  { name: 'Iraq', dial: '+964' },
  { name: 'Israel', dial: '+972' },
  { name: 'Italy', dial: '+39' },
  { name: 'Japan', dial: '+81' },
  { name: 'Jordan', dial: '+962' },
  { name: 'Kuwait', dial: '+965' },
  { name: 'Lebanon', dial: '+961' },
  { name: 'Libya', dial: '+218' },
  { name: 'Malaysia', dial: '+60' },
  { name: 'Mexico', dial: '+52' },
  { name: 'Morocco', dial: '+212' },
  { name: 'Netherlands', dial: '+31' },
  { name: 'Oman', dial: '+968' },
  { name: 'Pakistan', dial: '+92' },
  { name: 'Philippines', dial: '+63' },
  { name: 'Poland', dial: '+48' },
  { name: 'Qatar', dial: '+974' },
  { name: 'Russia', dial: '+7' },
  { name: 'Saudi Arabia', dial: '+966' },
  { name: 'South Africa', dial: '+27' },
  { name: 'South Korea', dial: '+82' },
  { name: 'Spain', dial: '+34' },
  { name: 'Sudan', dial: '+249' },
  { name: 'Sweden', dial: '+46' },
  { name: 'Switzerland', dial: '+41' },
  { name: 'Syria', dial: '+963' },
  { name: 'Tunisia', dial: '+216' },
  { name: 'Turkey', dial: '+90' },
  { name: 'Ukraine', dial: '+380' },
  { name: 'United Arab Emirates', dial: '+971' },
  { name: 'United Kingdom', dial: '+44' },
  { name: 'United States / Canada', dial: '+1' },
  { name: 'Yemen', dial: '+967' },
]

export const DEFAULT_DIAL_CODE = '+970'

export const CALLING_CODES: CallingCode[] = [
  CALLING_CODES_UNSORTED[0],
  ...CALLING_CODES_UNSORTED.slice(1).sort((left, right) => left.name.localeCompare(right.name)),
]

export const CALLING_CODES_BY_LENGTH: CallingCode[] = [...CALLING_CODES].sort(
  (left, right) => right.dial.length - left.dial.length,
)

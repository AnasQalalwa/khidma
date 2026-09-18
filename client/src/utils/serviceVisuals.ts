import carpetCleaning from '../assets/catalog/carpet-cleaning.webp'
import carpentry from '../assets/catalog/carpentry.webp'
import catalogHeroRoom from '../assets/catalog/catalog-hero-room.webp'
import computerRepair from '../assets/catalog/computer-repair.webp'
import electrical from '../assets/catalog/electrical.webp'
import englishTutoring from '../assets/catalog/english-tutoring.webp'
import homeCleaning from '../assets/catalog/home-cleaning.webp'
import mathTutoring from '../assets/catalog/math-tutoring.webp'
import networkSetup from '../assets/catalog/network-setup.webp'
import painting from '../assets/catalog/painting.webp'
import phoneRepair from '../assets/catalog/phone-repair.webp'
import plumbing from '../assets/catalog/plumbing.webp'
import windowCleaning from '../assets/catalog/window-cleaning.webp'

export const catalogHeroImage = catalogHeroRoom

type ServiceVisual = {
  image: string
  alt: string
  description: string
}

const SERVICE_VISUALS: Record<string, ServiceVisual> = {
  'Carpet Cleaning': {
    image: carpetCleaning,
    alt: 'Carpet cleaning service',
    description: 'Professional carpet cleaning for a fresher, healthier home.',
  },
  'Home Cleaning': {
    image: homeCleaning,
    alt: 'Home cleaning service',
    description: 'Reliable home cleaning services for a cleaner space.',
  },
  'Window Cleaning': {
    image: windowCleaning,
    alt: 'Window cleaning service',
    description: 'Crystal-clear windows inside and out.',
  },
  Carpentry: {
    image: carpentry,
    alt: 'Carpentry service',
    description: 'Custom woodwork, repairs and installations.',
  },
  Electrical: {
    image: electrical,
    alt: 'Electrical repair service',
    description: 'Safe and reliable electrical services for your home.',
  },
  Painting: {
    image: painting,
    alt: 'Painting service',
    description: 'Give your space a fresh new look.',
  },
  Plumbing: {
    image: plumbing,
    alt: 'Plumbing repair service',
    description: 'Fix leaks, installations and more.',
  },
  'Computer Repair': {
    image: computerRepair,
    alt: 'Computer repair service',
    description: 'Fast and reliable computer repair services.',
  },
  'Network Setup': {
    image: networkSetup,
    alt: 'Network setup service',
    description: 'Set up and optimize your home or office network.',
  },
  'Phone Repair': {
    image: phoneRepair,
    alt: 'Phone repair service',
    description: 'Screen repair, battery replacement and more.',
  },
  'English Tutoring': {
    image: englishTutoring,
    alt: 'English tutoring service',
    description: 'Improve your English with experienced tutors.',
  },
  'Math Tutoring': {
    image: mathTutoring,
    alt: 'Math tutoring service',
    description: 'Get help with math from qualified tutors.',
  },
}

const FALLBACK_VISUAL: ServiceVisual = {
  image: catalogHeroRoom,
  alt: 'Local service',
  description: 'Browse this service in the Khidma catalog.',
}

export function getServiceVisual(name: string): ServiceVisual {
  return SERVICE_VISUALS[name] ?? FALLBACK_VISUAL
}

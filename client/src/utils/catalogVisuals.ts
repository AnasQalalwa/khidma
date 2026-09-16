import type { LucideIcon } from 'lucide-react'
import { GraduationCap, Grid2X2, House, Monitor, Sparkles } from 'lucide-react'

export type CategoryBadgeTone = 'cleaning' | 'home' | 'technology' | 'tutoring' | 'default'

function normalize(value: string) {
  return value.trim().toLowerCase()
}

export function getCategoryIcon(name: string): LucideIcon {
  const key = normalize(name)

  if (key.includes('clean')) {
    return Sparkles
  }

  if (key.includes('home') || key.includes('handyman')) {
    return House
  }

  if (key.includes('tech') || key.includes('computer') || key.includes('it ')) {
    return Monitor
  }

  if (key.includes('tutor') || key.includes('educat') || key.includes('learn')) {
    return GraduationCap
  }

  return Grid2X2
}

export function getCategoryBadgeTone(name: string): CategoryBadgeTone {
  const key = normalize(name)

  if (key.includes('clean')) {
    return 'cleaning'
  }

  if (key.includes('home') || key.includes('handyman')) {
    return 'home'
  }

  if (key.includes('tech') || key.includes('computer') || key.includes('it ')) {
    return 'technology'
  }

  if (key.includes('tutor') || key.includes('educat') || key.includes('learn')) {
    return 'tutoring'
  }

  return 'default'
}

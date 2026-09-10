/* eslint-disable react-refresh/only-export-components */
import type { LucideIcon, LucideProps } from 'lucide-react'
import {
  BookOpen,
  Briefcase,
  Droplets,
  GraduationCap,
  Hammer,
  Home,
  Laptop,
  PaintRoller,
  Smartphone,
  Sparkles,
  Truck,
  Wifi,
  Wrench,
  Zap,
} from 'lucide-react'

export const ICON_STROKE = 1.75

export type AccentKind =
  | 'cleaning'
  | 'plumbing'
  | 'electrical'
  | 'painting'
  | 'carpentry'
  | 'technology'
  | 'education'
  | 'moving'
  | 'home'
  | 'service'

export type IconSize = 16 | 20 | 24

export function Icon({
  icon: Lucide,
  size = 20,
  className,
  ...props
}: {
  icon: LucideIcon
  size?: IconSize
  className?: string
} & Omit<LucideProps, 'size' | 'strokeWidth' | 'icon'>) {
  return (
    <Lucide
      size={size}
      strokeWidth={ICON_STROKE}
      className={className}
      aria-hidden={props['aria-hidden'] ?? true}
      {...props}
    />
  )
}

export function IconTile({
  icon,
  accent = 'service',
  size = 20,
  large = false,
}: {
  icon: LucideIcon
  accent?: AccentKind
  size?: IconSize
  large?: boolean
}) {
  return (
    <span
      className={`icon-tile icon-tile-${accent}${large ? ' icon-tile-lg' : ''}`}
      aria-hidden="true"
    >
      <Icon icon={icon} size={size} />
    </span>
  )
}

export function categoryVisual(name: string): { icon: LucideIcon; accent: AccentKind } {
  const key = name.toLowerCase()

  if (/(clean|maid|laundry|carpet|window)/.test(key)) {
    return { icon: Sparkles, accent: 'cleaning' }
  }

  if (/(plumb|pipe|drain)/.test(key)) {
    return { icon: Droplets, accent: 'plumbing' }
  }

  if (/(electric|wiring|lighting)/.test(key)) {
    return { icon: Zap, accent: 'electrical' }
  }

  if (/(paint)/.test(key)) {
    return { icon: PaintRoller, accent: 'painting' }
  }

  if (/(carpent|wood|cabinet)/.test(key)) {
    return { icon: Hammer, accent: 'carpentry' }
  }

  if (/(phone|mobile)/.test(key)) {
    return { icon: Smartphone, accent: 'technology' }
  }

  if (/(network|wifi|internet)/.test(key)) {
    return { icon: Wifi, accent: 'technology' }
  }

  if (/(tech|computer|laptop|it support|repair)/.test(key)) {
    return { icon: Laptop, accent: 'technology' }
  }

  if (/(tutor|educat|learn|school|math|english|book)/.test(key)) {
    return { icon: GraduationCap, accent: 'education' }
  }

  if (/(class|lesson)/.test(key)) {
    return { icon: BookOpen, accent: 'education' }
  }

  if (/(mov(e|ing)|reloc)/.test(key)) {
    return { icon: Truck, accent: 'moving' }
  }

  if (/(home|house|handyman)/.test(key)) {
    return { icon: Home, accent: 'home' }
  }

  if (/(wrench|tool|fix)/.test(key)) {
    return { icon: Wrench, accent: 'home' }
  }

  return { icon: Briefcase, accent: 'service' }
}

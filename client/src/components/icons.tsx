/* eslint-disable react-refresh/only-export-components */
import type { LucideIcon, LucideProps } from 'lucide-react'
import {
  BookOpen,
  Briefcase,
  GraduationCap,
  Hammer,
  Home,
  Monitor,
  PaintRoller,
  Smartphone,
  Sparkles,
  Truck,
  Wifi,
  Wrench,
  Zap,
} from 'lucide-react'

export const ICON_STROKE = 2

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

export function categoryVisual(name: string): {
  icon: LucideIcon
  accent: AccentKind
  description: string
} {
  const key = name.toLowerCase()

  if (/(clean|maid|laundry|carpet|window)/.test(key)) {
    return {
      icon: Sparkles,
      accent: 'cleaning',
      description: 'A cleaner, healthier home',
    }
  }

  if (/(plumb|pipe|drain)/.test(key)) {
    return {
      icon: Wrench,
      accent: 'plumbing',
      description: 'Fix leaks, installs and more',
    }
  }

  if (/(electric|wiring|lighting)/.test(key)) {
    return {
      icon: Zap,
      accent: 'electrical',
      description: 'Safe and reliable service',
    }
  }

  if (/(paint)/.test(key)) {
    return {
      icon: PaintRoller,
      accent: 'painting',
      description: 'Fresh finishes, done right',
    }
  }

  if (/(carpent|wood|cabinet)/.test(key)) {
    return {
      icon: Hammer,
      accent: 'carpentry',
      description: 'Built to last',
    }
  }

  if (/(phone|mobile)/.test(key)) {
    return {
      icon: Smartphone,
      accent: 'technology',
      description: 'Tech help made easy',
    }
  }

  if (/(network|wifi|internet)/.test(key)) {
    return {
      icon: Wifi,
      accent: 'technology',
      description: 'Tech help made easy',
    }
  }

  if (/(tech|computer|laptop|it support|repair)/.test(key)) {
    return {
      icon: Monitor,
      accent: 'technology',
      description: 'Tech help made easy',
    }
  }

  if (/(tutor|educat|learn|school|math|english|book)/.test(key)) {
    return {
      icon: GraduationCap,
      accent: 'education',
      description: 'Learn with expert tutors',
    }
  }

  if (/(class|lesson)/.test(key)) {
    return {
      icon: BookOpen,
      accent: 'education',
      description: 'Learn with expert tutors',
    }
  }

  if (/(mov(e|ing)|reloc)/.test(key)) {
    return {
      icon: Truck,
      accent: 'moving',
      description: 'Hassle-free moving',
    }
  }

  if (/(home|house|handyman)/.test(key)) {
    return {
      icon: Home,
      accent: 'home',
      description: 'Repairs, installs and more',
    }
  }

  if (/(wrench|tool|fix)/.test(key)) {
    return {
      icon: Wrench,
      accent: 'home',
      description: 'Repairs, installs and more',
    }
  }

  return {
    icon: Briefcase,
    accent: 'service',
    description: 'Local help when you need it',
  }
}

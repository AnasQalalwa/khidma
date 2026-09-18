export function tokenColor(name: string, fallback: string): string {
  if (typeof window === 'undefined') {
    return fallback
  }

  const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim()
  return value || fallback
}

export function chartColors() {
  return {
    primary: tokenColor('--color-primary', '#0f766e'),
    heading: tokenColor('--color-heading', '#12203a'),
    muted: tokenColor('--color-text-muted', '#5b6b7a'),
    danger: tokenColor('--color-danger', '#b42318'),
    warning: tokenColor('--color-warning', '#b45309'),
    border: tokenColor('--color-border', '#dde4e1'),
  }
}

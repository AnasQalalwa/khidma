import { Camera } from 'lucide-react'
import { useState } from 'react'
import { providerPhotoUrl } from '../api/providers'

function initials(name: string) {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('')
}

export function ProviderAvatar({
  providerId,
  name,
  hasPhoto,
  cacheKey,
  editable = false,
  uploading = false,
  size = 'lg',
  onUpload,
}: {
  providerId: number
  name: string
  hasPhoto: boolean
  cacheKey?: string | number
  editable?: boolean
  uploading?: boolean
  size?: 'sm' | 'md' | 'lg'
  onUpload?: (file: File) => void
}) {
  const [broken, setBroken] = useState(false)
  const showPhoto = hasPhoto && !broken
  const src = providerPhotoUrl(providerId, cacheKey)

  return (
    <div className={`provider-avatar provider-avatar-${size}${editable ? ' is-editable' : ''}`}>
      {showPhoto ? (
        <img src={src} alt={name} onError={() => setBroken(true)} />
      ) : (
        <span aria-hidden="true">{initials(name) || 'K'}</span>
      )}
      {editable && onUpload ? (
        <label className="provider-avatar-edit">
          <Camera size={16} />
          <span>{uploading ? 'Saving…' : 'Photo'}</span>
          <input
            type="file"
            accept=".jpg,.jpeg,.png,image/jpeg,image/png"
            disabled={uploading}
            onChange={(event) => {
              const file = event.target.files?.[0]
              event.target.value = ''
              if (file) {
                setBroken(false)
                onUpload(file)
              }
            }}
          />
        </label>
      ) : null}
    </div>
  )
}

import type { CatalogService } from './catalog'

export type PagedResult<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type PageQuery = {
  page?: number
  pageSize?: number
  status?: string
}

export type BookingStatus =
  | 'Pending'
  | 'Scheduled'
  | 'InProgress'
  | 'Completed'
  | 'Declined'
  | 'Cancelled'

export type WorkingHour = {
  dayOfWeek: number
  hour: number
}

export type BusyInterval = {
  start: string
  end: string
}

export type ProviderAvailability = {
  workingHours: WorkingHour[]
  busy: BusyInterval[]
}

export type ScheduleEntry = {
  bookingId: number
  serviceName: string
  customerName: string
  status: BookingStatus
  start: string | null
  end: string | null
  requestedDate: string | null
}

export type ProviderSchedule = {
  workingHours: WorkingHour[]
  items: ScheduleEntry[]
}

export type CreateBookingPayload = {
  providerProfileId: number
  serviceId: number
  requestedDate: string
  notes?: string
}

export type AcceptBookingPayload = {
  price: number
  message?: string
  scheduledStart: string
  durationHours: number
}

export type ReschedulePayload = {
  scheduledStart: string
  durationHours: number
  note?: string
}

export type Review = {
  id: number
  rating: number
  comment: string | null
  createdAt: string
  customerDisplayName: string
}

export type PublicReview = {
  reviewerFirstName: string
  rating: number
  comment: string | null
  createdAt: string
}

export type BookingSummary = {
  id: number
  serviceId: number
  serviceName: string
  categoryName: string
  city: string
  requestedDate: string
  scheduledStart: string | null
  scheduledEnd: string | null
  durationHours: number | null
  quotedPrice: number | null
  status: BookingStatus
  counterpartyName: string
  createdAt: string
  hasReview: boolean
}

export type BookingDetail = {
  id: number
  serviceId: number
  providerProfileId: number
  serviceName: string
  categoryName: string
  city: string
  notes: string | null
  requestedDate: string
  scheduledStart: string | null
  scheduledEnd: string | null
  durationHours: number | null
  rescheduledAt: string | null
  rescheduleNote: string | null
  quotedPrice: number | null
  providerMessage: string | null
  declineReason: string | null
  status: BookingStatus
  customerId: string
  providerId: string
  customerName: string
  providerName: string
  customerPhone: string | null
  providerPhone: string | null
  customerCity: string | null
  providerCity: string | null
  createdAt: string
  respondedAt: string | null
  startedAt: string | null
  completedAt: string | null
  cancelledAt: string | null
  cancellationReason: string | null
  canAccept: boolean
  canReschedule: boolean
  canDecline: boolean
  canStart: boolean
  canComplete: boolean
  canCancel: boolean
  canReview: boolean
  review: Review | null
}

export type AccountProfile = {
  fullName: string
  email: string
  phoneNumber: string
  role: string
  city: string | null
}

export type UpdateAccountProfilePayload = {
  fullName: string
  phoneNumber: string
  city?: string | null
}

export type ProviderVerificationStatus = 'PendingReview' | 'Approved' | 'Rejected'
export type VerificationDocumentStatus = 'Pending' | 'Approved' | 'Rejected'
export type VerificationDocumentType =
  | 'ProfessionalCertificate'
  | 'ProfessionalLicense'
  | 'TrainingCertificate'
  | 'PortfolioEvidence'
  | 'Other'
export type AuditOutcome = 'Success' | 'Denied' | 'Failed'

export type ProviderChangeRequestType = 'Location' | 'AddService'
export type ProviderChangeRequestStatus = 'Pending' | 'Approved' | 'Rejected'

export type ProviderChangeRequest = {
  id: number
  type: ProviderChangeRequestType
  status: ProviderChangeRequestStatus
  requestedCity: string | null
  requestedLatitude: number | null
  requestedLongitude: number | null
  serviceId: number | null
  serviceName: string | null
  proofDocumentId: number | null
  proofFileName: string | null
  proofReviewStatus: VerificationDocumentStatus | null
  createdAt: string
  reviewNote: string | null
}

export type ProviderMe = {
  id: number
  userId: string
  fullName: string
  email: string
  phoneNumber: string
  city: string
  latitude: number | null
  longitude: number | null
  yearsOfExperience: number
  bio: string | null
  verificationStatus: ProviderVerificationStatus
  isSuspended: boolean
  suspensionReason: string | null
  verificationRejectionReason: string | null
  averageRating: number
  reviewCount: number
  services: CatalogService[]
  hasPhoto: boolean
  canEditLocation: boolean
  canEditServices: boolean
  pendingChanges: ProviderChangeRequest[]
}

export type PublicProvider = {
  id: number
  fullName: string
  city: string
  yearsOfExperience: number
  bio: string | null
  isVerified: boolean
  averageRating: number
  reviewCount: number
  services: CatalogService[]
  recentReviews: PublicReview[]
  hasPhoto: boolean
  workingHours: WorkingHour[]
}

export type AdminStats = {
  totalUsers: number
  customers: number
  providers: number
  pendingProviders: number
  pendingVerification: number
  approvedProviders: number
  suspendedProviders: number
  pendingDocuments: number
  rejectedDocuments: number
  categories: number
  services: number
  pendingBookings: number
  activeBookings: number
  completedBookings: number
  auditEventsLast24h: number
}

export type AdminAttentionItem = {
  kind: string
  title: string
  detail: string
  href: string
}

export type AdminAttention = {
  items: AdminAttentionItem[]
}

export type AdminProvider = {
  id: number
  userId: string
  fullName: string
  email: string
  city: string
  verificationStatus: ProviderVerificationStatus
  isSuspended: boolean
  suspensionReason: string | null
  averageRating: number
  reviewCount: number
  documentCount: number
  approvedDocumentCount: number
  services: string[]
}

export type AdminUser = {
  userId: string
  fullName: string
  email: string
  role: string
  createdAt: string
  lastLoginAt: string | null
  providerProfileId: number | null
  verificationStatus: ProviderVerificationStatus | null
  isSuspended: boolean | null
  averageRating: number | null
  reviewCount: number | null
  city: string | null
}

export type AdminUserDetail = AdminUser & {
  bookingCount: number
  reviewCount: number
  suspensionReason: string | null
  activeBookingCount: number
  completedBookingCount: number
  services: string[]
  recentAuditEvents: AuditLogItem[]
}

export type VerificationDocument = {
  id: number
  documentType: VerificationDocumentType
  originalFileName: string
  contentType: string
  fileSizeBytes: number
  uploadedAt: string
  reviewStatus: VerificationDocumentStatus
  reviewNote: string | null
  reviewedAt: string | null
  serviceId: number | null
  serviceName: string | null
}

export type ProviderVerification = {
  providerProfileId: number
  userId: string
  fullName: string
  email: string
  city: string
  latitude: number | null
  longitude: number | null
  yearsOfExperience: number
  bio: string | null
  verificationStatus: ProviderVerificationStatus
  verificationRejectionReason: string | null
  verificationReviewedAt: string | null
  isSuspended: boolean
  suspensionReason: string | null
  suspendedAt: string | null
  averageRating: number
  reviewCount: number
  services: string[]
  documents: VerificationDocument[]
  hasApprovedDocument: boolean
  hasPhoto: boolean
  pendingChanges: ProviderChangeRequest[]
}

export type AdminVerificationListItem = {
  providerProfileId: number
  userId: string
  fullName: string
  email: string
  city: string
  yearsOfExperience: number
  verificationStatus: ProviderVerificationStatus
  isSuspended: boolean
  documentCount: number
  pendingDocumentCount: number
  approvedDocumentCount: number
  rejectedDocumentCount: number
  services: string[]
  pendingChangeCount: number
}

export type AuditLogItem = {
  id: number
  createdAt: string
  actorUserId: string | null
  actorEmail: string | null
  actorRole: string | null
  category: string
  action: string
  entityType: string | null
  entityId: string | null
  outcome: AuditOutcome
  message: string | null
  summary: string
  ipAddress: string | null
}

export type AuditLogDetail = AuditLogItem & {
  detailsJson: string | null
  userAgent: string | null
  correlationId: string | null
}

export type AuditSummary = {
  eventsToday: number
  deniedActions: number
  adminActions: number
  providerVerificationEvents: number
}

export type AuditFilterOptions = {
  categories: string[]
  actions: { value: string; label: string; category: string }[]
}

export type AdminOverviewRange = 'today' | '7d' | '30d'

export type SeriesPoint = {
  date: string
  value: number
}

export type Kpi = {
  value: number
  previous: number | null
  series: SeriesPoint[]
}

export type BookingsSeriesPoint = {
  date: string
  created: number
  completed: number
  cancelled: number
}

export type AdminOverview = {
  range: AdminOverviewRange
  from: string
  to: string
  bucket: 'hour' | 'day'
  kpis: {
    bookingValue: Kpi
    bookingsActive: Kpi
    bookingsCompleted: Kpi
    conversionRate: Kpi
    avgProviderRating: Kpi
  }
  bookingsSeries: BookingsSeriesPoint[]
  funnel: {
    requested: number
    accepted: number
    completed: number
  }
  attention: {
    pendingVerifications: number
    stalePendingBookings: number
    overdueBookings: number
    suspendedProviders: number
  }
  supplyDemand: {
    city: string
    serviceId: number
    serviceName: string
    pendingBookings: number
    eligibleProviders: number
  }[]
  topProviders: {
    id: number
    name: string
    city: string
    rating: number
    reviewCount: number
    completedJobs: number
  }[]
  security24h: {
    failedLogins: number
    deniedActions: number
    csrfRejections: number
  }
}

export type ProviderDashboard = {
  verificationStatus: ProviderVerificationStatus
  isSuspended: boolean
  suspensionReason: string | null
  pendingRequestCount: number
  activeJobCount: number
  averageRating: number
  reviewCount: number
  recentPendingRequests: BookingSummary[]
  activeJobs: BookingSummary[]
}

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

export type ServiceRequestStatus = 'Open' | 'Booked' | 'Completed' | 'Cancelled'
export type OfferStatus = 'Pending' | 'Accepted' | 'Rejected' | 'Withdrawn'
export type BookingStatus = 'Scheduled' | 'InProgress' | 'Completed' | 'Cancelled'

export type ServiceRequestSummary = {
  id: number
  title: string
  serviceId: number
  serviceName: string
  categoryName: string
  city: string
  preferredDate: string
  budgetMin: number | null
  budgetMax: number | null
  status: ServiceRequestStatus
  offerCount: number
  createdAt: string
}

export type OfferForCustomer = {
  id: number
  providerId: string
  providerProfileId: number
  providerDisplayName: string
  providerAverageRating: number
  providerReviewCount: number
  price: number
  message: string
  estimatedDate: string
  status: OfferStatus
  createdAt: string
  canAccept: boolean
}

export type OfferSnapshot = {
  id: number
  price: number
  message: string
  estimatedDate: string
  status: OfferStatus
  createdAt: string
}

export type RequestDetailForCustomer = {
  id: number
  title: string
  description: string
  serviceId: number
  serviceName: string
  categoryName: string
  city: string
  preferredDate: string
  budgetMin: number | null
  budgetMax: number | null
  status: ServiceRequestStatus
  offerCount: number
  createdAt: string
  canEdit: boolean
  canCancel: boolean
  bookingId: number | null
  offers: OfferForCustomer[]
}

export type RequestSummaryForProvider = {
  id: number
  title: string
  description: string
  serviceId: number
  serviceName: string
  categoryName: string
  city: string
  preferredDate: string
  budgetMin: number | null
  budgetMax: number | null
  status: ServiceRequestStatus
  createdAt: string
}

export type RequestDetailForProvider = RequestSummaryForProvider & {
  canOffer: boolean
  myOffer: OfferSnapshot | null
}

export type CreateServiceRequestPayload = {
  serviceId: number
  title: string
  description: string
  city: string
  preferredDate: string
  budgetMin?: number | null
  budgetMax?: number | null
}

export type UpdateServiceRequestPayload = {
  title: string
  description: string
  preferredDate: string
  budgetMin?: number | null
  budgetMax?: number | null
}

export type SubmitOfferPayload = {
  price: number
  message: string
  estimatedDate: string
}

export type OfferMine = {
  id: number
  serviceRequestId: number
  requestTitle: string
  serviceName: string
  categoryName: string
  city: string
  price: number
  message: string
  estimatedDate: string
  status: OfferStatus
  requestStatus: ServiceRequestStatus
  createdAt: string
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
  serviceRequestId: number
  offerId: number
  title: string
  serviceName: string
  categoryName: string
  city: string
  scheduledDate: string
  finalPrice: number
  status: BookingStatus
  counterpartyName: string
  createdAt: string
  hasReview: boolean
}

export type BookingDetail = {
  id: number
  serviceRequestId: number
  offerId: number
  providerProfileId: number
  title: string
  description: string
  serviceName: string
  categoryName: string
  city: string
  scheduledDate: string
  finalPrice: number
  status: BookingStatus
  customerId: string
  providerId: string
  customerName: string
  providerName: string
  customerEmail: string | null
  providerEmail: string | null
  customerContact: string | null
  customerCity: string | null
  providerCity: string | null
  createdAt: string
  startedAt: string | null
  completedAt: string | null
  cancelledAt: string | null
  cancellationReason: string | null
  canStart: boolean
  canComplete: boolean
  canCancel: boolean
  canReview: boolean
  review: Review | null
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

export type ProviderMe = {
  id: number
  userId: string
  fullName: string
  email: string
  city: string
  yearsOfExperience: number
  bio: string | null
  verificationStatus: ProviderVerificationStatus
  isSuspended: boolean
  suspensionReason: string | null
  verificationRejectionReason: string | null
  averageRating: number
  reviewCount: number
  services: CatalogService[]
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
  openRequests: number
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
  requestCount: number
  bookingCount: number
  reviewCount: number
  suspensionReason: string | null
  offerCount: number
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
}

export type ProviderVerification = {
  providerProfileId: number
  userId: string
  fullName: string
  email: string
  city: string
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
    requestsCreated: number
    requestsWithOffer: number
    booked: number
    completed: number
  }
  attention: {
    pendingVerifications: number
    staleOpenRequests: number
    overdueBookings: number
    suspendedProviders: number
  }
  supplyDemand: {
    city: string
    serviceId: number
    serviceName: string
    openRequests: number
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

export type CustomerDashboard = {
  openRequestCount: number
  offersAwaitingDecision: number
  activeBookingCount: number
  completedAwaitingReview: number
  recentRequests: ServiceRequestSummary[]
  activeBookings: BookingSummary[]
}

export type ProviderDashboard = {
  verificationStatus: ProviderVerificationStatus
  isSuspended: boolean
  suspensionReason: string | null
  eligibleRequestCount: number
  pendingOfferCount: number
  activeBookingCount: number
  averageRating: number
  reviewCount: number
  recentAvailableRequests: RequestSummaryForProvider[]
  recentOffers: OfferMine[]
  activeJobs: BookingSummary[]
}

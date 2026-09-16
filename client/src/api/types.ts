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

export type ProviderMe = {
  id: number
  userId: string
  fullName: string
  email: string
  city: string
  yearsOfExperience: number
  bio: string | null
  isApproved: boolean
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
  isApproved: boolean
  averageRating: number
  reviewCount: number
  services: CatalogService[]
  recentReviews: PublicReview[]
}

export type AdminStats = {
  customers: number
  providers: number
  pendingProviders: number
  categories: number
  services: number
  openRequests: number
  activeBookings: number
  completedBookings: number
}

export type AdminProvider = {
  id: number
  userId: string
  fullName: string
  email: string
  city: string
  isApproved: boolean
  averageRating: number
  reviewCount: number
  services: string[]
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
  isApproved: boolean
  eligibleRequestCount: number
  pendingOfferCount: number
  activeBookingCount: number
  averageRating: number
  reviewCount: number
  recentAvailableRequests: RequestSummaryForProvider[]
  recentOffers: OfferMine[]
  activeJobs: BookingSummary[]
}

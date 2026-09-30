// Mirrors the API contracts (InspectFlow.Modules.*.Application DTOs).

export type Role = "Company" | "Agent" | "Tenant";

export interface CompanyMembership { companyId: string; companyName: string; role: string }
export type SubscriptionStatus = "None" | "Pending" | "Active" | "PastDue" | "Cancelled" | "Expired";
/** `hasAccess` is true when the role needs no subscription; `status` is effective ("Expired" once the grace period is over). */
export interface SubscriptionSummary {
  required: boolean; hasAccess: boolean; status: SubscriptionStatus; planCode: string | null; planName: string | null;
  currentPeriodEnd: string | null; accessEndsAt: string | null;
}
export interface Me { id: string; email: string; fullName: string; roles: Role[]; company: CompanyMembership | null; subscription: SubscriptionSummary }

export type PlanInterval = "Month" | "Year";
export interface BillingPlan { code: string; name: string; description: string; priceCents: number; currency: string; interval: PlanInterval; features: string[] }
export interface CheckoutStart { checkoutId: string; checkoutUrl: string; expiresAt: string }
export type CheckoutSessionStatus = "Open" | "Completed" | "Failed" | "Expired";
export interface CheckoutStatus { checkoutId: string; status: CheckoutSessionStatus; planCode: string; subscription: SubscriptionSummary }
export interface SandboxSession { sessionId: string; planName: string; priceCents: number; currency: string; interval: PlanInterval; status: CheckoutSessionStatus; customerEmail: string }
export interface SandboxPaymentResult { approved: boolean; message: string; redirectUrl: string | null }
export interface AuthResponse { accessToken: string; accessTokenExpiresAt: string; refreshToken: string | null; user: Me }

export type InspectionType = "MoveIn" | "MoveOut" | "Periodic" | "Other";
export type Visibility = "Public" | "Private";
export type InspectionStatus =
  | "Draft" | "Open" | "Assigned" | "InProgress" | "Review" | "Completed"
  | "AwaitingTenant" | "Accepted" | "Disputed" | "Cancelled" | "Expired";
export type RoomStatus = "Pending" | "InProgress" | "Completed";
export type DefectClassification = "Unknown" | "NewDamage" | "PreExisting" | "NormalWear" | "Resolved" | "Unchanged";
export type ComparisonDecision = "NewDamage" | "PreExisting" | "NormalWear" | "Resolved" | "Unchanged" | "UnableToDetermine";

export const ROOM_TYPES = ["LivingRoom", "Bedroom", "Kitchen", "Bathroom", "DiningRoom", "Hallway", "Garage", "Garden", "Utility", "Office", "Balcony", "Other"] as const;
export const PROPERTY_TYPES = ["House", "Apartment", "Studio", "Townhouse", "Bungalow", "Commercial", "Other"] as const;
export type RoomType = (typeof ROOM_TYPES)[number];
export type PropertyType = (typeof PROPERTY_TYPES)[number];

export interface PropertyRoom { id: string; roomType: RoomType; name: string; sequence: number; createdAt: string }
export interface PropertySummary {
  id: string; addressLine1: string; addressLine2: string | null; city: string; postcode: string; country: string;
  propertyType: PropertyType; roomCount: number; activeTenancies: number; openInspections: number; createdAt: string; updatedAt: string;
}
export interface Property {
  id: string; addressLine1: string; addressLine2: string | null; city: string; postcode: string; country: string;
  propertyType: PropertyType; rooms: PropertyRoom[]; createdAt: string; updatedAt: string;
}

export interface TenancyMember { id: string; fullName: string; email: string; joined: boolean; invitedAt: string; joinedAt: string | null }
export interface Tenancy {
  id: string; propertyId: string; reference: string | null; startDate: string; endDate: string | null;
  status: "Upcoming" | "Active" | "Ended" | "Cancelled"; members: TenancyMember[]; createdAt: string;
}
export interface TenantInvitation { member: TenancyMember; invitationLink: string; expiresAt: string }

export interface InspectionSummary {
  id: string; propertyId: string; propertyAddress: string; tenancyId: string | null; inspectionType: InspectionType;
  visibility: Visibility; status: InspectionStatus; agentName: string | null; roomsCompleted: number; roomsTotal: number;
  scheduledDate: string | null; createdAt: string; updatedAt: string; completedAt: string | null;
}
export interface RoomProgress {
  id: string; name: string; roomType: RoomType; sequence: number; status: RoomStatus; generalPhotoCount: number;
  defectCount: number; hasAiDescription: boolean; hasFinalDescription: boolean; comparisonDecided: boolean | null;
}
export interface InvitationStatus {
  createdAt: string; expiresAt: string; attemptCount: number; maxAttempts: number; used: boolean; usedAt: string | null;
  revoked: boolean; expired: boolean; invitedEmail: string | null;
}
export interface InspectionDetails {
  id: string; inspectionType: InspectionType; visibility: Visibility; status: InspectionStatus;
  property: { id: string; addressLine1: string; addressLine2: string | null; city: string; postcode: string; country: string; propertyType: string };
  tenancy: Tenancy | null; agent: { id: string; fullName: string; email: string | null; phone: string | null } | null; companyName: string;
  comparisonInspection: { inspectionId: string; inspectionType: string; completedAt: string | null; reportNumber: string | null } | null;
  instructions: string | null; scheduledDate: string | null; acceptBy: string | null; createdAt: string; publishedAt: string | null;
  acceptedAt: string | null; startedAt: string | null; completedAt: string | null; sentToTenantAt: string | null; tenantRespondedAt: string | null;
  rooms: RoomProgress[]; roomsCompleted: number; report: { reportId: string; reportNumber: string; latestVersion: number; generatedAt: string } | null;
  invitation: InvitationStatus | null; allowedActions: string[];
}
export interface PrivateInvitation { link: string; accessCode: string; expiresAt: string; maxAttempts: number }
export interface PublishResult { inspection: InspectionDetails; invitation: PrivateInvitation | null }

export interface CompanyDashboard {
  properties: number; draft: number; open: number; assigned: number; inProgress: number; inReview: number;
  awaitingTenant: number; completed: number; disputed: number;
  recentInspections: { id: string; propertyAddress: string; propertyId: string; inspectionType: InspectionType; status: InspectionStatus; agentName: string | null; updatedAt: string }[];
}

export interface AvailableInspection {
  id: string; inspectionType: InspectionType; visibility: Visibility; companyName: string; city: string; postcodeArea: string;
  propertyType: string; roomCount: number; roomNames: string[]; scheduledDate: string | null; acceptBy: string | null;
  publishedAt: string | null; hasComparison: boolean;
}
export interface AgentDashboard { available: number; assigned: number; inProgress: number; inReview: number; completed: number; active: InspectionSummary[] }
export interface InvitationPreview { requiresCode: boolean; expiresAt: string; attemptsRemaining: number; inspection: AvailableInspection | null }

export interface Media { id: string; mediaType: "General" | "Defect"; defectId: string | null; url: string; originalFilename: string; mimeType: string; fileSize: number; uploadedAt: string }
export interface AiAnalysis {
  id: string; kind: string; status: "Pending" | "Processing" | "Completed" | "Failed"; provider: string; model: string | null; isMock: boolean;
  description: string | null; confidence: number | null; error: string | null; requestedAt: string; completedAt: string | null; result: unknown;
}
export interface Defect {
  id: string; description: string | null; location: string | null; classification: DefectClassification; aiDescription: string | null;
  aiConfidence: number | null; finalDescription: string | null; agentConfirmed: boolean; photos: Media[]; latestAnalysis: AiAnalysis | null;
}
export interface BaselineRoom {
  name: string; description: string | null; agentNotes: string | null; defectsFound: boolean; photoUrls: string[];
  defects: { title: string | null; location: string | null; classification: string; description: string | null; photoUrls: string[] }[];
  reportNumber: string | null; completedAt: string | null;
}
export interface Comparison {
  id: string; basicComparison: string | null; aiAnalysis: string | null; agentDecision: ComparisonDecision | null; agentNotes: string | null;
  decidedAt: string | null; latestAnalysis: AiAnalysis | null; baseline: BaselineRoom | null;
}
export interface RoomDetail {
  id: string; inspectionId: string; name: string; roomType: RoomType; sequence: number; status: RoomStatus; isRequired: boolean;
  aiDescription: string | null; aiDescriptionGeneratedAt: string | null; finalDescription: string | null;
  finalDescriptionSource: "None" | "Ai" | "Agent"; defectsFound: boolean; agentNotes: string | null; completedAt: string | null;
  generalPhotos: Media[]; defects: Defect[]; latestAnalysis: AiAnalysis | null; comparison: Comparison | null;
  completionIssues: string[]; editable: boolean; inspectionStatus: InspectionStatus; previousRoomId: string | null; nextRoomId: string | null;
  roomsCompleted: number; roomsTotal: number;
}

// Report snapshot (immutable)
export interface ReportPhoto { mediaId: string; storageKey: string; mimeType: string; caption: string | null; uploadedAt: string }
export interface ReportDefect { id: string; title: string | null; location: string | null; classification: string; description: string | null; photos: ReportPhoto[] }
export interface ReportRoom {
  id: string; name: string; roomType: string; sequence: number; description: string | null; agentNotes: string | null; defectsFound: boolean;
  descriptionSource: string; photos: ReportPhoto[]; defects: ReportDefect[];
  comparison: { baselineDescription: string | null; baselineDefects: string[]; baselinePhotos: ReportPhoto[]; basicComparison: string | null; aiAnalysis: string | null; decision: string | null; notes: string | null } | null;
}
export interface ReportSnapshot {
  schemaVersion: number; reportId: string; reportNumber: string; versionNumber: number; generatedAt: string;
  company: { name: string; contactEmail: string | null; phone: string | null };
  property: { addressLine1: string; addressLine2: string | null; city: string; postcode: string; country: string; propertyType: string };
  inspection: { id: string; type: InspectionType; scheduledDate: string | null; startedAt: string | null; completedAt: string; tenancyReference: string | null;
    tenancyStartDate: string | null; tenancyEndDate: string | null; comparisonReportNumber: string | null; comparisonCompletedAt: string | null };
  agent: { name: string; email: string | null } | null;
  tenants: { name: string; email: string | null }[];
  rooms: ReportRoom[];
  comparison: { sourceReportNumber: string | null; roomsCompared: number; decisionCounts: Record<string, number>; items: { roomName: string; decision: string; notes: string | null }[] } | null;
}
export interface ReportView {
  reportId: string; reportNumber: string; versionNumber: number; generatedAt: string; snapshotSha256: string; inspectionId: string;
  inspectionStatus: InspectionStatus; viewerKind: "Company" | "Agent" | "Tenant" | "Shared"; snapshot: ReportSnapshot;
  photoUrls: Record<string, string>; pdfUrl: string | null;
  versions: { versionNumber: number; generatedAt: string; reason: string; snapshotSha256: string; pdfSha256: string | null }[];
  observations: { id: string; roomId: string | null; roomName: string | null; authorName: string; text: string; createdAt: string }[];
  responses: { id: string; userName: string; decision: "Accepted" | "Disputed"; comment: string | null; createdAt: string }[];
  canRespond: boolean;
}
export interface Review { inspection: InspectionDetails; preview: ReportSnapshot; photoUrls: Record<string, string>; blockingIssues: string[]; canFinalize: boolean }

export interface TenantInspection {
  id: string; inspectionType: InspectionType; status: InspectionStatus; propertyAddress: string; companyName: string;
  completedAt: string | null; sentToTenantAt: string | null; respondedAt: string | null; reportNumber: string | null;
}
export interface TenantDashboard { awaitingReview: TenantInspection[]; accepted: TenantInspection[]; disputed: TenantInspection[] }

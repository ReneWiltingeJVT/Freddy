/** Admin API types matching the ASP.NET Core backend DTOs. */

export type PackageCategory = 'Protocol' | 'WorkInstruction' | 'PersonalPlan';

export const CATEGORY_LABELS: Record<PackageCategory, string> = {
  Protocol: 'Protocol',
  WorkInstruction: 'Werkinstructie',
  PersonalPlan: 'Persoonlijk plan',
};

export interface PackageSummaryDto {
  id: string;
  title: string;
  description: string;
  tags: string[];
  category: PackageCategory;
  clientId: string | null;
  isPublished: boolean;
  documentCount: number;
  updatedAt: string;
}

export interface PackageDto {
  id: string;
  title: string;
  description: string;
  content: string;
  tags: string[];
  synonyms: string[];
  category: PackageCategory;
  clientId: string | null;
  isPublished: boolean;
  requiresConfirmation: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface DocumentDto {
  id: string;
  packageId: string;
  name: string;
  description: string | null;
  type: string;
  stepsContent: string | null;
  fileUrl: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreatePackageRequest {
  title: string;
  description: string;
  content: string;
  tags: string[];
  synonyms: string[];
  category: PackageCategory;
  clientId: string | null;
  requiresConfirmation: boolean;
}

export interface UpdatePackageRequest {
  title: string;
  description: string;
  content: string;
  tags: string[];
  synonyms: string[];
  category: PackageCategory;
  clientId: string | null;
  requiresConfirmation: boolean;
}

// ── Clients ───────────────────────────────────────────────

export interface ClientDto {
  id: string;
  displayName: string;
  aliases: string[];
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateClientRequest {
  displayName: string;
  aliases: string[];
}

export interface UpdateClientRequest {
  displayName: string;
  aliases: string[];
  isActive: boolean;
}

export interface CreateDocumentRequest {
  name: string;
  description?: string;
  type: string;
  stepsContent?: string;
  fileUrl?: string;
}

export interface UpdateDocumentRequest {
  name: string;
  description?: string;
  type: string;
  stepsContent?: string;
  fileUrl?: string;
}

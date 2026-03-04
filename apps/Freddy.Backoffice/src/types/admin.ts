/** Admin API types matching the ASP.NET Core backend DTOs. */

export interface PackageSummaryDto {
  id: string;
  title: string;
  description: string;
  tags: string[];
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
  requiresConfirmation: boolean;
}

export interface UpdatePackageRequest {
  title: string;
  description: string;
  content: string;
  tags: string[];
  synonyms: string[];
  requiresConfirmation: boolean;
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

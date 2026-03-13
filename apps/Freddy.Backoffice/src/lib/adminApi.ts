import ky from 'ky';
import type {
  PackageSummaryDto,
  PackageDto,
  DocumentDto,
  ClientDto,
  CreatePackageRequest,
  UpdatePackageRequest,
  CreateDocumentRequest,
  UpdateDocumentRequest,
  CreateClientRequest,
  UpdateClientRequest,
} from '../types/admin';

const ADMIN_API_KEY = 'freddy-admin-dev-key';

const adminApi = ky.create({
  prefixUrl: '/api/admin',
  timeout: 30_000,
  hooks: {
    beforeRequest: [
      (request) => {
        request.headers.set('X-Admin-Api-Key', ADMIN_API_KEY);
      },
    ],
  },
});

// ── Packages ──────────────────────────────────────────────

export async function getPackages(params?: {
  search?: string;
  isPublished?: boolean;
  category?: string;
}): Promise<PackageSummaryDto[]> {
  const searchParams = new URLSearchParams();
  if (params?.search) searchParams.set('search', params.search);
  if (params?.isPublished !== undefined)
    searchParams.set('isPublished', String(params.isPublished));
  if (params?.category) searchParams.set('category', params.category);

  return adminApi.get('packages', { searchParams }).json<PackageSummaryDto[]>();
}

export async function getPackage(id: string): Promise<PackageDto> {
  return adminApi.get(`packages/${id}`).json<PackageDto>();
}

export async function createPackage(
  data: CreatePackageRequest,
): Promise<PackageDto> {
  return adminApi.post('packages', { json: data }).json<PackageDto>();
}

export async function updatePackage(
  id: string,
  data: UpdatePackageRequest,
): Promise<PackageDto> {
  return adminApi.put(`packages/${id}`, { json: data }).json<PackageDto>();
}

export async function deletePackage(id: string): Promise<void> {
  await adminApi.delete(`packages/${id}`);
}

export async function publishPackage(id: string): Promise<PackageDto> {
  return adminApi.post(`packages/${id}/publish`).json<PackageDto>();
}

export async function unpublishPackage(id: string): Promise<PackageDto> {
  return adminApi.post(`packages/${id}/unpublish`).json<PackageDto>();
}

// ── Documents ─────────────────────────────────────────────

export async function getDocuments(
  packageId: string,
): Promise<DocumentDto[]> {
  return adminApi
    .get(`packages/${packageId}/documents`)
    .json<DocumentDto[]>();
}

export async function createDocument(
  packageId: string,
  data: CreateDocumentRequest,
): Promise<DocumentDto> {
  return adminApi
    .post(`packages/${packageId}/documents`, { json: data })
    .json<DocumentDto>();
}

export async function updateDocument(
  packageId: string,
  documentId: string,
  data: UpdateDocumentRequest,
): Promise<DocumentDto> {
  return adminApi
    .put(`packages/${packageId}/documents/${documentId}`, { json: data })
    .json<DocumentDto>();
}

export async function deleteDocument(
  packageId: string,
  documentId: string,
): Promise<void> {
  await adminApi.delete(`packages/${packageId}/documents/${documentId}`);
}

export async function uploadDocument(
  packageId: string,
  file: File,
  description?: string,
): Promise<DocumentDto> {
  const formData = new FormData();
  formData.append('file', file);
  if (description) formData.append('description', description);

  return adminApi
    .post(`packages/${packageId}/documents/upload`, { body: formData })
    .json<DocumentDto>();
}

// ── Clients ───────────────────────────────────────────────

export async function getClients(params?: {
  isActive?: boolean;
  search?: string;
}): Promise<ClientDto[]> {
  const searchParams = new URLSearchParams();
  if (params?.isActive !== undefined)
    searchParams.set('isActive', String(params.isActive));
  if (params?.search) searchParams.set('search', params.search);
  return adminApi.get('clients', { searchParams }).json<ClientDto[]>();
}

export async function getClient(id: string): Promise<ClientDto> {
  return adminApi.get(`clients/${id}`).json<ClientDto>();
}

export async function createClient(
  data: CreateClientRequest,
): Promise<ClientDto> {
  return adminApi.post('clients', { json: data }).json<ClientDto>();
}

export async function updateClient(
  id: string,
  data: UpdateClientRequest,
): Promise<ClientDto> {
  return adminApi.put(`clients/${id}`, { json: data }).json<ClientDto>();
}

export async function deleteClient(id: string): Promise<void> {
  await adminApi.delete(`clients/${id}`);
}

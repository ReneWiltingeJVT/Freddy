import {
  useQuery,
  useMutation,
  useQueryClient,
} from '@tanstack/react-query';
import {
  getDocuments,
  createDocument,
  updateDocument,
  deleteDocument,
  uploadDocument,
} from '../lib/adminApi';
import type { CreateDocumentRequest, UpdateDocumentRequest } from '../types/admin';

const documentsKey = (packageId: string) =>
  ['packages', packageId, 'documents'] as const;

export function useDocuments(packageId: string | undefined) {
  return useQuery({
    queryKey: documentsKey(packageId!),
    queryFn: () => getDocuments(packageId!),
    enabled: !!packageId,
  });
}

export function useCreateDocument(packageId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateDocumentRequest) =>
      createDocument(packageId, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: documentsKey(packageId),
      });
    },
  });
}

export function useUpdateDocument(packageId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      documentId,
      data,
    }: {
      documentId: string;
      data: UpdateDocumentRequest;
    }) => updateDocument(packageId, documentId, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: documentsKey(packageId),
      });
    },
  });
}

export function useDeleteDocument(packageId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (documentId: string) => deleteDocument(packageId, documentId),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: documentsKey(packageId),
      });
    },
  });
}

export function useUploadDocument(packageId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      file,
      description,
    }: {
      file: File;
      description?: string;
    }) => uploadDocument(packageId, file, description),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: documentsKey(packageId),
      });
    },
  });
}

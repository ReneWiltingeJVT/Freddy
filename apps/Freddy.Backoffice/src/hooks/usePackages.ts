import {
  useQuery,
  useMutation,
  useQueryClient,
} from '@tanstack/react-query';
import {
  getPackages,
  getPackage,
  createPackage,
  updatePackage,
  deletePackage,
  publishPackage,
  unpublishPackage,
} from '../lib/adminApi';
import type { CreatePackageRequest, UpdatePackageRequest } from '../types/admin';

const PACKAGES_KEY = ['packages'] as const;
const packageKey = (id: string) => ['packages', id] as const;

export function usePackages(params?: {
  search?: string;
  isPublished?: boolean;
  category?: string;
}) {
  return useQuery({
    queryKey: [...PACKAGES_KEY, params],
    queryFn: () => getPackages(params),
  });
}

export function usePackage(id: string | undefined) {
  return useQuery({
    queryKey: packageKey(id!),
    queryFn: () => getPackage(id!),
    enabled: !!id,
  });
}

export function useCreatePackage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreatePackageRequest) => createPackage(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: PACKAGES_KEY });
    },
  });
}

export function useUpdatePackage(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdatePackageRequest) => updatePackage(id, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: PACKAGES_KEY });
      void queryClient.invalidateQueries({ queryKey: packageKey(id) });
    },
  });
}

export function useDeletePackage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deletePackage(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: PACKAGES_KEY });
    },
  });
}

export function usePublishPackage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => publishPackage(id),
    onSuccess: (_data, id) => {
      void queryClient.invalidateQueries({ queryKey: PACKAGES_KEY });
      void queryClient.invalidateQueries({ queryKey: packageKey(id) });
    },
  });
}

export function useUnpublishPackage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => unpublishPackage(id),
    onSuccess: (_data, id) => {
      void queryClient.invalidateQueries({ queryKey: PACKAGES_KEY });
      void queryClient.invalidateQueries({ queryKey: packageKey(id) });
    },
  });
}

import { useEffect } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { useClient, useCreateClient, useUpdateClient } from '../../hooks/useClients';

interface ClientFormValues {
  displayName: string;
  aliases: string;
  isActive: boolean;
}

export function ClientFormPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const isEditing = !!id;

  const { data: existingClient, isLoading } = useClient(id);
  const createMutation = useCreateClient();
  const updateMutation = useUpdateClient(id ?? '');

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ClientFormValues>({
    defaultValues: {
      displayName: '',
      aliases: '',
      isActive: true,
    },
  });

  // Populate form when editing
  useEffect(() => {
    if (isEditing && existingClient) {
      reset({
        displayName: existingClient.displayName,
        aliases: existingClient.aliases.join(', '),
        isActive: existingClient.isActive,
      });
    }
  }, [isEditing, existingClient, reset]);

  async function onSubmit(values: ClientFormValues) {
    const payload = {
      displayName: values.displayName.trim(),
      aliases: values.aliases
        .split(',')
        .map((a) => a.trim())
        .filter(Boolean),
      isActive: values.isActive,
    };

    try {
      if (isEditing) {
        await updateMutation.mutateAsync(payload);
      } else {
        await createMutation.mutateAsync(payload);
      }
      navigate('/clients');
    } catch {
      // Error is handled by mutation state
    }
  }

  if (isEditing && isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary-200 border-t-primary-600" />
      </div>
    );
  }

  const mutationError = createMutation.error ?? updateMutation.error;

  return (
    <div className="max-w-xl">
      {/* Breadcrumb */}
      <nav className="mb-6 text-sm text-gray-500">
        <Link to="/clients" className="hover:text-primary-600">Cliënten</Link>
        {isEditing && existingClient && (
          <>
            <span className="mx-2">›</span>
            <span className="text-gray-700">{existingClient.displayName}</span>
          </>
        )}
        <span className="mx-2">›</span>
        <span className="text-gray-900">
          {isEditing ? 'Bewerken' : 'Nieuwe cliënt'}
        </span>
      </nav>

      <h2 className="text-2xl font-bold text-gray-900 mb-6">
        {isEditing ? 'Cliënt bewerken' : 'Nieuwe cliënt aanmaken'}
      </h2>

      {mutationError && (
        <div className="rounded-md bg-red-50 p-4 text-sm text-red-700 mb-6">
          Er ging iets mis bij het opslaan. Controleer de velden en probeer opnieuw.
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
        {/* Display name */}
        <div>
          <label htmlFor="displayName" className="block text-sm font-medium text-gray-700 mb-1">
            Naam *
          </label>
          <input
            id="displayName"
            type="text"
            {...register('displayName', {
              required: 'Naam is verplicht',
              maxLength: { value: 200, message: 'Naam mag maximaal 200 tekens bevatten' },
            })}
            className="w-full rounded-md border border-gray-300 px-4 py-2 text-sm shadow-sm focus:border-primary-500 focus:ring-1 focus:ring-primary-500 focus:outline-none"
            placeholder="bijv. Jan de Vries"
          />
          {errors.displayName && (
            <p className="mt-1 text-sm text-red-600">{errors.displayName.message}</p>
          )}
          <p className="mt-1 text-xs text-gray-500">
            De naam die Freddy gebruikt om de cliënt te herkennen in gesprekken.
          </p>
        </div>

        {/* Aliases */}
        <div>
          <label htmlFor="aliases" className="block text-sm font-medium text-gray-700 mb-1">
            Aliassen
          </label>
          <input
            id="aliases"
            type="text"
            {...register('aliases')}
            className="w-full rounded-md border border-gray-300 px-4 py-2 text-sm shadow-sm focus:border-primary-500 focus:ring-1 focus:ring-primary-500 focus:outline-none"
            placeholder="Jan, de heer De Vries (gescheiden door komma's)"
          />
          <p className="mt-1 text-xs text-gray-500">
            Alternatieve namen of bijnamen, zodat Freddy de cliënt ook herkent als ze anders worden aangesproken.
          </p>
        </div>

        {/* isActive — only when editing */}
        {isEditing && (
          <div className="flex items-center gap-3">
            <input
              id="isActive"
              type="checkbox"
              {...register('isActive')}
              className="h-4 w-4 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
            />
            <label htmlFor="isActive" className="text-sm text-gray-700">
              Cliënt is actief
            </label>
          </div>
        )}

        {/* Actions */}
        <div className="flex items-center gap-3 pt-4 border-t border-gray-200">
          <button
            type="submit"
            disabled={isSubmitting}
            className="inline-flex items-center rounded-md bg-primary-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-primary-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {isSubmitting ? 'Opslaan...' : isEditing ? 'Wijzigingen opslaan' : 'Cliënt aanmaken'}
          </button>
          <Link
            to="/clients"
            className="rounded-md px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-100 transition-colors"
          >
            Annuleren
          </Link>
        </div>
      </form>
    </div>
  );
}

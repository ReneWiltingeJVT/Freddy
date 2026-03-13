import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useClients, useDeleteClient } from '../../hooks/useClients';

export function ClientListPage() {
  const [search, setSearch] = useState('');
  const { data: clients, isLoading } = useClients({ search: search || undefined });
  const deleteMutation = useDeleteClient();

  function handleDelete(id: string, name: string) {
    if (!confirm(`Weet je zeker dat je "${name}" wilt verwijderen?`)) return;
    deleteMutation.mutate(id);
  }

  return (
    <div>
      {/* Page header */}
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-bold text-gray-900">Cliënten</h2>
        <Link
          to="/clients/new"
          className="inline-flex items-center rounded-md bg-primary-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-primary-700 transition-colors"
        >
          + Nieuwe cliënt
        </Link>
      </div>

      {/* Search */}
      <div className="mb-6">
        <input
          type="search"
          placeholder="Zoek op naam of alias..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-full max-w-sm rounded-md border border-gray-300 px-4 py-2 text-sm shadow-sm focus:border-primary-500 focus:ring-1 focus:ring-primary-500 focus:outline-none"
        />
      </div>

      {/* Loading state */}
      {isLoading && (
        <div className="flex items-center justify-center py-12">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary-200 border-t-primary-600" />
        </div>
      )}

      {/* Empty state */}
      {!isLoading && clients && clients.length === 0 && (
        <div className="rounded-lg border border-gray-200 bg-white p-12 text-center text-sm text-gray-500">
          {search ? 'Geen cliënten gevonden voor deze zoekopdracht.' : 'Nog geen cliënten aangemaakt.'}
        </div>
      )}

      {/* Table */}
      {!isLoading && clients && clients.length > 0 && (
        <div className="overflow-hidden rounded-lg border border-gray-200 bg-white shadow-sm">
          <table className="min-w-full divide-y divide-gray-200 text-sm">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left font-medium text-gray-500 uppercase tracking-wider text-xs">Naam</th>
                <th className="px-6 py-3 text-left font-medium text-gray-500 uppercase tracking-wider text-xs">Aliassen</th>
                <th className="px-6 py-3 text-left font-medium text-gray-500 uppercase tracking-wider text-xs">Status</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500 uppercase tracking-wider text-xs">Acties</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {clients.map((client) => {
                const aliasDisplay = client.aliases.length === 0
                  ? '—'
                  : client.aliases.slice(0, 3).join(', ') + (client.aliases.length > 3 ? ` +${client.aliases.length - 3}` : '');
                return (
                  <tr key={client.id} className="hover:bg-gray-50 transition-colors">
                    <td className="px-6 py-4 font-medium text-gray-900">{client.displayName}</td>
                    <td className="px-6 py-4 text-gray-500">{aliasDisplay}</td>
                    <td className="px-6 py-4">
                      <span
                        className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
                          client.isActive
                            ? 'bg-green-100 text-green-800'
                            : 'bg-gray-100 text-gray-600'
                        }`}
                      >
                        {client.isActive ? 'Actief' : 'Inactief'}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-3">
                        <Link
                          to={`/clients/${client.id}/edit`}
                          className="text-primary-600 hover:text-primary-800 font-medium"
                        >
                          Bewerken
                        </Link>
                        <button
                          onClick={() => handleDelete(client.id, client.displayName)}
                          disabled={deleteMutation.isPending}
                          className="text-red-600 hover:text-red-800 font-medium disabled:opacity-50"
                        >
                          Verwijderen
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

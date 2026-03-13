import { Routes, Route } from 'react-router-dom';
import { Layout } from './components/Layout';
import { PackageListPage } from './features/packages/PackageListPage';
import { PackageFormPage } from './features/packages/PackageFormPage';
import { PackageDetailPage } from './features/packages/PackageDetailPage';
import { ClientListPage } from './features/clients/ClientListPage';
import { ClientFormPage } from './features/clients/ClientFormPage';

export function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<PackageListPage />} />
        <Route path="/packages/new" element={<PackageFormPage />} />
        <Route path="/packages/:id" element={<PackageDetailPage />} />
        <Route path="/packages/:id/edit" element={<PackageFormPage />} />
        <Route path="/clients" element={<ClientListPage />} />
        <Route path="/clients/new" element={<ClientFormPage />} />
        <Route path="/clients/:id/edit" element={<ClientFormPage />} />
      </Route>
    </Routes>
  );
}

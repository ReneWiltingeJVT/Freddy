import { Routes, Route } from 'react-router-dom';
import { Layout } from './components/Layout';
import { PackageListPage } from './features/packages/PackageListPage';
import { PackageFormPage } from './features/packages/PackageFormPage';
import { PackageDetailPage } from './features/packages/PackageDetailPage';

export function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<PackageListPage />} />
        <Route path="/packages/new" element={<PackageFormPage />} />
        <Route path="/packages/:id" element={<PackageDetailPage />} />
        <Route path="/packages/:id/edit" element={<PackageFormPage />} />
      </Route>
    </Routes>
  );
}

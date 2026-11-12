import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from './AuthContext';

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { kullanici } = useAuth();
  if (!kullanici) return <Navigate to="/giris" replace />;
  return <>{children}</>;
}
